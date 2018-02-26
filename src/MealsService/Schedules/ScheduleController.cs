using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;

using MealsService.Common;
using MealsService.Common.Extensions;

using MealsService.Responses;
using MealsService.Services;
using MealsService.Recipes;
using MealsService.Requests;
using MealsService.Schedules.Dtos;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class ScheduleController : AuthorizedController
    {
        private ScheduleService _scheduleService { get; }
        private RecipesService _recipesService { get; }

        public ScheduleController(ScheduleService scheduleService, RecipesService recipesService)
        {
            _scheduleService = scheduleService;
            _recipesService = recipesService;
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

            DateTime date = dateString == "" ? DateTime.UtcNow : DateTime.Parse(dateString);

            var weekBeginning = date.GetWeekStart();

            var scheduleDays = _scheduleService.GetSchedule(userId, weekBeginning)
                .Select(_scheduleService.ToScheduleDayDto)
                .ToList();

            var recipeIds = scheduleDays.SelectMany(d => d.Meals.Select(s => s.RecipeId));
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
        public IActionResult UpdateMeal(int userId, int mealId, [FromBody]MealPatchRequest request)
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
                success = _scheduleService.ConfirmMeal(userId, mealId, request.Confirm);
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

            if (request.Op == ScheduleDayPatchRequest.Operation.AcceptChallenge)
            {
                var day = _scheduleService.AddChallengeDay(userId, DateTime.Parse(dateString));

                return Json(new SuccessResponse<object>(
                    new
                    {
                        scheduledDay = day
                    }));
            }
            else if (request.Op == ScheduleDayPatchRequest.Operation.DeclineChallenge)
            {
                var day = _scheduleService.RemoveChallengeDay(userId, DateTime.Parse(dateString));

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

            DateTime date = dateString == "" ? DateTime.UtcNow : DateTime.Parse(dateString);

            date = date.GetWeekStart();
            var end = date.AddDays(6);

            if (end > DateTime.UtcNow)
            {
                _scheduleService.GenerateSchedule(userId, date, end, request);
            }

            return Get(userId, dateString);
        }
    }
}