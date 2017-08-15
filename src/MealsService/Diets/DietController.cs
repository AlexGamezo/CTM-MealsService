using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MealsService.Diets.Dtos;
using MealsService.Responses;

namespace MealsService.Diets
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
            var claims = HttpContext.User.Claims;
            int id;
            bool isAdmin;

            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);
            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out id);


            return id != 0 && (id == userId || isAdmin);
        }
    }
}
