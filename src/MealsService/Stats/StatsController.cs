using System.Linq;
using System.Threading.Tasks;
using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Responses;
using MealsService.Stats.Data;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.Stats
{
    [Route("[controller]")]
    public class StatsController : AuthorizedController
    {
        private StatsService _statsService;

        public StatsController(StatsService statsService)
        {
            _statsService = statsService;
        }

        [HttpGet("me/progress")]
        [HttpGet("{userId:int}/progress")]
        public IActionResult GetProgress(int userId)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            var stats = _statsService.GetStats(userId);
            stats.Add(_statsService.GetProgress(userId));

            return Json(new
            {
                Data = stats,
                CurrentStreak = stats.Last().Streak
            });
        }

        [HttpGet("me/impact")]
        [HttpGet("{userId:int}/impact")]
        public IActionResult GetImpactStats(int userId)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }

            return Json(new
            {
                Impacts = _statsService.GetImpactStatements(userId)
                
                /*new List<object>
                {
                    new
                    {
                        Type = ImpactType.WATER,
                        Text = "Each plant-based meal saves an average 3 gallons of water compared to a meat-heavy meal",
                        RefUrl = "http://greenerplate.com/",
                        Meta = "An average pound of meat requires 10 gallons of water, compared to 1 gallon for an equivalent amount of "
                    },
                    new
                    {
                        Type = ImpactType.AIR,
                        Text = "Each meat-based meal not eaten reduces Global Warming emissions equivalent to an average car driven for a week",
                        RefUrl = "http://greenerplate.com/",
                        Meta = "An average pound of meat requires 10 gallons of water, compared to 1 gallon for an equivalent amount of "
                    }
                }*/
            });
        }

        [HttpPost("processWeek")]
        public async Task<IActionResult> ProcessWeek()
        {
            await _statsService.ProcessWeekStatsAsync();

            return Json(new SuccessResponse());
        }

        [HttpGet("didyouknow/list")]
        public IActionResult ListDidYouKnowStats()
        {
            if(!IsAdmin)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            return Json(new
            {
                Stats = _statsService.ListDidYouKnowStats()
            });
        }

        [HttpPost("didyouknow")]
        public IActionResult UpdateDidYouKnowStats([FromBody]DidYouKnowStat stat)
        {
            if (!IsAdmin)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            var updatedStat = _statsService.AddDidYouKnowStat(stat);

            return Json(new
            {
                Stat = updatedStat
            });
        }

        [HttpPut("didyouknow/{id:int}")]
        public IActionResult UpdateDidYouKnowStats(int id, [FromBody]DidYouKnowStat stat)
        {
            if (!IsAdmin)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            var success = _statsService.UpdateDidYouKnowStat(stat);
            
            return Json(new SuccessResponse(success));
        }
    }
}
