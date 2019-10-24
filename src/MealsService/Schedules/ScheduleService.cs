using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
using MealsService.Diets;
using MealsService.Email;
using MealsService.Infrastructure;
using MealsService.Recipes;
using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Responses.Schedules;
using MealsService.Schedules;
using MealsService.Schedules.Data;
using MealsService.Schedules.Dtos;
using MealsService.ShoppingList;
using MealsService.Stats;
using MealsService.Users;
using MealsService.Users.Data;

namespace MealsService.Services
{
    public class ScheduleService
    {
        private DietService _dietService;
        private IRecipesService _recipesService;
        private IUserRecipesService _userRecipesService;
        private SubscriptionsService _subscriptionsService;
        private IServiceProvider _serviceProvider;
        private StatsService _statsService;
        private IMemcachedClient _memcached;

        private ScheduleRepository _scheduleRepo;

        private ILogger<ScheduleService> _logger;

        public ScheduleService(DietService dietService, IUserRecipesService userRecipesService, IRecipesService recipesService, SubscriptionsService subscriptionService,
            StatsService statsService, IServiceProvider serviceProvider)
        {
            _scheduleRepo = new ScheduleRepository(serviceProvider);

            _dietService = dietService;
            _recipesService = recipesService;
            _userRecipesService = userRecipesService;
            _subscriptionsService = subscriptionService;
            _serviceProvider = serviceProvider;
            _statsService = statsService;

            _memcached = _serviceProvider.GetService<IMemcachedClient>();

            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<ScheduleService>();
        }

        public async Task<List<ScheduleDayDto>> GetScheduleAsync(int userId, LocalDate start, LocalDate? end, bool regenIfEmpty = true)
        {
            await _subscriptionsService.VerifyDateInSubscriptionAsync(userId, start);

            var schedule = await _memcached.GetValueOrCreateAsync(
                CacheKeys.Schedule.UserSchedule(userId, start.GetWeekStart().ToDateTimeUnspecified()), 60,
                async () => (await GetScheduleInternalAsync(userId, start.GetWeekStart(), end, regenIfEmpty))
                    .Select(ToScheduleDayDto).ToList());
            
            return schedule;
        }

        public PreparationDto GetPreparation(int userId, int preparationId)
        {
            var currentPrep = _scheduleRepo.GetPreparation(preparationId);

            if (currentPrep == null)
            {
                throw ScheduleErrors.MissingPreparation;
            }

            if (currentPrep.UserId != userId)
            {
                throw ScheduleErrors.InvalidTargetByUser;
            }

            return ToPreparationDto(currentPrep);
        }

