using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using MealsService.Common;
using Microsoft.AspNetCore.Authorization;

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

            DateTime date = dateString == "" ? _scheduleService.GetWeekStart(DateTime.UtcNow) : DateTime.Parse(dateString);

            var weekBeginning = _scheduleService.GetWeekStart(date);

            var scheduleDays = _scheduleService.GetSchedule(userId, weekBeginning)
                .Select(_scheduleService.ToScheduleDayDto)
                .ToList();

            var recipeIds = scheduleDays.SelectMany(d => d.ScheduleSlots.Select(s => s.RecipeId));
            var recipes = _recipesService.GetRecipes(recipeIds, userId);

            return Json(new SuccessResponse<object>( new
            {
                scheduleDays,
                recipes
            }));
        }

        [Authorize]
        [Route("me/slots/{slotId:int}/recycles"), HttpPost]
        public IActionResult RegenerateSlot(int slotId) 
        {
            var newSlot = _scheduleService.RegenerateSlot(AuthorizedUser, slotId);

            if (newSlot != null)
            {
                return Json(new SuccessResponse<object>(
                new {
                    slot = newSlot
                }));
            }

            return Json(new ErrorResponse("Could not regenerate slot", 500));
        }
        
        [Authorize]
        [Route("me/slots/{slotId:int}"), HttpPatch]
        [Route("{userId:int}/slots/{slotId:int}"), HttpPatch]
        public IActionResult UpdateSlot(int userId, int slotId, [FromBody]ScheduleSlotPatchRequest request)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var success = false;

            if (request.Op == ScheduleSlotPatchRequest.Operation.MoveSlot)
            {
                success = _scheduleService.MoveSlot(userId, slotId, request.ScheduleDayId);
            }
            else if (request.Op == ScheduleSlotPatchRequest.Operation.UpdateConfirmState)
            {
                success = _scheduleService.ConfirmDay(userId, slotId, request.Confirm);
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

            DateTime date = dateString == "" ? _scheduleService.GetWeekStart(DateTime.UtcNow) : DateTime.Parse(dateString);

            var end = date.AddDays(6);

            if (end > DateTime.UtcNow)
            {
                _scheduleService.GenerateSchedule(userId, date, end, request);
            }

            return Get(userId, dateString);
        }
    }
}