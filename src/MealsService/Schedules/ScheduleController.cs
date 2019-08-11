using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
using MealsService.Email;
using MealsService.Infrastructure;
using MealsService.Responses;
using MealsService.Services;
using MealsService.Recipes;
using MealsService.Requests;
using MealsService.Schedules.Data;
using MealsService.Schedules.Dtos;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class ScheduleController : AuthorizedController
    {
        private ScheduleService _scheduleService { get; }
        private RecipesService _recipesService { get; }
        private IServiceProvider _serviceProvider { get; }

        public ScheduleController(ScheduleService scheduleService, RecipesService recipesService, IServiceProvider serviceProvider)
        {
            _scheduleService = scheduleService;
            _recipesService = recipesService;
            _serviceProvider = serviceProvider;
        }

        [Route("mealplanready"), HttpPost]
        public async Task<IActionResult> MealPlanReady()
        {
            var success = await _scheduleService.SendNextWeekScheduleNotifications();

            return Json(success ? (object)new SuccessResponse() : new ErrorResponse("Could not send message", 500));
        }

        [Route("email/test"), HttpPost]
        public async Task<IActionResult> TestEmail()
        {
            var success = await _serviceProvider.GetService<EmailService>()
                .SendEmail("Test", "alex@agamezo.com", "Test Email - 3/31/2019", "Alex Gamezo");

            return Json(success ? (object) new SuccessResponse() : new ErrorResponse("Could not send message", 500));
        }

        [Authorize]
        [Route("me"), HttpGet]
        [Route("{userId:int}"), HttpGet]
        [Route("me/{dateString:datetime}"), HttpGet]
        [Route("{userId:int}/{dateString:datetime}"), HttpGet]
        public async Task<IActionResult> Get(int userId, string dateString = "")
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var result = LocalDatePattern.Iso.Parse(dateString);
            LocalDate localDate;

            if (!string.IsNullOrEmpty(dateString))
            {
                if (result.Success)
                {
                    localDate = result.Value;
                }
                else
                {
                    throw StandardErrors.InvalidDateSpecified;
                }
            }
            else
            {
                var zone = _serviceProvider.GetService<RequestContext>().Dtz;
                localDate = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;
            }

            var scheduleDays = (await _scheduleService.GetScheduleAsync(userId, localDate.GetWeekStart(), localDate.GetWeekEnd()))
                .ToList();

            var recipeIds = scheduleDays.Where(d => d.Meals != null).SelectMany(d => d.Meals.Select(s => s.RecipeId));

            return Json(new SuccessResponse<object>( new
            {
                scheduleDays
            }));
        }
        
        [Authorize]
        [Route("me/slots/{preparationId:int}/recycles"), HttpPost]
        public async Task<IActionResult> RegenerateSlot(int preparationId) 
        {
            var success = await _scheduleService.RegeneratePreparationAsync(AuthorizedUser, preparationId);

            if (success)
            {
                return Json(new SuccessResponse());
            }

            return Json(new ErrorResponse("Could not regenerate slot", 500));
        }
        
        [Authorize]
        [Route("me/meals/{mealId:int}"), HttpPatch]
        [Route("{userId:int}/meals/{mealId:int}"), HttpPatch]
        public async Task<IActionResult> UpdateMeal(int userId, int mealId, [FromBody]MealPatchRequest request)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var success = false;

            if (request.Op == MealPatchRequest.Operation.MoveMeal)
            {
                //success = await _scheduleService.MoveMealAsync(userId, mealId, request.ScheduleDayId);
            }
            else if (request.Op == MealPatchRequest.Operation.UpdateConfirmState)
            {
                success = await _scheduleService.ConfirmMealAsync(userId, mealId, request.Confirm);
            }
            else if (request.Op == MealPatchRequest.Operation.ChangeServings)
            {
                success = await _scheduleService.UpdateServings(userId, mealId, request.NewServings);
            }

            if (!success)
            {
                Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                return Json(new ErrorResponse("Failed to update slot", 500));
            }

            return Json(new SuccessResponse());
        }
        
        [Authorize]
        [Route("me/preparations/{prepId:int}"), HttpPatch]
        [Route("{userId:int}/preparations/{prepId:int}"), HttpPatch]
        public async Task<IActionResult> UpdatePrep(int userId, int prepId, [FromBody]PreparationPatchRequest request)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            if (request.Op == PreparationPatchRequest.Operation.MovePreparation)
            {
                await _scheduleService.MovePreparationAsync(userId, prepId, request.ScheduleDayId);
            }
            else if (request.Op == PreparationPatchRequest.Operation.SetRecipe)
            {
                await _scheduleService.SetPreparationRecipeAsync(userId, prepId, request.RecipeId);
            }

            return Json(new SuccessResponse());
        }

        [Authorize]
        [Route("me/{dateString:datetime}"), HttpPatch]
        [Route("{userId:int}/{dateString:datetime}"), HttpPatch]
        public async Task<IActionResult> UpdateDay(int userId, string dateString, [FromBody] ScheduleDayPatchRequest request)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }
            
            var result = LocalDatePattern.Iso.Parse(dateString);
            LocalDate localDate;
            if (result.Success)
            {
                localDate = result.Value;
            }
            else
            {
                throw StandardErrors.InvalidDateSpecified;
            }

            if (request.Op == ScheduleDayPatchRequest.Operation.AcceptChallenge)
            {
                var day = await _scheduleService.AddChallengeDayAsync(userId, localDate);

                return Json(new SuccessResponse<object>(
                    new
                    {
                        scheduledDay = day
                    }));
            }
            else if (request.Op == ScheduleDayPatchRequest.Operation.DeclineChallenge)
            {
                var day = await _scheduleService.RemoveChallengeDayAsync(userId, localDate);

                return Json(new SuccessResponse<object>(
                    new
                    {
                        scheduledDay = day
                    }));
            }

            Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Json(new ErrorResponse("Bad request", 400));
        }

        [Authorize]
        [Route("me"), HttpPost]
        [Route("{userId:int}"), HttpPost]
        [Route("me/{dateString:datetime}"), HttpPost]
        [Route("{userId:int}/{dateString:datetime}"), HttpPost]
        public async Task<IActionResult> GenerateSchedule([FromBody]GenerateScheduleRequest request, int userId, string dateString = "")
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var result = LocalDatePattern.Iso.Parse(dateString);
            LocalDate localDate;
            if (result.Success)
            {
                localDate = result.Value;
            }
            else
            {
                throw StandardErrors.InvalidDateSpecified;
            }

            var end = localDate.GetWeekEnd();
            var endInstant = end.AtStartOfDayInZone(_serviceProvider.GetService<RequestContext>().Dtz).ToInstant();

            if (endInstant > SystemClock.Instance.GetCurrentInstant())
            {
                await _scheduleService.GenerateScheduleAsync(userId, localDate, end, request);
            }

            return await Get(userId, dateString);
        }

        [Authorize]
        [Route("me/nextmeal"), HttpGet]
        [Route("{userId}/nextmeal"), HttpGet]
        public async Task<IActionResult> NextMeal(int userId)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var zone = _serviceProvider.GetService<RequestContext>().Dtz;
            var startDate = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;

            var schedule = await _scheduleService.GetScheduleAsync(userId, startDate, startDate.GetWeekEnd());

            if (!schedule.Any(d => d.Meals.Any(m => m.Confirmed == ConfirmStatus.UNSET)))
            {
                startDate = startDate.PlusDays(7);
                schedule = await _scheduleService.GetScheduleAsync(userId, startDate, startDate.GetWeekEnd());
            }

            var meal = schedule.SelectMany(d => d.Meals).First(m => m.Confirmed == ConfirmStatus.UNSET);
            
            return Json(new
            {
                meal,
            });
        }
    }
}