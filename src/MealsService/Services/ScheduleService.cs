using MealsService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Responses;

namespace MealsService.Services
{
    public class ScheduleService
    {
        private MealsDbContext _dbContext;

        public ScheduleService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ScheduleDay> GetSchedule(int userId, DateTime start, DateTime? end = null)
        {
            if (!end.HasValue)
            {
                end = start.AddDays(7);
            }
            
            return _dbContext.ScheduleDays.Where(d => d.UserId == userId && d.Date >= start && d.Date <= end.Value)
                .Include(d => d.ScheduleSlots)
                    .ThenInclude(s => s.Meal)
                        .ThenInclude(m => m.MealIngredients)
                            .ThenInclude(mi => mi.Ingredient)
                .Include(d => d.DietType)
                .OrderBy(d => d.Date)
                .ToList();
        }
        public ScheduleDayDto ToScheduleDayDto(ScheduleDay day)
        {
            return new ScheduleDayDto
            {
                Date = day.Date,
                LastModified = day.Modified,
                DietType = day.DietType.Name,
                ScheduleSlots = day.ScheduleSlots.Select(ToScheduleSlotDto).ToList()
            };
        }

        public ScheduleSlotDto ToScheduleSlotDto(ScheduleSlot slot)
        {
            return new ScheduleSlotDto
            {
                Id = slot.Id,
                MealType = slot.Type.ToString(),
                Meal = ToMealDto(slot.Meal)
            };
        }

        public MealDto ToMealDto(Meal meal)
        {
            return new MealDto
            {
                Id = meal.Id,
                Name = meal.Name,
                Brief = meal.Brief,
                Description = meal.Description,
                Image = meal.Image,
                CookTime = meal.CookTime,
                PrepTime = meal.PrepTime,
                MealType = meal.MealType.ToString(),
                Ingredients = meal.MealIngredients.Select(ToMealIngredientDto).ToList()
            };
        }

        public MealIngredientDto ToMealIngredientDto(MealIngredient mealIngredient)
        {
            return new MealIngredientDto
            {
                Id = mealIngredient.IngredientId,
                Quantity = mealIngredient.Amount,
                Measure = mealIngredient.AmountType,
                Name = mealIngredient.Ingredient.Name
            };
        }
    }
}
