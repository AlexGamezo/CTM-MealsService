using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Common.Extensions;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;
using MealsService.Diets;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Responses.Schedules;
using MealsService.Schedules.Data;
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

        public List<ScheduleDay> GetSchedule(int userId, DateTime start, DateTime? end = null, bool regenIfEmpty = true)
        {
            if (!end.HasValue)
            {
                end = start.AddDays(6);
            }
            
            var schedule = _dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && d.Date <= end.Value)
                .Include(d => d.Preparations)
                    .ThenInclude(p => p.Meals)
                .Include(d => d.Meals)
                .OrderBy(d => d.Date)
                .ToList();

            //If no schedule currently and not a past week, generate and recall method
            if (schedule.Count == 0 && end >= DateTime.UtcNow.Date && regenIfEmpty)
            {
                GenerateSchedule(userId, start, end.GetValueOrDefault(start.AddDays(6)), new GenerateScheduleRequest());
                return GetSchedule(userId, start, end, false);
            }

            return schedule;
        }

        public bool MoveMeal(int userId, int slotId, int dayId)
        {
            var currentMeal = _dbContext.Meals.Include(s => s.ScheduleDay)
                .Include(m => m.Preparation)
                    .ThenInclude(p => p.Meals)
                .FirstOrDefault(s => s.Id == slotId);

            if (currentMeal == null)
            {
                return false;
            }

            if (currentMeal.ScheduleDay.UserId != userId)
            {
                return false;
                //TODO: throw appropriate exception
            }

            //Can't move a confirmed meal
            if (currentMeal.ConfirmStatus == ConfirmStatus.CONFIRMED_YES)
            {
                return false;
                //TODO: throw appropriate exception
            }


            var weekStart = currentMeal.ScheduleDay.Date.GetWeekStart();

            var schedule = GetSchedule(userId, weekStart);
            
            var targetDay = schedule.FirstOrDefault(d => dayId != currentMeal.ScheduleDayId && d.Id == dayId);
            if (targetDay == null || targetDay.DietTypeId != 0)
            {
                return false;
            }

            /*  Not sure if this is needed. This may just be something we want to confirm client-side before allowing
            //If any of the meals for this prep have been confirmed for a later date than the move, don't allow move
            var prepDays = schedule.Where(d => d.Meals.Any(m => m.PreparationId == currentMeal.PreparationId)).ToList();
            if (prepDays.Any(d => d.Meals.Any(m =>
                m.PreparationId == currentMeal.PreparationId && m.ConfirmStatus == ConfirmStatus.CONFIRMED_YES &&
                d.Date > targetDay.Date)))
            {
                return false;
            }
            */

            var oldDay = currentMeal.ScheduleDay;
            oldDay.DietTypeId = 0;

            currentMeal.ScheduleDay = null;
            currentMeal.ScheduleDayId = targetDay.Id;
            _dbContext.SaveChanges();

            return true;
        }

        public bool MovePreparation(int userId, int prepId, int dayId)
        {
            var currentPrep = _dbContext.Preparations
                .Include(s => s.ScheduleDay)
                .Include(p => p.Meals)
                .FirstOrDefault(s => s.Id == prepId);

            if (currentPrep == null)
            {
                return false;
            }

            if (currentPrep.ScheduleDay.UserId != userId)
            {
                return false;
                //TODO: throw appropriate exception
            }

            var weekStart = currentPrep.ScheduleDay.Date.GetWeekStart();

            var schedule = GetSchedule(userId, weekStart);
            var targetDay = schedule.FirstOrDefault(d => dayId != currentPrep.ScheduleDayId && d.Id == dayId);
            //Prevent collapsing days
            if (targetDay == null || targetDay.DietTypeId != 0)
            {
                return false;
            }

            var oldDay = currentPrep.ScheduleDay;
            var oldMeals = currentPrep.Meals.Where(m => m.ScheduleDayId == currentPrep.ScheduleDayId);
            oldDay.DietTypeId = 0;

            currentPrep.ScheduleDay = null;
            foreach (var meal in oldMeals)
            {
                meal.ScheduleDay = targetDay;
                meal.ScheduleDayId = targetDay.Id;
            }

            //Find the earlist meal for this preparation, that will be the new preparation, if one(s) being moved are to after it
            var targetPrepDay = schedule.Where(d => currentPrep.Meals.Select(m => m.ScheduleDayId).Contains(d.Id))
                .OrderBy(d => d.Date).First();

            currentPrep.ScheduleDayId = targetDay.Date < targetPrepDay.Date ? targetDay.Id : targetPrepDay.Id;

            _dbContext.SaveChanges();

            return true;
        }

        public ScheduleDayDto AddChallengeDay(int userId, DateTime date)
        {
            var scheduleDay = _dbContext.ScheduleDays
                .Include(d => d.Meals)
                .FirstOrDefault(s => s.UserId == userId && s.Date == date);

            if (scheduleDay == null || !(scheduleDay.Meals == null || !scheduleDay.Meals.Any()))
            {
                return null;
            }

            var goal = _dietService.GetDietGoalsByUserId(userId, date)?.FirstOrDefault();
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);

            scheduleDay.DietTypeId = goal.TargetDietId;
            var preparations = new List<Preparation>();

            foreach (var mealType in preference.MealTypes)
            {
                var prep = new Preparation
                {
                    UserId = userId,
                    ScheduleDayId = scheduleDay.Id,
                    MealType = mealType,
                    Meals = new List<Meal>
                    {
                        new Meal
                        {
                            ScheduleDayId = scheduleDay.Id,
                            IsChallenge = true,
                            ConfirmStatus = ConfirmStatus.UNSET,
                            Type = mealType,
                        }
                    }
                };
                preparations.Add(prep);
            }
            _dbContext.Preparations.AddRange(preparations);
            _dbContext.SaveChanges();

            foreach (var prep in preparations)
            {
                RegeneratePreparation(userId, prep.Id);
            }

            var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
            shoppingListService.HandlePreparationsAdded(userId, preparations, date.GetWeekStart());

            return ToScheduleDayDto(scheduleDay);
        }

        public ScheduleDayDto RemoveChallengeDay(int userId, DateTime date)
        {
            var scheduleDay = _dbContext.ScheduleDays
                .Include(d => d.Preparations)
                    .ThenInclude(p => p.Meals)
                .Include(d => d.Meals)
                .FirstOrDefault(s => s.UserId == userId && s.Date == date);

            if (scheduleDay?.Meals == null || !scheduleDay.Meals.Any() ||
                scheduleDay.DietTypeId == 0 || scheduleDay.Meals.Any(s => !s.IsChallenge))
            {
                return null;
            }

            var shoppingListService = (ShoppingListService) _serviceProvider.GetService(typeof(ShoppingListService));
            shoppingListService.HandlePreparationsRemoved(userId, scheduleDay.Preparations);

            scheduleDay.DietTypeId = 0;
            _dbContext.Preparations.RemoveRange(scheduleDay.Preparations);
            _dbContext.Meals.RemoveRange(scheduleDay.Meals);
            scheduleDay.Meals = new List<Meal>();
            scheduleDay.Preparations= new List<Preparation>();
            _dbContext.SaveChanges();

            return ToScheduleDayDto(scheduleDay);
        }

        public bool RegeneratePreparation(int userId, int preparationId, bool updateShoppingList = true)
        {
            var preparation = _dbContext.Preparations
                .Include(p => p.ScheduleDay)
                .Include(p => p.Meals)
                .FirstOrDefault(s => s.Id == preparationId);

            if (preparation?.ScheduleDay.UserId != userId)
            {
                return false;
                //TODO: Throw appropriate exception
            }

            if (preparation.Meals.Any(m => m.ConfirmStatus == ConfirmStatus.CONFIRMED_YES))
            {
                return false;
                //TODO: Throw appropriate exception
            }

            var weekBeginning = preparation.ScheduleDay.Date.GetWeekStart();
            if (updateShoppingList && preparation.RecipeId > 0)
            {
                var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                shoppingListService.HandlePreparationsRemoved(userId, new List<Preparation>{preparation});
            }

            //TODO: Add tracking for individual slot regeneration

            var randomRecipeRequest = new RandomRecipeRequest
            {
                DietTypeId = preparation.ScheduleDay.DietTypeId,
                MealType = preparation.MealType,
            };

            var myVotes = _dbContext.RecipeVotes
                .Where(v => v.UserId == userId && v.Vote != RecipeVote.VoteType.UNKNOWN)
                .ToList();

            var recipeWeights = new Dictionary<int, int>
            {
                {preparation.RecipeId, 1}
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
            //TODO: Pull recipe preferences to filter for style of recipe (Quick&Dirty, Healthy, etc)
            var recipe = GetRandomRecipe(randomRecipeRequest, recipeWeights);
            
            if (recipe != null && recipe.Id != preparation.RecipeId)
            {
                preparation.RecipeId = recipe.Id;
                preparation.Meals.ForEach(m => m.RecipeId = recipe.Id);
                if (_dbContext.SaveChanges() > 0)
                {
                    if (updateShoppingList)
                    {
                        var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                        shoppingListService.HandlePreparationsAdded(userId, new List<Preparation> { preparation }, weekBeginning);
                    }
                    return true;
                }
            }

            return false;
        }

        public void GenerateSchedule(int userId, DateTime start, DateTime end, GenerateScheduleRequest request)
        {
            //Pull the preferences to know which recipes to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);
            
            var dietGoal = _dietService.GetDietGoalsByUserId(userId).FirstOrDefault();
            //TODO: This should be based on user's TZ
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
            
            var plan = _dietService.GetPrepPlan(userId, start);
            var generatorDays = plan.Generators.Select(g => g.DayOfWeek).ToList();
            var consumerDays = plan.Consumers.Select(c => c.DayOfWeek).ToList();

            var scheduleDays = new List<ScheduleDay>();

            while (currentDay.Ticks <= end.Ticks)
            {
                //Weeks start on Monday
                var currentDOW = (int) currentDay.DayOfWeek - 1;
                if (currentDOW < 0) currentDOW += 7;

                var hasMeal = consumerDays.Contains(currentDOW) || generatorDays.Contains(currentDOW);

                var scheduleDay = new ScheduleDay
                {
                    Date = currentDay,
                    DietTypeId = hasMeal ? dietGoal.TargetDietId : 0,
                    UserId = userId,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    Preparations = new List<Preparation>(),
                    Meals = new List<Meal>()
                };

                scheduleDays.Add(scheduleDay);
                currentDay = currentDay.AddDays(1);
            }

            foreach (var generator in plan.Generators)
            {
                var genDay = scheduleDays[generator.DayOfWeek];
                randomRecipeRequest.DietTypeId = genDay.DietTypeId;
                randomRecipeRequest.MealType = generator.MealType;

                var recipe = GetRandomRecipe(randomRecipeRequest, usedRecipeCounts);

                if (recipe != null)
                {
                    if (!usedRecipeCounts.ContainsKey(recipe.Id))
                    {
                        usedRecipeCounts.Add(recipe.Id, 0);
                    }
                    usedRecipeCounts[recipe.Id]++;

                    var prep = new Preparation
                    {
                        UserId = userId,
                        RecipeId = recipe.Id,
                        MealType = generator.MealType,
                        Meals = new List<Meal>()
                    };
                    genDay.Preparations.Add(prep);

                    foreach (var consumer in generator.Consumers)
                    {
                        var consumeDay = scheduleDays[consumer.DayOfWeek];
                        var meal = new Meal
                        {
                            RecipeId = recipe.Id,
                            Type = consumer.MealType,
                            Servings = consumer.NumServings,
                        };

                        consumeDay.Meals.Add(meal);
                        prep.Meals.Add(meal);
                    }
                }
            }

            _dbContext.ScheduleDays.AddRange(scheduleDays);
            _dbContext.SaveChanges();
        }

        public bool ConfirmMeal(int userId, int mealId, ConfirmStatus confirm)
        {
            var slot = _dbContext.Meals
                .Where(s => s.Id == mealId)
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
                IsChallenge = day.Meals.Any(s => s.IsChallenge),
                Meals = day.Meals.Select(ToMealDto).ToList()
            };
        }

        public MealDto ToMealDto(Meal meal)
        {
            return new MealDto
            {
                Id = meal.Id,
                MealType = meal.Type.ToString(),
                RecipeId = meal.RecipeId,
                Confirmed = meal.ConfirmStatus,
                ScheduleDayId = meal.ScheduleDayId,
                IsChallenge = meal.IsChallenge
            };
        }

        private void ClearSchedule(int userId, DateTime start, DateTime? end)
        {
            var days = _dbContext.ScheduleDays
                .Where(d => d.UserId == userId && d.Date >= start && (!end.HasValue || d.Date <= end.Value))
                .ToList();

            ((ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService))).ClearShoppingList(userId, start);

            _dbContext.ScheduleDays.RemoveRange(days);
            _dbContext.SaveChanges();
        }

        private Recipe GetRandomRecipe(RandomRecipeRequest request, Dictionary<int, int> recipeWeights = null)
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
            
            var sortedRecipes = _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                .Where(m => (m.MealType == MealType.Any || m.MealType == request.MealType) && (request.DietTypeId == 0 || m.RecipeDietTypes.Any(mdt => mdt.DietTypeId == request.DietTypeId)))
                //Exclude any recipes that have ingredients that were requested to be excluded
                .Where(m => m.RecipeIngredients.All(mi => !excludedIngredientIds.Contains(mi.IngredientId)))
                //Sort recipes that have the requested ingredients to the top
                .OrderBy(m => m.RecipeIngredients.Count(mi => consumeIngredientIds.Contains(mi.IngredientId)))
                //Preference the recipes that haven't been used yet
                .ThenBy(m => recipeWeights != null && recipeWeights.ContainsKey(m.Id) ? recipeWeights[m.Id] : 0);

            var countDiff = sortedRecipes.Count() - sortedRecipes.Count(r => recipeWeights != null && recipeWeights.ContainsKey(r.Id));
            var index = countDiff > 0 ? _rand.Next(countDiff) : _rand.Next(sortedRecipes.Count());

            var recipeId = sortedRecipes.Skip(index).Select(m => m.Id).FirstOrDefault();

            var recipe = _dbContext.Recipes
                .Include(m => m.RecipeDietTypes)
                    .ThenInclude(mdt => mdt.DietType)
                .FirstOrDefault(m => m.Id == recipeId);
            
            //TODO: Add logger
            /*if (recipe == null)
            {
                throw new Exception("No recipes for type " + request.MealType);
            }*/

            return recipe;
        }
    }
}
