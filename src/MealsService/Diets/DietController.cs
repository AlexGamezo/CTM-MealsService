using System;
using MealsService.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MealsService.Diets.Dtos;
using MealsService.Responses;

namespace MealsService.Diets
{
    [Route("[controller]")]
    public class DietController : AuthorizedController
    {
        private DietService DietService { get; }

        public DietController(DietService dietService)
        {
            DietService = dietService;
        }

        [Authorize]
        [Route("me"), HttpGet]
        public IActionResult MyDiet()
        {
            return Get(AuthorizedUser);
        }

        [Authorize]
        [Route("{userId}"), HttpGet]
        public IActionResult Get(int userId)
        {
            if (VerifyPermission(userId))
            {
                var dietGoals = DietService.GetDietGoalsByUserId(userId, DateTime.UtcNow);

                var menuPreference = DietService.GetPreferences(userId);
                return Json(new DietDto
                { 
                    Preferences = menuPreference,
                    Goals = dietGoals
                });
            }
            else
            {
                this.Response.StatusCode = 403;
                return Json(new ErrorResponse("You are not logged in as the requested user.", 403));
            }
        }

        [Authorize]
        [Route("{userId}"), HttpPut]
        public IActionResult Set(int userId, [FromBody] DietDto diet)
        {
            if (VerifyPermission(userId))
            {
                DietService.UpdatePreferences(userId, diet.Preferences);
                if (diet.Goals != null)
                {
                    DietService.UpdateDietGoals(userId, diet.Goals);
                }

                return Json(new SuccessResponse(true));
            }
            else
            {
                this.Response.StatusCode = 403;
                return Json(new ErrorResponse("You are not logged in as the requested user.", 403));
            }
        }

        protected bool VerifyPermission(int userId)
        {
            return AuthorizedUser == userId || IsAdmin;
        }
    }
}
