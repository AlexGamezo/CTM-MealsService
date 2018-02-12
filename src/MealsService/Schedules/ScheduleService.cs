using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;
using MealsService.Diets;
using MealsService.Models;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Responses.Schedules;
using MealsService.ShoppingList;

namespace MealsService.Services
{
    public class ScheduleService
    {
        private MealsDbContext _dbContext;

        private DietService _dietService;
        private IServiceProvider _serviceProvider;

        private Random _rand;

        public ScheduleService(MealsDbContext dbContext, DietService dietService, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;

            _dietService = dietService;
            _serviceProvider = serviceProvider;

            _rand = new Random();
        }

        public DateTime GetWeekStart(DateTime? when = null)
        {
            if (!when.HasValue)
            {
                when = DateTime.UtcNow;
            }

            when = when.Value.Date;

            var days = (int)when.Value.DayOfWeek - 1;
            if (days < 0) days += 7;

            return when.Value.Subtract(new TimeSpan(days, 0, 0, 0));
        }

        public List<ScheduleDay> GetSchedule(int userId, DateTime start, DateTime? end = null, bool regenIfEmpty = true)
        {
            if (!end.HasValue)
            {
                end = start.AddDays(6);
            }
            
            var schedule = _dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && d.Date <= end.Value)
                .Include(d => d.ScheduleSlots)
                .OrderBy(d => d.Date)
                .ToList();

            //If no schedule currently and not a past week, generate and recall method
            if (schedule.Count == 0 && end > DateTime.UtcNow && regenIfEmpty)
            {
                GenerateSchedule(userId, start, end.GetValueOrDefault(start.AddDays(6)), new GenerateScheduleRequest());
                return GetSchedule(userId, start, end, false);
            }

            return schedule;
        }

        public bool MoveSlot(int userId, int slotId, int dayId)
        {
            var currentSlot = _dbContext.ScheduleSlots.Include(s => s.ScheduleDay)
                .FirstOrDefault(s => s.Id == slotId);

            if (currentSlot == null)
            {
                return false;
            }

            var weekStart = GetWeekStart(currentSlot.ScheduleDay.Date);

            var schedule = GetSchedule(userId, weekStart);
            var targetDay = schedule.FirstOrDefault(d => dayId != currentSlot.ScheduleDayId && d.Id == dayId);
            if (targetDay == null || targetDay.DietTypeId != 0)
            {
                return false;
            }

            var oldDay = currentSlot.ScheduleDay;
            oldDay.DietTypeId = 0;
            
            currentSlot.ScheduleDay = null;
            currentSlot.ScheduleDayId = targetDay.Id;
            _dbContext.SaveChanges();

            return true;
        }

        public ScheduleDayDto AddChallengeDay(int userId, DateTime date)
        {
            var scheduleDay = _dbContext.ScheduleDays
                .Include(d => d.ScheduleSlots)
                .FirstOrDefault(s => s.UserId == userId && s.Date == date);

            if (scheduleDay == null || !(scheduleDay.ScheduleSlots == null || !scheduleDay.ScheduleSlots.Any()))
            {
                return null;
            }

            var goal = _dietService.GetDietGoalsByUserId(userId, date)?.FirstOrDefault();
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);

            scheduleDay.DietTypeId = goal.TargetDietId;
            var slots = new List<ScheduleSlot>();

            foreach (var mealType in preference.MealTypes)
            {
                var slot = new ScheduleSlot
                {
                    ScheduleDayId = scheduleDay.Id,
                    IsChallenge = true,
                    ConfirmStatus = ConfirmStatus.UNSET,
                    Type = mealType,
                };
                slots.Add(slot);
            }
            _dbContext.ScheduleSlots.AddRange(slots);
            _dbContext.SaveChanges();

            foreach (var slot in slots)
            {
                RegenerateSlot(userId, slot.Id);
            }

            var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
            shoppingListService.HandleSlotsAdded(userId, slots, GetWeekStart(date));

