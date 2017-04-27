using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using MealsService.Services;
using Microsoft.AspNetCore.Authorization;
using MealsService.Responses;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class DietController : Controller
    {
        private DietService DietService { get; set; }

        public DietController(DietService dietService)
        {
            DietService = dietService;
        }

        [Authorize]
        [Route("me"), HttpGet]
        public IActionResult MyDiet()
        {
            var claims = HttpContext.User.Claims;
            var id = Int32.Parse(claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);

            return Get(id);
        }

        [Authorize]
        [Route("{userId}"), HttpGet]
        public IActionResult Get(int userId)
        {
            if (VerifyPermission(userId))
            {
                var dietGoals = DietService.GetDietGoalsByUserId(userId);
                var menuPreference = DietService.GetPreferences(userId);
                return Json(new MenuPreferencesDto
                {
                    DietGoals = dietGoals,
                    ShoppingFrequency = menuPreference.ShoppingFreq,
                    MealStyle = menuPreference.MealStyle,
                    MealTypes = menuPreference.MealTypes
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
        public IActionResult Set(int userId, [FromBody] MenuPreferencesDto mealPreferences)
        {
            if (VerifyPermission(userId))
            {
                DietService.UpdatePreferences(userId, mealPreferences);
                if (mealPreferences.DietGoals != null)
                {
                    DietService.UpdateDietGoals(userId, mealPreferences);
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
            var claims = HttpContext.User.Claims;
            int id = 0;
            bool isAdmin = false;

            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);
            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out id);


            return id != 0 && (id == userId || isAdmin);
        }
    }
}
