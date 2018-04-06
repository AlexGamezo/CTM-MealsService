
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Common.Extensions;
using MealsService.Diets;
using MealsService.Infrastructure;
using MealsService.Schedules.Data;
using MealsService.Stats.Data;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.Stats
{
    public class StatsService
    {
        private MealsDbContext _dbContext;

        private DietService _dietService;
        private IServiceProvider _serviceProvider;

        public StatsService(MealsDbContext dbContext, DietService dietService, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _dietService = dietService;
            _serviceProvider = serviceProvider;
        }

        public List<ImpactStatement> GetImpactStatements(int userId, List<ImpactStatement.Type> types = null)
        {
            IQueryable<ImpactStatement> statementsQuery = _dbContext.ImpactStatements;

            if (types != null && types.Any())
            {
                statementsQuery = statementsQuery.Where(s => types.Contains(s.ImpactType));
            }

            var statements = statementsQuery.ToList();

            return statements.Skip(new Random().Next() % (statements.Count - 3)).Take(3).ToList();
        }

        public StatSnapshot GetProgress(int userId, LocalDate? start = null)
        {
            var zone = _serviceProvider.GetService<RequestContext>().Dtz;

            if (start == null)
            {
                start = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;
            }

            start = start.Value.GetWeekStart();
            var end = start.Value.PlusDays(6);

            var meals = _dbContext.Meals.Where(m =>
                m.ScheduleDay.UserId == userId && m.ScheduleDay.Date >= start.Value.ToDateTimeUnspecified() && m.ScheduleDay.Date <= end.ToDateTimeUnspecified());

            var targetDiet = _dietService.GetDietGoalsByUserId(userId, start);
            var goal = _dietService.GetTargetForDiet(userId, targetDiet.First().TargetDietId, start);

            var progress = meals.Count(m => m.ConfirmStatus == ConfirmStatus.CONFIRMED_YES);
            var challengesMet = progress > goal ? progress - goal : 0;

            return new StatSnapshot
            {
                UserId = userId,
                Goal = goal,
                Value = progress,
                Challenges = challengesMet,
                MealsPerDay = 0,
                Streak = GetStats(userId, 1).FirstOrDefault()?.Streak ?? 0,
                NodaWeek = start.Value
            };
        }

        public List<StatSnapshot> GetStats(int userId, int howManyWeeks = 9)
        {
            var stats = _dbContext.StatSnapshots.Where(s => s.UserId == userId).OrderByDescending(s => s.Week).Take(howManyWeeks)
                .ToList();

            stats.Reverse();

            return stats;
        }

        public void ProcessWeekStats()
        {
            var users = _dbContext.MenuPreferences.Select(m => m.UserId).ToList();
            var zone = _serviceProvider.GetService<RequestContext>().Dtz;

            for (var i = 0; i < users.Count; i++)
            {
                var progress = GetProgress(users[i], SystemClock.Instance.GetCurrentInstant().InZone(zone).Date.PlusDays(-6));

                var snapshot = _dbContext.StatSnapshots.FirstOrDefault(s => s.UserId == users[i] && s.Week == progress.Week);

                if (snapshot == null)
                {
                    snapshot = progress;
                    _dbContext.Add(snapshot);
                }
                else
                {
                    snapshot.Goal = progress.Goal;
                    snapshot.Value = progress.Value;
                    snapshot.Streak = progress.Streak;
                    snapshot.Challenges = progress.Challenges;
                }
            }

            _dbContext.SaveChanges();
        }
    }
}