        public async Task<bool> MoveMealAsync(int userId, int slotId, int dayId)
        {
            //TODO: Verify subscription to allow access to future slots

            var currentMeal = _scheduleRepo.GetMeal(slotId);

            if (currentMeal == null)
            {
                throw ScheduleErrors.MissingMeal;
            }

            if (currentMeal.ScheduleDay.UserId != userId)
            {
                throw ScheduleErrors.InvalidTargetByUser;
            }

            if (currentMeal.ConfirmStatus == ConfirmStatus.CONFIRMED_YES)
            {
                throw ScheduleErrors.CantMoveConfirmedMeal;
            }

            var weekStart = currentMeal.ScheduleDay.NodaDate.GetWeekStart();
            var weekEnd = weekStart.GetWeekEnd();

            var schedule = await GetScheduleAsync(userId, weekStart, weekEnd);
            
            var targetDay = schedule.FirstOrDefault(d => dayId != currentMeal.ScheduleDayId && d.Id == dayId);
            //TODO: this prevents replacing a meal, need to support swapping or force-replacing
            if (targetDay == null || targetDay.DietTypeId != 0)
            {
                throw ScheduleErrors.CantMoveMealOutsideWeek;
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

            _scheduleRepo.MoveMeal(currentMeal.Id, dayId);

            //TODO: If we allow moving a meal across weeks, we'll need to clear cache for both weeks
            ClearScheduleCache(userId, weekStart);

            return true;
        }

        public async Task SetPreparationRecipeAsync(int userId, int prepId, int recipeId)
        {
            var currentPrep = _scheduleRepo.GetPreparation(prepId);

            if (currentPrep == null)
            {
                throw ScheduleErrors.MissingPreparation;
            }

            if (currentPrep.ScheduleDay.UserId != userId)
            {
                throw ScheduleErrors.InvalidTargetByUser;
            }

            var weekStart = currentPrep.ScheduleDay.NodaDate.GetWeekStart();

            var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
            await shoppingListService.GetShoppingListAsync(userId, weekStart);

            shoppingListService.HandlePreparationsRemoved(userId, new List<PreparationDto> { ToPreparationDto(currentPrep) });

            _scheduleRepo.SetPreparationRecipeId(prepId, recipeId);

            currentPrep = _scheduleRepo.GetPreparation(prepId);

            shoppingListService.HandlePreparationsAdded(userId, new List<PreparationDto> { ToPreparationDto(currentPrep) }, weekStart);

            ClearScheduleCache(userId, weekStart);
        }

        public async Task<bool> MovePreparationAsync(int userId, int prepId, int dayId)
        {
            //TODO: Verify subscription to allow access to future slots

            var currentPrep = _scheduleRepo.GetPreparation(prepId);

            if (currentPrep == null)
            {
                throw ScheduleErrors.MissingPreparation;
            }

            if (currentPrep.ScheduleDay.UserId != userId)
            {
                throw ScheduleErrors.InvalidTargetByUser;
            }

            var weekStart = currentPrep.ScheduleDay.NodaDate.GetWeekStart();
            var weekEnd = weekStart.GetWeekEnd();

            var schedule = await GetScheduleAsync(userId, weekStart, weekEnd);
            var targetDay = schedule.FirstOrDefault(d => dayId != currentPrep.ScheduleDayId && d.Id == dayId);
            //Prevent collapsing days
            //TODO: this prevents replacing a meal, need to support swapping or force-replacing
            if (targetDay == null)
            {
                throw ScheduleErrors.MissingScheduleDay;
            }

            if (targetDay.DietTypeId != 0)
            {
                throw ScheduleErrors.CantReplaceExistingPreparation;
            }

            //Find the earliest meal for this preparation, that will be the new preparation, if one(s) being moved are to after it
            _scheduleRepo.MovePrep(currentPrep.Id, targetDay.Id);

            //TODO: If we allow moving across weeks, we'll need to clear cache for both weeks
            ClearScheduleCache(userId, weekStart);
                        
            return true;
        }

        public async Task<bool> UpdateServings(int userId, int slotId, int numServings)
        {
            var currentMeal = _scheduleRepo.GetMeal(slotId);

            if (currentMeal == null)
            {
                throw ScheduleErrors.MissingMeal;
            }

            if (currentMeal.ScheduleDay.UserId != userId)
            {
                throw ScheduleErrors.InvalidTargetByUser;
            }

            //Can't move a confirmed meal
            if (currentMeal.ConfirmStatus == ConfirmStatus.CONFIRMED_YES)
            {
                throw ScheduleErrors.CantMoveConfirmedMeal;
            }

            var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
            await shoppingListService.GetShoppingListAsync(userId, currentMeal.ScheduleDay.NodaDate.GetWeekStart());

            shoppingListService.HandlePreparationsRemoved(userId, new List<PreparationDto>{ ToPreparationDto(currentMeal.Preparation)});

            _scheduleRepo.SetMealServings(slotId, numServings);

            var prep = _scheduleRepo.GetPreparation(currentMeal.PreparationId);
            shoppingListService.HandlePreparationsAdded(userId, new List<PreparationDto> {ToPreparationDto(prep)}, currentMeal.ScheduleDay.NodaDate.GetWeekStart());

            ClearScheduleCache(userId, currentMeal.ScheduleDay.NodaDate.GetWeekStart());

            return true;
        }

        public async Task<ScheduleDayDto> AddChallengeDayAsync(int userId, LocalDate date)
        {
            await _subscriptionsService.VerifyDateInSubscriptionAsync(userId, date);

            var scheduleDay = (await GetScheduleInternalAsync(userId, date, null))
                .FirstOrDefault(d => d.NodaDate == date);

            //If date is in the past, we don't generate a schedule
            if (scheduleDay == null)
            {
                throw ScheduleErrors.MissingScheduleDay;
            }

            //If there are any meals, you can't add a challenge to it
            if (!(scheduleDay.Meals == null || !scheduleDay.Meals.Any()))
            {
                throw ScheduleErrors.CantAddChallengeToDayWithMeals;
            }

            var goal = _dietService.GetDietGoalsByUserId(userId, date)?.FirstOrDefault();
            //Pull the preferences to know which meals to filter to (Quick&Dirty, Healthy, etc)
            var preference = _dietService.GetPreferences(userId);

            scheduleDay.DietTypeId = goal.TargetDietId;
            var preparations = new List<Preparation>();

            foreach (var mealType in preference.MealTypes)
            {
                var randomRecipeRequest = new RandomRecipeRequest
                {
                    DietTypeId = scheduleDay.DietTypeId,
                    MealType = mealType,
                };

                var recipe = await _userRecipesService.GetRandomRecipeAsync(randomRecipeRequest, userId);
                
                var prep = new Preparation
                {
                    UserId = userId,
                    ScheduleDayId = scheduleDay.Id,
                    MealType = mealType,
                    RecipeId = recipe.Id,
                    Meals = new List<Meal>
                    {
                        new Meal
                        {
                            ScheduleDayId = scheduleDay.Id,
                            IsChallenge = true,
                            ConfirmStatus = ConfirmStatus.UNSET,
                            Type = mealType,
                            RecipeId = recipe.Id,
                            IsLeftovers = false,
                            Servings = 1
                        }
                    }
                };
                preparations.Add(prep);
            }

            if (scheduleDay.Preparations == null)
            {
                scheduleDay.Preparations = new List<Preparation>();
            }
            scheduleDay.Preparations.AddRange(preparations);
            _scheduleRepo.SaveScheduleDays(new List<ScheduleDay> {scheduleDay});

            var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
            await shoppingListService.GetShoppingListAsync(userId, date.GetWeekStart());

            shoppingListService.HandlePreparationsAdded(userId, preparations.Select(ToPreparationDto).ToList(), date.GetWeekStart());

            ClearScheduleCache(userId, date.GetWeekStart());

            return ToScheduleDayDto(scheduleDay);
        }

        public async Task<ScheduleDayDto> RemoveChallengeDayAsync(int userId, LocalDate date)
        {
            await _subscriptionsService.VerifyDateInSubscriptionAsync(userId, date);
            var dateTime = date.ToDateTimeUnspecified();

            var scheduleDay = (await GetScheduleAsync(userId, date, null))
                .FirstOrDefault(d => d.Date == dateTime);

            if (scheduleDay?.Meals == null || !scheduleDay.Meals.Any() ||
                scheduleDay.DietTypeId == 0 || scheduleDay.Meals.Any(s => !s.IsChallenge))
            {
                return null;
            }

            var shoppingListService = (ShoppingListService) _serviceProvider.GetService(typeof(ShoppingListService));
            var preparations = scheduleDay.Meals.Select(m => m.Preparation).ToList();
            shoppingListService.HandlePreparationsRemoved(userId, preparations);

            _scheduleRepo.RemovePreparations(preparations.Select(p => p.Id).ToList());
            ClearScheduleCache(userId, date.GetWeekStart());

            scheduleDay = (await GetScheduleAsync(userId, date, null))
                .FirstOrDefault(d => d.Date == dateTime);


            return scheduleDay;
        }

        public async Task<bool> RegeneratePreparationAsync(int userId, int preparationId, bool updateShoppingList = true)
        {
            var preparation = _scheduleRepo.GetPreparation(preparationId);

            if (preparation?.ScheduleDay.UserId != userId)
            {
                throw StandardErrors.UnauthorizedRequest;
            }

            if (preparation.Meals.Any(m => m.ConfirmStatus == ConfirmStatus.CONFIRMED_YES))
            {
                throw ScheduleErrors.CantReplaceConfirmedMeal;
            }

            var weekBeginning = preparation.ScheduleDay.NodaDate.GetWeekStart();
            if (updateShoppingList && preparation.RecipeId > 0)
            {
                var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                await shoppingListService.GetShoppingListAsync(userId, weekBeginning);

                shoppingListService.HandlePreparationsRemoved(userId, new List<PreparationDto>{ ToPreparationDto(preparation)});
            }

            //TODO: Add tracking for individual slot regeneration

            var randomRecipeRequest = new RandomRecipeRequest
            {
                DietTypeId = preparation.ScheduleDay.DietTypeId,
                MealType = preparation.MealType,
            };

            var myVotes = await _userRecipesService.ListRecipeVotesAsync(userId);

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

            var recipe = await _userRecipesService.GetRandomRecipeAsync(randomRecipeRequest, userId);

            if (recipe != null && recipe.Id != preparation.RecipeId && _scheduleRepo.SetPreparationRecipeId(preparationId, recipe.Id))
            {
                if (updateShoppingList)
                {
                    var shoppingListService = (ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService));
                    shoppingListService.HandlePreparationsAdded(userId, new List<PreparationDto> { ToPreparationDto(preparation) }, weekBeginning);
                }

                ClearScheduleCache(userId, weekBeginning);

                return true;
            }

            return false;
        }

