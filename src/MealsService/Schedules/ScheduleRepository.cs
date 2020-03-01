using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Schedules.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Schedules
{
    public class ScheduleRepository
    {
        private IServiceProvider _serviceContainer;

        public ScheduleRepository(IServiceProvider serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public List<ScheduleDay> GetSchedule(int userId, DateTime start, DateTime end)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var meals = dbContext.Meals.Where(m =>
                m.ScheduleDay.UserId == userId &&
                m.ScheduleDay.Date >= start && m.ScheduleDay.Date <= end);
            meals.Load();

            var preparations = dbContext.Preparations.Where(p => p.ScheduleDay.UserId == userId && p.ScheduleDay.Date >= start && p.ScheduleDay.Date <= end);
            preparations.Load();

            var schedule = dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && d.Date <= end)
                .OrderBy(d => d.Date)
                .ToList();

            return schedule;
        }

        public Meal GetMeal(int slotId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.Meals.Include(s => s.ScheduleDay)
                .Include(m => m.Preparation)
                    .ThenInclude(p => p.ScheduleDay)
                .Include(m => m.Preparation)
                    .ThenInclude(p => p.Meals)
                .FirstOrDefault(s => s.Id == slotId);
        }

        public Preparation GetPreparation(int prepId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.Preparations
                .Include(s => s.ScheduleDay)
                .Include(p => p.Meals)
                .FirstOrDefault(s => s.Id == prepId);
        }

        public bool SetPreparationRecipeId(int prepId, int recipeId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            Preparation prep;

            if (dbContext.ChangeTracker.Entries<Preparation>().FirstOrDefault(m => m.Entity.Id == prepId) == null)
            {
                prep = new Preparation
                {
                    Id = prepId,
                };
                dbContext.Add(prep);
                dbContext.Preparations.Include(p => p.Meals).Load();
            }
            else
            {
                prep = dbContext.Preparations
                    .Include(p => p.Meals)
                    .FirstOrDefault(p => p.Id == prepId);
            }
            
            prep.RecipeId = recipeId;
            prep.Meals.ForEach(m => m.RecipeId = recipeId);

            return dbContext.SaveChanges() > 0;
        }

        public bool SetMealServings(int slotId, int numServings)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            if (dbContext.ChangeTracker.Entries<Meal>().FirstOrDefault(m => m.Entity.Id == slotId) == null)
            {
                var meal = new Meal
                {
                    Id = slotId,
                    Servings = numServings
                };

                dbContext.Meals.Attach(meal);
                dbContext.Entry(meal).Property(p => p.Servings).IsModified = true;
            }
            else
            {
                dbContext.Meals.First(m => m.Id == slotId).Servings = numServings;
            }
            
            return dbContext.SaveChanges() > 0;
        }

        public bool SetConfirmState(int slotId, ConfirmStatus confirmStatus)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            
            if (dbContext.ChangeTracker.Entries<Meal>().FirstOrDefault(m => m.Entity.Id == slotId) == null)
            {
                var meal = new Meal
                {
                    Id = slotId,
                    ConfirmStatus = confirmStatus
                };
                dbContext.Meals.Attach(meal);
                dbContext.Entry(meal).Property(p => p.ConfirmStatus).IsModified = true;

            }
            else
            {
                var meal = dbContext.Meals.First(m => m.Id == slotId);
                meal.ConfirmStatus = confirmStatus;
            }

            return dbContext.SaveChanges() > 0;
        }

        public bool TrackScheduleGeneration(int userId, DateTime start, DateTime end, bool regeneration = false)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            dbContext.ScheduleGenerations.Add(new ScheduleGenerated
            {
                UserId = userId,
                StartDate = start,
                EndDate = end,
                Created = DateTime.UtcNow
            });

            return dbContext.SaveChanges() > 0;
        }

        public bool SaveScheduleDays(List<ScheduleDay> days)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            foreach (var day in days)
            {
                if (day.Id > 0)
                {
                    var tracked = dbContext.ChangeTracker.Entries<ScheduleDay>()
                        .FirstOrDefault(m => m.Entity.Id == day.Id);
                    if (tracked != null)
                    {
                        dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                    }
                    dbContext.ScheduleDays.Attach(day);
                    dbContext.Entry(day).State = EntityState.Modified;

                    dbContext.Entry(day).Property(d => d.Created).IsModified = false;
                }
                else
                {
                    dbContext.ScheduleDays.Add(day);
                }
                
            }

            return dbContext.SaveChanges() > 0;
        }

        public bool RemovePreparations(List<int> prepIds)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            //TODO: This should be a getter
            var preparations = dbContext.Preparations
                .Include(p => p.Meals)
                .Include(p => p.ScheduleDay)
                    .ThenInclude(d => d.Meals)
                .Where(p => prepIds.Contains(p.Id))
                .ToList();

            if (!preparations.Any())
            {
                return false;
            }

            dbContext.Preparations.RemoveRange(preparations);
            dbContext.Meals.RemoveRange(preparations.SelectMany(p => p.Meals));

            foreach (var prep in preparations)
            {
                if (prep.ScheduleDay.Meals.All(m => prepIds.Contains(m.PreparationId)))
                {
                    prep.ScheduleDay.DietTypeId = 0;
                }
            }

            return dbContext.SaveChanges() > 0;
        }

        public bool ClearSchedule(int userId, DateTime start, DateTime? end)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var days = dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && (!end.HasValue || d.Date <= end.Value))
                .ToList();
            
            dbContext.ScheduleDays.RemoveRange(days);
            return dbContext.SaveChanges() > 0;
        }

        //TODO: Validate MoveMeal and MovePrep functionality!!!
        public bool MoveMeal(int mealId, int targetDayId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var meal = dbContext.Meals
                .Include(m => m.ScheduleDay)
                .ThenInclude(d => d.Meals)
                .FirstOrDefault(m => m.Id == mealId);

            if (meal == null || meal.ScheduleDayId == targetDayId)
            {
                return false;
            }

            if (meal.ScheduleDay.Meals.All(m => m.Id == mealId))
            {
                meal.ScheduleDay.DietTypeId = 0;
            }
            
            meal.ScheduleDayId = targetDayId;
            return dbContext.SaveChanges() > 0;
        }

        //TODO: Validate MoveMeal and MovePrep functionality!!!
        public bool MovePrep(int prepId, int targetDayId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var preparation = dbContext.Preparations
                .Include(p => p.ScheduleDay)
                .ThenInclude(d => d.Meals)
                .FirstOrDefault(p => p.Id == prepId);

            if (preparation == null || preparation.ScheduleDayId == targetDayId)
            {
                return false;
            }

            var oldMeals = preparation.Meals.Where(m => m.ScheduleDayId == preparation.ScheduleDayId);

            foreach (var meal in oldMeals)
            {
                MoveMeal(meal.Id, targetDayId);
            }
            
            if (preparation.ScheduleDay.Meals.All(m => m.PreparationId == prepId))
            {
                preparation.ScheduleDay.DietTypeId = 0;
            }

            //Find earliest meal for this preparation to make that the prep day
            var targetPrepDay = preparation.Meals
                .OrderBy(m => m.ScheduleDay.Date)
                .First();

            preparation.ScheduleDayId = targetPrepDay.Id;
            return dbContext.SaveChanges() > 0;
        }
    }
}
