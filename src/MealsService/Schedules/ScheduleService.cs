using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;
using MealsService.Diets;
using MealsService.Ingredients.Data;
using MealsService.Recipes;
using MealsService.Models;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Responses.Schedules;

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
                GenerateSchedule(userId, start, end.GetValueOrDefault(start.AddDays(6)), new GenerateScheduleRequest());
                return GetSchedule(userId, start, end);
            }

            return schedule;
        }

        public void GenerateSchedule(int userId, DateTime start, DateTime end, GenerateScheduleRequest request)
        {
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);
            
            //TODO: support multiple diet goals
            var dietGoal = _dietService.GetDietGoalsByUserId(userId).FirstOrDefault();
            var daysForDiet = 7 - _dietService.GetTargetForDiet(userId, dietGoal.TargetDietId);
            var currentDay = new DateTime(start.Ticks, DateTimeKind.Utc);
            
            ClearSchedule(userId, start, end);

            _dbContext.ScheduleGenerations.Add(new ScheduleGenerated
            {
                UserId = userId,
                StartDate = start,
                EndDate = end,
                Created = DateTime.UtcNow
            });
            
            _dbContext.SaveChanges();

            //TODO: Take into account likes/dislikes and schedule for days/weeks before and after this timeframe
            var usedRecipeCounts = new Dictionary<int, int>();
            var randomRecipeRequest = new RandomRecipeRequest
            {
                ExcludeTags = request.ExcludeTags,
                ConsumeIngredients = request.RecipeIngredients
            };

            while (currentDay.Ticks <= end.Ticks)
            {
                var scheduleDay = new ScheduleDay
                {
                    Date = currentDay,
                    DietTypeId = daysForDiet > 0 ? dietGoal.TargetDietId : preference.CurrentDietId,
                    UserId = userId,
                    ScheduleSlots = new List<ScheduleSlot>(),
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                randomRecipeRequest.DietTypeId = scheduleDay.DietTypeId;

                foreach (var mealType in preference.MealTypes)
                {
                    var slot = new ScheduleSlot
                    {
                        Type = mealType,
                    };

                    randomRecipeRequest.MealType = mealType;
                    var meal = GetRandomMeal(randomRecipeRequest, usedRecipeCounts);

                    if (meal != null)
                    {
                        if (!usedRecipeCounts.ContainsKey(meal.Id))
                        {
                            usedRecipeCounts.Add(meal.Id, 0);
                        }
                        usedRecipeCounts[meal.Id]++;

                        slot.MealId = meal.Id;
                    }
                    scheduleDay.ScheduleSlots.Add(slot);
                }

                _dbContext.ScheduleDays.Add(scheduleDay);

                daysForDiet--;
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

        private Meal GetRandomMeal(RandomRecipeRequest request, Dictionary<int, int> recipeWeights = null)
        {
            request.ExcludeTags = request.ExcludeTags?.Select(g => g.ToLower()).ToList();

            //TODO: Add tags to recipes, specific to the recipe, but also pulled up from the ingredients
            //TODO: Refactor to use recipe's tags instead of ingredients
            //TODO: Don't count optional ingredients, once there is such a thing
            var excludedIngredientIds = new List<int>();
            
            if (request.ExcludeTags != null)
            {
                excludedIngredientIds = _dbContext.Ingredients
                    .Include(i => i.IngredientTags)
                        .ThenInclude(it => it.Tag)
                    .Where(i => i.Tags.Any() && request.ExcludeTags.Any(c => i.Tags.Any(t => t == c)))
                    .Select(i => i.Id)
                    .ToList();
            }

            var consumeIngredientIds = request.ConsumeIngredients != null ? request.ConsumeIngredients.Select(ri => ri.IngredientId).ToList() : new List<int>();
            
            var sortedRecipes = _dbContext.Meals
                .Include(m => m.MealIngredients)
                .Where(m => m.MealType == request.MealType && (request.DietTypeId == 0 || m.MealDietTypes.Any(mdt => mdt.DietTypeId == request.DietTypeId)))
                //Exclude any recipes that have ingredients that were requested to be excluded
                .Where(m => m.MealIngredients.All(mi => !excludedIngredientIds.Contains(mi.IngredientId)))
                //Sort recipes that have the requested ingredients to the top
                .OrderBy(m => m.MealIngredients.Count(mi => consumeIngredientIds.Contains(mi.IngredientId)))
                //Preference the meals that haven't been used yet
                .ThenBy(m => recipeWeights != null && recipeWeights.ContainsKey(m.Id) ? recipeWeights[m.Id] : 0);

            var countDiff = sortedRecipes.Count() - sortedRecipes.Count(r => recipeWeights != null && recipeWeights.ContainsKey(r.Id));
            var index = countDiff > 0 ? _rand.Next(countDiff) : _rand.Next(sortedRecipes.Count());

            var recipeId = sortedRecipes.Skip(index).Select(m => m.Id).FirstOrDefault();

            var meal = _dbContext.Meals.Include(m => m.MealDietTypes).ThenInclude(mdt => mdt.DietType)
                .FirstOrDefault(m => m.Id == recipeId);
            
            //TODO: Add logger
            /*if (meal == null)
            {
                throw new Exception("No meals for type " + request.MealType);
            }*/

            return meal;
        }
    }
}
