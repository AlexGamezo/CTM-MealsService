using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using NodaTime;

using MealsService.Common.Extensions;
using MealsService.Email;
using MealsService.Infrastructure;
using MealsService.Recipes;
using MealsService.Schedules;
using MealsService.Schedules.Dtos;
using MealsService.Stats;
using MealsService.Users;
using MealsService.Users.Data;
using MealsService.Ingredients;

namespace MealsService.Notifications
{
    public class NotificationsService : INotificationsService
    {
        private RequestContextFactory _requestContextFactory;
        private UsersService _usersService;
        private IScheduleService _scheduleService;
        private IIngredientsService _ingredientsService;
        private EmailService _emailService;
        private StatsService _statsService;
        private IRecipesService _recipesService;

        private ILogger<INotificationsService> _logger;

        public NotificationsService(RequestContextFactory requestContextFactory,
            UsersService usersService,
            StatsService statsService,
            EmailService emailService,
            IRecipesService recipesService,
            IIngredientsService ingredientsService,
            IScheduleService scheduleService,
            ILoggerFactory loggerFactory
        )
        {
            _requestContextFactory = requestContextFactory;
            _usersService = usersService;
            _statsService = statsService;
            _emailService = emailService;

            _recipesService = recipesService;
            _scheduleService = scheduleService;
            _ingredientsService = ingredientsService;

            _logger = loggerFactory.CreateLogger<NotificationsService>();
        }

        public async Task<bool> SendNextWeekScheduleNotificationsAsync()
        {
            var offset = 0;
            var count = 200;
            List<UserDto> users;

            do
            {
                users = await _usersService.GetActiveUsers(count, offset);
                
                //TODO: Batch-get for Profile + Preferences, or filter as part of Profile GET request
                for (var i = 0; i < users.Count; i++)
                {
                    var user = await _requestContextFactory.StartRequestContext(users[i].UserId);

                    if (user != null && user.UserId != 0)
                    {
                        var prefs = await _usersService.GetUserPreferences(user.UserId);

                        if (prefs != null &&
                            (!prefs.Preferences.ContainsKey("mealPlanReminder") || prefs.Preferences["mealPlanReminder"] != "false"))
                        {
                            await SendNextWeekScheduleAsync(user);
                        }
                    }
                }

                offset += count;
            } while (users.Any() && users.Count == count);

            _requestContextFactory.ClearContext();

            return true;
        }

        public async Task<bool> SendNextWeekScheduleAsync(UserDto user)
        {
            try
            {
                var nowDate = SystemClock.Instance.GetCurrentInstant()
                    .InZone(DateTimeZoneProviders.Tzdb.GetZoneOrNull(RequestContext.DEFAULT_TIMEZONE) ?? DateTimeZoneProviders.Tzdb.GetSystemDefault())
                    .Date.PlusDays(7);

                var schedule =
                    (await _scheduleService.GetScheduleAsync(user.UserId, nowDate.GetWeekStart()))
                    .ToList();
                var recipeIds = schedule.SelectMany(d => d.Meals?.Select(m => m.RecipeId).ToList() ?? new List<int>())
                    .ToList();

                var stat = _statsService.GetDidYouKnowStat();

                var model = new ScheduleRecipeContainer
                {
                    Schedule = schedule,
                    Recipes = _recipesService.ListRecipes().Where(r => recipeIds.Contains(r.Id))
                        .ToDictionary(r => r.Id, r => r),
                    Ingredients = _ingredientsService.ListIngredients().ToDictionary(i => i.Id, i => i),
                    DidYouKnowStat = stat
                };

                return await _emailService
                    .SendEmail("MealPlanReady", user.Email, "Your meals for next week are ready", user.FullName, model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send next week schedule for user {0}", user.UserId);
                return false;
            }
        }
    }
}