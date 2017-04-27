using MealsService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Responses;
using Microsoft.EntityFrameworkCore;

namespace MealsService.Services
{
    public class ScheduleService
    {
        private MealsDbContext _dbContext;

        private DietService _dietService;
        private RecipesService _recipesService;

        private Random _rand;

        public ScheduleService(MealsDbContext dbContext, DietService dietService, RecipesService recipesService)
        {
            _dbContext = dbContext;

            _dietService = dietService;
            _recipesService = recipesService;

            _rand = new Random();
        }

        public List<ScheduleDay> GetSchedule(int userId, DateTime start, DateTime? end = null)
        {
            if (!end.HasValue)
            {
                end = start.AddDays(6);
            }
            
            var schedule = _dbContext.ScheduleDays.Where(d => d.UserId == userId && d.Date >= start && d.Date <= end.Value)
                .Include(d => d.ScheduleSlots)
                .OrderBy(d => d.Date)
                .ToList();

            //If no schedule currently, generate and recall method
            if (schedule.Count == 0)
            {
                GenerateSchedule(userId, start, end.GetValueOrDefault(start.AddDays(6)));
                return GetSchedule(userId, start, end);
            }

            return schedule;
        }

        public void GenerateSchedule(int userId, DateTime start, DateTime end)
        {
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);
            
            //TODO: support multiple diet goals
            var dietGoal = _dietService.GetDietGoalsByUserId(userId).FirstOrDefault();

            var daysForDiet = 7 - dietGoal.Current;

            var currentDay = new DateTime(start.Ticks, DateTimeKind.Utc);
            
            ClearSchedule(userId, start, end);

            while (currentDay.Ticks <= end.Ticks)
            {
                var scheduleDay = new ScheduleDay
                {
                    Date = currentDay,
                    DietTypeId = daysForDiet > 0 ? dietGoal.TargetDietId : 1,
                    UserId = userId,
                    ScheduleSlots = new List<ScheduleSlot>()
                };

                //TODO: Remove hard-coded meal-types
                if (preference.MealTypes == null)
                {
                    preference.MealTypes = new List<Meal.Type>
                    {
                        Meal.Type.Lunch
                    };
                }

                foreach (var mealType in preference.MealTypes)
                {
                    var meal = GetRandomMeal(mealType, scheduleDay.DietTypeId);
                    if (meal == null)
                    {
                        throw new Exception("No meals for type " + mealType);
                    }

                    var slot = new ScheduleSlot
                    {
                        MealId = meal.Id,
                        Type = meal.MealType,
                    };

                    scheduleDay.ScheduleSlots.Add(slot);
                }

                _dbContext.ScheduleDays.Add(scheduleDay);

                currentDay = currentDay.AddDays(1);
            }

            _dbContext.SaveChanges();
        }

        public ScheduleDayDto ToScheduleDayDto(ScheduleDay day)
        {
            return new ScheduleDayDto
            {
                Date = day.Date,
                LastModified = day.Modified,
                DietType = day.DietType?.Name,
                ScheduleSlots = day.ScheduleSlots.Select(ToScheduleSlotDto).ToList()
            };
        }

        public ScheduleSlotDto ToScheduleSlotDto(ScheduleSlot slot)
        {
            return new ScheduleSlotDto
            {
                Id = slot.Id,
                MealType = slot.Type.ToString(),
                RecipeId = slot.MealId
            };
        }

        private void ClearSchedule(int userId, DateTime start, DateTime? end)
        {
            var days = _dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && (!end.HasValue || d.Date <= end.Value))
                .ToList();
            var dayIds = days.Select(d => d.Id).ToList();

            var slots = _dbContext.ScheduleSlots.Where(s => dayIds.Contains(s.ScheduleDayId));

            _dbContext.ScheduleDays.RemoveRange(days);
            _dbContext.ScheduleSlots.RemoveRange(slots);
            _dbContext.SaveChanges();
        }

        private Meal GetRandomMeal(Meal.Type mealType, int dietTypeId = 0)
        {
            var meals = _dbContext.Meals.Include(m => m.MealDietTypes).ThenInclude(mdt => mdt.DietType)
                .Where(m => m.MealType == mealType && dietTypeId == 0 || m.MealDietTypes.Any(mdt => mdt.DietTypeId == dietTypeId));
            var index = _rand.Next(meals.Count());

            return meals.Skip(index).FirstOrDefault();
        }
    }
}
