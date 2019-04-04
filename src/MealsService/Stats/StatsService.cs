
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Common.Extensions;
using MealsService.Diets;
using MealsService.Infrastructure;
using MealsService.Schedules.Data;
using MealsService.Stats.Data;
using MealsService.Stats.Extensions;
using MealsService.Users;
using MealsService.Users.Data;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.Stats
{
    public class StatsService
    {
        private MealsDbContext _dbContext;

        private DietService _dietService;
        private IServiceProvider _serviceProvider;

        private static int JOURNEY_MEAL_COMPLETION_ID = 1;

        public StatsService(MealsDbContext dbContext, DietService dietService, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _dietService = dietService;
            _serviceProvider = serviceProvider;
        }

        public List<PersonalizedStatement> GetImpactStatements(int userId, List<ImpactType> types = null)
        {
            IQueryable<ImpactStatement> statementsQuery = _dbContext.ImpactStatements;

            if (types != null && types.Any())
            {
                statementsQuery = statementsQuery.Where(s => types.Contains(s.ImpactType));
            }

            var summary = GetSummaryStats(userId);

            if (summary == null || summary.NumMeals == 0)
            {
                statementsQuery = statementsQuery.Where(s => s.CalcType == CalculationType.STATIC);
            }
            else
            {
                statementsQuery = statementsQuery.Where(s => s.CalcType != CalculationType.STATIC);
            }

            var allStatements = statementsQuery.ToList();
            var statements = new List<ImpactStatement>();
            var rand = new Random();

            for (var i = 0; i < 3 && allStatements.Count > 0; i++)
            {
                var randIndex = rand.Next(allStatements.Count);
                statements.Add(allStatements[randIndex]);
                allStatements.RemoveAt(randIndex);
            }
 
            return PersonalizeStatements(userId, statements);
        }

        public async Task TrackCompletionAsync(int userId, int increment, bool isChallenge)
        {
            if (increment != 0)
            {
                var progress = GetProgress(userId);

                var summary = _dbContext.StatSummaries.FirstOrDefault(s => s.UserId == userId);

                if (summary == null)
                {
                    summary = new StatSummary
                    {
                        UserId = userId,
                    };
                    _dbContext.StatSummaries.Add(summary);
                }

                summary.NumMeals = Math.Max(0, summary.NumMeals + increment);
                summary.NumChallenges = Math.Max(0, summary.NumChallenges + (isChallenge ? increment : 0));
                summary.CurrentStreak = progress.Streak;
                summary.MealsPerWeek = progress.Goal;

                if (summary.NumMeals <= 1)
                {
                    var updateRequest = new UpdateJourneyProgressRequest
                    {
                        JourneyStepId = JOURNEY_MEAL_COMPLETION_ID,
                        Completed = summary.NumMeals == 1
                    };
                    await _serviceProvider.GetService<UsersService>().UpdateJourneyProgressAsync(userId, updateRequest);
                }

                _dbContext.SaveChanges();
            }
        }

        private List<PersonalizedStatement> PersonalizeStatements(int userId, List<ImpactStatement> impactStatements)
        {
            var summary = GetSummaryStats(userId);

            var personalizedStatements = new List<PersonalizedStatement>();

            foreach (var statement in impactStatements)
            {
                personalizedStatements.Add(statement.PersonalizeStatement(userId, summary));
            }

            return personalizedStatements;
        }

        public StatSummary GetSummaryStats(int userId)
        {
            var summary = _dbContext.StatSummaries.FirstOrDefault(s => s.UserId == userId);

            return summary;
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

            //TODO: This should be in meals service, requestable
            var meals = _dbContext.Meals
                .Where(m => m.ScheduleDay.UserId == userId &&
                            m.ScheduleDay.Date >= start.Value.ToDateTimeUnspecified() &&
                            m.ScheduleDay.Date <= end.ToDateTimeUnspecified());

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
                Streak = progress >= goal ? GetStats(userId, 1).FirstOrDefault()?.Streak ?? 0 + 1 : 0,
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

        public async Task ProcessWeekStatsAsync()
        {
            var users = _dbContext.MenuPreferences.Select(m => m.UserId).ToList();
            var requestContextFactory = _serviceProvider.GetService<RequestContextFactory>();

            //start context for the user
                //load user and profile for timezone
                //
            for (var i = 0; i < users.Count; i++)
            {
                await requestContextFactory.StartRequestContext(users[i]);

                var context = _serviceProvider.GetService<RequestContext>();
                if (context.UserId != 0)
                {
                    var zone = context.Dtz;
                    var progress = GetProgress(users[i],
                        SystemClock.Instance.GetCurrentInstant().InZone(zone).Date.PlusDays(-6));

                    var snapshot =
                        _dbContext.StatSnapshots.FirstOrDefault(s => s.UserId == users[i] && s.Week == progress.Week);

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
            }

            _serviceProvider.GetService<RequestContextFactory>().ClearContext();

            _dbContext.SaveChanges();
        }
    }
}