        public async Task GenerateScheduleAsync(int userId, LocalDate start, LocalDate end, GenerateScheduleRequest request)
        {
            await _subscriptionsService.VerifyDateInSubscriptionAsync(userId, start);

            //Pull the preferences to know which recipes to filter to (Quick&Dirty, Healthy, etc)
            //var preference = _dietService.GetPreferences(userId);
            
            var dietGoal = _dietService.GetDietGoalsByUserId(userId).FirstOrDefault();
            //TODO: This should be based on user's TZ
            var currentDay = new LocalDate(start.Year, start.Month, start.Day);
            
            ClearSchedule(userId, start, end);

            //TODO: Flag regenerations
            _scheduleRepo.TrackScheduleGeneration(userId, start.ToDateTimeUnspecified(), end.ToDateTimeUnspecified());

            //TODO: Take into account schedule for days/weeks before and after this timeframe
            var myVotes = await _userRecipesService.ListRecipeVotesAsync(userId);

            var usedRecipeCounts = new Dictionary<int, int>();
            foreach (var vote in myVotes)
            {
                var val = 100;
                if (vote.Vote == RecipeVote.VoteType.HATE)
                {
                    val *= -1;
                }
                else if (vote.Vote == RecipeVote.VoteType.UNKNOWN)
                {
                    continue;
                }

                usedRecipeCounts.Add(vote.RecipeId, val);
            }

            var randomRecipeRequest = new RandomRecipeRequest
            {
                ExcludeTags = request.ExcludeTags,
                ConsumeIngredients = request.RecipeIngredients
            };
            
            var plan = _dietService.GetPrepPlan(userId, start);
            var generatorDays = plan.Generators.Select(g => g.DayOfWeek).ToList();
            var consumerDays = plan.Consumers.Select(c => c.DayOfWeek).ToList();

            //TODO: GetEmptyWeek(int userId, DateTime start, DateTime end)
            var scheduleDays = new List<ScheduleDay>();

            while (currentDay <= end)
            {
                //Weeks start on Monday
                var hasMeal = consumerDays.Contains((int)currentDay.DayOfWeek - 1) || generatorDays.Contains((int)currentDay.DayOfWeek - 1);

                var scheduleDay = new ScheduleDay
                {
                    NodaDate = currentDay,
                    DietTypeId = hasMeal ? dietGoal.TargetDietId : 0,
                    UserId = userId,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    Preparations = new List<Preparation>(),
                    Meals = new List<Meal>()
                };

                scheduleDays.Add(scheduleDay);
                currentDay = currentDay.PlusDays(1);
            }

            //TODO: FillWeekFromPrepPlan(List<ScheduleDay> scheduleDays, PrepPlan plan)
            foreach (var generator in plan.Generators)
            {
                var genDay = scheduleDays[generator.DayOfWeek];
                randomRecipeRequest.DietTypeId = genDay.DietTypeId;
                randomRecipeRequest.MealType = generator.MealType;

                var recipe = await _userRecipesService.GetRandomRecipeAsync(randomRecipeRequest, userId);

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
                            IsLeftovers = consumer.DayOfWeek != generator.DayOfWeek || consumer.MealType != generator.MealType
                        };

                        consumeDay.Meals.Add(meal);
                        prep.Meals.Add(meal);
                    }
                }
            }

            _scheduleRepo.SaveScheduleDays(scheduleDays);
            
            ClearScheduleCache(userId, start);
        }

        public async Task<bool> ConfirmMealAsync(int userId, int mealId, ConfirmStatus confirm)
        {
            var slot = _scheduleRepo.GetMeal(mealId);

            if (slot == null)
            {
                throw ScheduleErrors.MissingMeal;
            }

            if (slot.ScheduleDay.UserId != userId)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            await _subscriptionsService.VerifyDateInSubscriptionAsync(userId, slot.ScheduleDay.NodaDate);

            if (_scheduleRepo.SetConfirmState(mealId, confirm))
            {
                var statService = _serviceProvider.GetService<StatsService>();

                await statService.TrackCompletionAsync(userId, confirm == ConfirmStatus.CONFIRMED_YES ? 1 : -1, slot.IsChallenge);
                
                ClearScheduleCache(userId, slot.ScheduleDay.NodaDate.GetWeekStart());

                return true;
            }

            return false;
        }

        public async Task<bool> SendNextWeekScheduleNotifications()
        {
            var requestContextFactory = _serviceProvider.GetService<RequestContextFactory>();
            var userService = _serviceProvider.GetService<UsersService>();

            var offset = 0;
            var count = 200;
            List<UserDto> users;
            do
            {
                users = await userService.GetActiveUsers(count, offset);
                
                //TODO: Batch-get for Profile + Preferences, or filter as part of Profile GET request
                for (var i = 0; i < users.Count; i++)
                {
                    var user = await requestContextFactory.StartRequestContext(users[i].UserId);

                    var context = _serviceProvider.GetService<RequestContext>();
                    if (context.UserId != 0 && user != null)
                    {
                        var prefs = await userService.GetUserPreferences(context.UserId);

                        if (prefs != null && 
                            (!prefs.Preferences.ContainsKey("mealPlanReminder") || prefs.Preferences["mealPlanReminder"] != "false"))
                        {
                            await SendNextWeekScheduleAsync(user);
                        }
                    }
                }
            } while (users.Any() && users.Count == count);

            _serviceProvider.GetService<RequestContextFactory>().ClearContext();

            return true;
        }

        public async Task<bool> SendNextWeekScheduleAsync(UserDto user)
        {
            try
            {
                var nowDate = SystemClock.Instance.GetCurrentInstant()
                    .InZone(DateTimeZoneProviders.Tzdb.GetZoneOrNull(RequestContext.DEFAULT_TIMEZONE))
                    .Date.PlusDays(7);

                var schedule =
                    (await GetScheduleAsync(user.UserId, nowDate.GetWeekStart(), nowDate.GetWeekEnd()))
                    .ToList();
                var recipeIds = schedule.SelectMany(d => d.Meals?.Select(m => m.RecipeId).ToList() ?? new List<int>())
                    .ToList();

                var stat = _statsService.GetDidYouKnowStat();

                var model = new ScheduleRecipeContainer
                {
                    Schedule = schedule,
                    Recipes = _serviceProvider.GetService<RecipesService>().GetRecipes(recipeIds)
                        .ToDictionary(r => r.Id, r => r),
                    DidYouKnowStat = stat
                };

                return await _serviceProvider.GetService<EmailService>()
                    .SendEmail("MealPlanReady", user.Email, "Your meals for next week are ready", user.FullName, model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send next week schedule for user {0}", user.UserId);
                return false;
            }
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
                IsChallenge = day.Meals?.Any(s => s.IsChallenge) ?? false,
                Meals = day.Meals?.Select(ToMealDto).ToList()
            };
        }

        public MealDto ToMealDto(Meal meal)
        {
            return new MealDto
            {
                Id = meal.Id,
                MealType = meal.Type.ToString(),
                RecipeId = meal.RecipeId,
                Preparation = meal.Preparation != null ? ToPreparationDto(meal.Preparation) : null,
                Confirmed = meal.ConfirmStatus,
                ScheduleDayId = meal.ScheduleDayId,
                Date = meal.ScheduleDay.Date,
                IsChallenge = meal.IsChallenge,
                IsLeftovers = meal.IsLeftovers,
                NumServings = meal.Servings
            };
        }

        public PreparationDto ToPreparationDto(Preparation prep)
        {
            return new PreparationDto
            {
                Id = prep.Id,
                MealType = prep.MealType,
                RecipeId = prep.RecipeId,
                Date = prep.ScheduleDay.Date,
                NumServings = prep.Meals?.Sum(m => m.Servings) ?? 0
            };
        }

        private async Task<List<ScheduleDay>> GetScheduleInternalAsync(int userId, LocalDate start, LocalDate? end, bool regenIfEmpty = true)
        {
            var startDateTime = start.GetWeekStart().ToDateTimeUnspecified();
            if (!end.HasValue)
            {
                end = start.GetWeekEnd();
            }
            var endDateTime = end.Value.ToDateTimeUnspecified();

            var schedule = _scheduleRepo.GetSchedule(userId, startDateTime, endDateTime);

            //TODO: Does this need to be relative to TimeZone???
            var reqContext = _serviceProvider.GetService<RequestContext>();
            var curInstant = SystemClock.Instance.GetCurrentInstant();
            var endInstant = end.Value.AtStartOfDayInZone(reqContext.Dtz).ToInstant();
            //If no schedule currently and not a past week, generate and recall method
            if (schedule.Count == 0 && endInstant >= curInstant && regenIfEmpty)
            {
                await GenerateScheduleAsync(userId, start, end.Value, new GenerateScheduleRequest());
                return await GetScheduleInternalAsync(userId, start, end, false);
            }

            return schedule;
        }

        private void ClearSchedule(int userId, LocalDate start, LocalDate? end)
        {
            ((ShoppingListService)_serviceProvider.GetService(typeof(ShoppingListService))).ClearShoppingList(userId, start);

            _scheduleRepo.ClearSchedule(userId, start.ToDateTimeUnspecified(), end?.ToDateTimeUnspecified());
            ClearScheduleCache(userId, start);
        }

        private void ClearScheduleCache(int userId, LocalDate start)
        {
            _memcached.Remove(CacheKeys.Schedule.UserSchedule(userId, start.ToDateTimeUnspecified()));
        }
    }
}