            return ToScheduleDayDto(scheduleDay);
        }

        public ScheduleDayDto RemoveChallengeDay(int userId, DateTime date)
        {
            var scheduleDay = _dbContext.ScheduleDays
                .Include(d => d.ScheduleSlots)
                .FirstOrDefault(s => s.UserId == userId && s.Date == date);

            if (scheduleDay == null || scheduleDay.ScheduleSlots == null || !scheduleDay.ScheduleSlots.Any() ||
                scheduleDay.DietTypeId == 0 || scheduleDay.ScheduleSlots.Any(s => !s.IsChallenge))
            {
                return null;
            }

            var shoppingListService = (ShoppingListService) _serviceProvider.GetService(typeof(ShoppingListService));
            shoppingListService.HandleSlotsRemoved(userId, scheduleDay.ScheduleSlots);

            scheduleDay.DietTypeId = 0;
            _dbContext.ScheduleSlots.RemoveRange(scheduleDay.ScheduleSlots);
            scheduleDay.ScheduleSlots = new List<ScheduleSlot>();
            _dbContext.SaveChanges();

            return ToScheduleDayDto(scheduleDay);
        }

        public ScheduleSlotDto RegenerateSlot(int userId, int slotId, bool updateShoppingList = true)
        {
            var slot = _dbContext.ScheduleSlots
                .Include(s => s.ScheduleDay)
                .FirstOrDefault(s => s.Id == slotId);

            if (slot?.ScheduleDay.UserId != userId)
            {
                return null;
            }

            var weekBeginning = GetWeekStart(slot.ScheduleDay.Date);
            if (updateShoppingList && slot.MealId > 0)
            {
                var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                shoppingListService.HandleSlotsRemoved(userId, new List<ScheduleSlot> { slot });
            }

            //TODO: Add tracking for individual slot regeneration

            var randomRecipeRequest = new RandomRecipeRequest
            {
                DietTypeId = slot.ScheduleDay.DietTypeId,
                MealType = slot.Type,
            };

            var myVotes = _dbContext.RecipeVotes
                .Where(v => v.UserId == userId && v.Vote != RecipeVote.VoteType.UNKNOWN)
                .ToList();

            var recipeWeights = new Dictionary<int, int>
            {
                {slot.MealId, 1}
            };

            //TODO: Do a better job of preferring Liked/Hated recipes
            foreach (var vote in myVotes)
            {
                if (!recipeWeights.ContainsKey(vote.RecipeId))
                {
                    recipeWeights.Add(vote.RecipeId, 100 * (vote.Vote == RecipeVote.VoteType.LIKE ? -1 : 1));
                }
            }
            
            //TODO: preference recipes not present this week
            //TODO: Pull meal preferences to filter for style of recipe (Quick&Dirty, Healthy, etc)
            var recipe = GetRandomMeal(randomRecipeRequest, recipeWeights);
            
            if (recipe != null && recipe.Id != slot.MealId)
            {
                slot.MealId = recipe.Id;
                if (_dbContext.SaveChanges() > 0)
                {
                    if (updateShoppingList)
                    {
                        var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                        shoppingListService.HandleSlotsAdded(userId, new List<ScheduleSlot> { slot }, weekBeginning);
                    }
                    return ToScheduleSlotDto(slot);
                }
            }

            return null;
        }

        public void GenerateSchedule(int userId, DateTime start, DateTime end, GenerateScheduleRequest request)
        {
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);
            
            //TODO: support multiple diet goals
            var dietGoal = _dietService.GetDietGoalsByUserId(userId).FirstOrDefault();
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

            //TODO: This should be a RecipeService method
            //TODO: Take into account schedule for days/weeks before and after this timeframe
            var myVotes = _dbContext.RecipeVotes
                .Where(v => v.UserId == userId)
                .Select(v => v.RecipeId)
                .ToList();

            var usedRecipeCounts = new Dictionary<int, int>();
            foreach (var vote in myVotes)
            {
                usedRecipeCounts.Add(vote, 100);
            }

            var randomRecipeRequest = new RandomRecipeRequest
            {
                ExcludeTags = request.ExcludeTags,
                ConsumeIngredients = request.RecipeIngredients
            };

            var changeDays = _dietService.GetChangeDays(userId, start);

            while (currentDay.Ticks <= end.Ticks)
            {
                //Weeks start on Monday
                var currentDOW = (int) currentDay.DayOfWeek - 1;
                if (currentDOW < 0) currentDOW += 7;

                var scheduleDay = new ScheduleDay
                {
                    Date = currentDay,
                    DietTypeId = changeDays.Contains(currentDOW) ? dietGoal.TargetDietId : 0,
                    UserId = userId,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };

                if (scheduleDay.DietTypeId > 0)
                {
                    scheduleDay.ScheduleSlots = new List<ScheduleSlot>();
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
                }

                _dbContext.ScheduleDays.Add(scheduleDay);
                currentDay = currentDay.AddDays(1);
            }

            _dbContext.SaveChanges();
        }

        public bool ConfirmDay(int userId, int scheduleSlotId, ConfirmStatus confirm)
        {
            var slot = _dbContext.ScheduleSlots
                .Where(s => s.Id == scheduleSlotId)
                .Include(s => s.ScheduleDay)
                .FirstOrDefault();

            if (slot == null || slot.ScheduleDay.UserId != userId)
            {
                return false;
            }
            
            slot.ConfirmStatus = confirm;
            
            return _dbContext.Entry(slot).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }


        public ScheduleDayDto ToScheduleDayDto(ScheduleDay day)
        {
            return new ScheduleDayDto
            {
                Id = day.Id,
                Date = day.Date,
                LastModified = day.Modified,
                DietType = day.DietType?.Name,
                DietTypeId = day.DietTypeId,
                IsChallenge = day.ScheduleSlots.Any(s => s.IsChallenge),
                ScheduleSlots = day.ScheduleSlots.Select(ToScheduleSlotDto).ToList()
            };
        }

        public ScheduleSlotDto ToScheduleSlotDto(ScheduleSlot slot)
        {
            return new ScheduleSlotDto
            {
                Id = slot.Id,
                MealType = slot.Type.ToString(),
                RecipeId = slot.MealId,
                Confirmed = slot.ConfirmStatus,
                ScheduleDayId = slot.ScheduleDayId,
                IsChallenge = slot.IsChallenge
            };
        }

        private void ClearSchedule(int userId, DateTime start, DateTime? end)
        {
            var days = _dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && (!end.HasValue || d.Date <= end.Value))
                .ToList();
            var dayIds = days.Select(d => d.Id).ToList();

            var slots = _dbContext.ScheduleSlots.Where(s => dayIds.Contains(s.ScheduleDayId)).ToList();

            ((ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService))).ClearShoppingList(userId, start);

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

            var meal = _dbContext.Meals
                .Include(m => m.MealDietTypes)
                    .ThenInclude(mdt => mdt.DietType)
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
