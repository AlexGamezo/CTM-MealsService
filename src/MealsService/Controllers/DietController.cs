using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using MealsService.Responses;
using MealsService.Services;
using Microsoft.AspNetCore.Authorization;

namespace MealsService.Controllers
{
    [Route("/api/[controller]")]
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

        [Route("{userId}"), HttpGet]
        public IActionResult Get(int userId)
        {
            var dietGoals = DietService.GetDietGoalsByUserId(userId);
            var menuPreference = DietService.GetPreferences(userId);
            return new JsonResult(new
            {
                dietGoals,
                shoppingFrequency = menuPreference.ShoppingFreq,
                mealStyle = menuPreference.MealStyle.ToString()
            });
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
