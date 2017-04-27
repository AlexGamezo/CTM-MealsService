using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using MealsService.Responses;
using MealsService.Services;
using Microsoft.AspNetCore.Authorization;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class ScheduleController : Controller
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
        [Route("me/{dateString:datetime}"), HttpGet]
        public IActionResult Get(string dateString = "")
        {
            var claims = HttpContext.User.Claims;
            int id = 0;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out id);

            return Get(id, dateString);
        }

        [Route("{userId:int}"), HttpGet]
        [Route("{userId:int}/{dateString:datetime}"), HttpGet]
        public IActionResult Get(int userId, string dateString = "")
        {
            DateTime date;
            if (dateString == "")
            {
                date = DateTime.Now.Date;
            }
            else
            {
                var dateParts = dateString.Split('-');
                if (dateParts.Length != 3)
                {
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-m-d)", 400));
                }

                int year, month, day;
                if (!int.TryParse(dateParts[0], out year)
                    || !int.TryParse(dateParts[1], out month)
                    || !int.TryParse(dateParts[2], out day))
                {
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-m-d)", 400));
                }
                date = new DateTime(year, month, day);
            }
            
            var days = (int) date.DayOfWeek - 1;
            if (days < 0) days += 7;
            var weekBeginning = date.Subtract(new TimeSpan(days, 0, 0, 0));

            var scheduleDays = _scheduleService.GetSchedule(userId, weekBeginning)
                .Select(_scheduleService.ToScheduleDayDto)
                .ToList();

            var recipeIds = scheduleDays.SelectMany(d => d.ScheduleSlots.Select(s => s.RecipeId));
            var recipes = _recipesService.GetRecipes(recipeIds);

            return Json(new SuccessResponse<object>( new
            {
                scheduleDays,
                recipes
            }));
        }

        [Route("{userId:int}"), HttpPost]
        [Route("{userId:int}/{dateString:datetime}"), HttpPost]
        public IActionResult GenerateSchedule(int userId, string dateString = "")
        {
            DateTime date;
            if (dateString == "")
            {
                date = DateTime.UtcNow.Date;
            }
            else
            {
                var dateParts = dateString.Split('-');
                if (dateParts.Length != 3)
                {
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-m-d)", 400));
                }

                int year, month, day;
                if (!int.TryParse(dateParts[0], out year)
                    || !int.TryParse(dateParts[1], out month)
                    || !int.TryParse(dateParts[2], out day))
                {
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-m-d)", 400));
                }
                date = new DateTime(year, month, day);
            }

            _scheduleService.GenerateSchedule(userId, date, date.AddDays(7));

            return Json(new SuccessResponse(true));
        }
    }
}