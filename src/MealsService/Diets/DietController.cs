using System;
using MealsService.Common;
using MealsService.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MealsService.Diets.Dtos;
using MealsService.Infrastructure;
using MealsService.Responses;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.Diets
{
    [Route("[controller]")]
    public class DietController : AuthorizedController
    {
        private DietService DietService { get; }
        private IServiceProvider _serviceProvider { get; }

        public DietController(DietService dietService, IServiceProvider serviceProvider)
        {
            DietService = dietService;
            _serviceProvider = serviceProvider;
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
            VerifyPermission(userId);

            var zone = _serviceProvider.GetService<RequestContext>().Dtz;
            var now = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;

            var dietGoals = DietService.GetDietGoalsByUserId(userId, now);

            var menuPreference = DietService.GetPreferences(userId);
            return Json(new DietDto
            { 
                Preferences = menuPreference,
                Goals = dietGoals,
                PrepPlanDays = DietService.GetPrepPlanDtos(userId, now)
            });
        }

        [Authorize]
        [Route("{userId}"), HttpPut]
        public IActionResult Set(int userId, [FromBody] DietDto diet)
        {
            VerifyPermission(userId);
            DietService.UpdatePreferences(userId, diet.Preferences);
            if (diet.Goals != null)
            {
                DietService.UpdateDietGoals(userId, diet.Goals);
            }

            if (diet.PrepPlanDays != null)
            {
                DietService.UpdatePrepPlanDays(userId, diet.PrepPlanDays);
            }

            return Json(new SuccessResponse(true));
        }

        protected bool VerifyPermission(int userId)
        {
            if (AuthorizedUser != userId && !IsAdmin)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            return true;
        }
    }
}
