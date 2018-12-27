using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
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

        [Authorize]
        [Route("me"), HttpGet]
        [Route("{userId:int}"), HttpGet]
        [Route("me/{dateString:datetime}"), HttpGet]
        [Route("{userId:int}/{dateString:datetime}"), HttpGet]
        public IActionResult Get(int userId, string dateString = "")
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

            var scheduleDays = _scheduleService.GetSchedule(userId, localDate.GetWeekStart(), localDate.GetWeekStart().PlusDays(6))
                .Select(_scheduleService.ToScheduleDayDto)
                .ToList();

            var recipeIds = scheduleDays.Where(d => d.Meals != null).SelectMany(d => d.Meals.Select(s => s.RecipeId));
            var recipes = _recipesService.GetRecipes(recipeIds, userId);

            return Json(new SuccessResponse<object>( new
            {
                scheduleDays,
                recipes
            }));
        }

        [Authorize]
        [Route("me/slots/{preparationId:int}/recycles"), HttpPost]
        public IActionResult RegenerateSlot(int preparationId) 
        {
            var success = _scheduleService.RegeneratePreparation(AuthorizedUser, preparationId);

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
                success = _scheduleService.MoveMeal(userId, mealId, request.ScheduleDayId);
            }
            else if (request.Op == MealPatchRequest.Operation.UpdateConfirmState)
            {
                success = await _scheduleService.ConfirmMealAsync(userId, mealId, request.Confirm);
            }

            if (!success)
            {
                Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                return Json(new ErrorResponse("Failed to update slot", 500));
            }

            return Json(new SuccessResponse());
        }

        [Authorize]
        [Route("me/{dateString:datetime}"), HttpPatch]
        [Route("{userId:int}/{dateString:datetime}"), HttpPatch]
        public IActionResult UpdateDay(int userId, string dateString, [FromBody] ScheduleDayPatchRequest request)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }
            
            var success = false;
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
                var day = _scheduleService.AddChallengeDay(userId, localDate);

                return Json(new SuccessResponse<object>(
                    new
                    {
                        scheduledDay = day
                    }));
            }
            else if (request.Op == ScheduleDayPatchRequest.Operation.DeclineChallenge)
            {
                var day = _scheduleService.RemoveChallengeDay(userId, localDate);

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
        public IActionResult GenerateSchedule([FromBody]GenerateScheduleRequest request, int userId, string dateString = "")
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

            var end = localDate.PlusDays(6);
            var endInstant = end.AtStartOfDayInZone(_serviceProvider.GetService<RequestContext>().Dtz).ToInstant();

            if (endInstant > SystemClock.Instance.GetCurrentInstant())
            {
                _scheduleService.GenerateSchedule(userId, localDate, end, request);
            }

            return Get(userId, dateString);
        }

        [Authorize]
        [Route("me/nextmeal"), HttpGet]
        [Route("{userId}/nextmeal"), HttpGet]
        public IActionResult NextMeal(int userId)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var zone = _serviceProvider.GetService<RequestContext>().Dtz;
            var startDate = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;

            var schedule = _scheduleService.GetSchedule(userId, startDate, startDate.PlusDays(6));

            if (!schedule.Any(d => d.Meals.Any(m => m.ConfirmStatus == ConfirmStatus.UNSET)))
            {
                startDate = startDate.PlusDays(7);
                schedule = _scheduleService.GetSchedule(userId, startDate, startDate.PlusDays(6));
            }

            var meal = _scheduleService.ToMealDto(schedule.SelectMany(d => d.Meals).First(m => m.ConfirmStatus == ConfirmStatus.UNSET));
            
            return Json(new
            {
                meal,
            });
        }
    }
}