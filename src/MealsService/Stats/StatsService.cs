
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Common.Extensions;
using MealsService.Diets;
using MealsService.Schedules.Data;
using MealsService.Stats.Data;

namespace MealsService.Stats
{
    public class StatsService
    {
        private MealsDbContext _dbContext;

        private DietService _dietService;

        public StatsService(MealsDbContext dbContext, DietService dietService)
        {
            _dbContext = dbContext;
            _dietService = dietService;
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

        public StatSnapshot GetProgress(int userId, DateTime? start = null)
        {
            if (start == null)
            {
                start = DateTime.UtcNow;
            }

            start = start.GetWeekStart();
            var end = start.Value.AddDays(7);

            var meals = _dbContext.Meals.Where(m =>
                m.ScheduleDay.UserId == userId && m.ScheduleDay.Date >= start && m.ScheduleDay.Date < end);

            var goal = meals.Count(m => !m.IsChallenge);
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
                Week = start.Value
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

            for (var i = 0; i < users.Count; i++)
            {
                var progress = GetProgress(users[i], DateTime.UtcNow.AddDays(-6));

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
