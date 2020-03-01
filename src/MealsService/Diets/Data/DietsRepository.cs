using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;

namespace MealsService.Diets.Data
{
    public class DietsRepository
    {
        private MealsDbContext _dbContext;
        private DietTypeService _dietTypesService;

        public DietsRepository(MealsDbContext dbContext, DietTypeService dietTypesService)
        {
            _dbContext = dbContext;
            _dietTypesService = dietTypesService;
        }

        public MenuPreference GetMenuPreference(int userId)
        {
            var preference = _dbContext.MenuPreferences.Find(userId);

            if(preference == null)
            {
                preference = DefaultMenuPreference(userId);
                _dbContext.MenuPreferences.Add(preference);
                if(_dbContext.SaveChanges() == 0)
                {
                    preference = null;
                }
            }

            return preference;
        }

        public List<DietGoal> GetDietGoals(int userId)
        {
            return _dbContext.DietGoals.Where(g => g.UserId == userId).ToList();
        }

        public PrepPlan GetPrepPlan(int userId, int targetDays)
        {
            return _dbContext.PrepPlans
                .Include(p => p.Generators)
                    .ThenInclude(g => g.Consumers)
                .Include(p => p.Consumers)
                .FirstOrDefault(d => d.UserId == userId && d.NumTargetDays == targetDays);
        }

        public void UpdatePrepPlan(PrepPlan plan, List<PrepPlanGenerator> removedGenerators, List<PrepPlanConsumer> removedConsumers)
        {
            if (removedConsumers != null && removedConsumers.Any())
            {
                _dbContext.PrepPlanConsumers.RemoveRange(removedConsumers);
            }
            if (removedGenerators != null && removedGenerators.Any())
            {
                _dbContext.PrepPlanGenerators.RemoveRange(removedGenerators);
            }

            if (plan != null)
            {
                if (plan.Id > 0 )
                {
                    _dbContext.PrepPlans.Update(plan);
                }
                else
                {
                    _dbContext.PrepPlans.Add(plan);
                }
            }

            _dbContext.SaveChanges();
        }

        public void RemoveGenerators(List<PrepPlanConsumer> consumers)
        {
            _dbContext.PrepPlanConsumers.RemoveRange(consumers);
            _dbContext.SaveChanges();
        }

        public List<ChangeDay> GetChangeDays(int userId, int targetDays)
        {
            return _dbContext.ChangeDays.Where(d => d.UserId == userId && d.TargetDays == targetDays).ToList();
        }

        public bool SetChangeDays(int userId, int targetDays, List<int> changeDaysOfWeek)
        {
            var changeDays = GetChangeDays(userId, targetDays);
            var changes = false;

            for (var i = 0; i < changeDaysOfWeek.Count; i++)
            {
                if (changeDays.Count < i + 1)
                {
                    _dbContext.Add(new ChangeDay
                    {
                        UserId = userId,
                        TargetDays = targetDays,
                        DayOfWeek = changeDaysOfWeek[i]
                    });
                    changes = true;
                }
                else
                {
                    changes = changes || changeDays[i].DayOfWeek != changeDaysOfWeek[i];
                    changeDays[i].DayOfWeek = changeDaysOfWeek[i];
                }
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool AddDietGoal(int userId, DietGoal goal)
        {
            var goals = GetDietGoals(userId);

            if(goals.Any(g => g.TargetDietId == goal.TargetDietId))
            {
                return false;
            }

            goal.UserId = userId;

            _dbContext.DietGoals.Add(goal);
            return _dbContext.SaveChanges() > 0; 
        }

        public bool UpdateDietGoal(int userId, DietGoal update)
        {
            var goal = GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == update.TargetDietId);

            if (goal == null)
            {
                return false;
            }

            goal.ReductionRate = update.ReductionRate;
            goal.Target = update.Target;
            goal.Current = update.Current;

            return _dbContext.Entry(goal).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public bool RemoveDietGoal(int userId, int targetDietId)
        {
            var goal = GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == targetDietId);

            if(goal == null)
            {
                return true;
            }

            _dbContext.Remove(goal);
            return _dbContext.SaveChanges() > 0;
        }

        public bool UpdateMenuPreference(int userId, MenuPreference update)
        {
            var preference = GetMenuPreference(userId);

            preference.RecipeStyle = update.RecipeStyle;
            preference.MealTypes = update.MealTypes;
            preference.ShoppingFreq = update.ShoppingFreq;

            return _dbContext.Entry(preference).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public MenuPreference DefaultMenuPreference(int userId)
        {
            return new MenuPreference
            {
                UserId = userId,
                RecipeStyle = RecipeStyle.ChefSpecial,
                MealTypes = new List<MealType> { MealType.Dinner },
                CurrentDietTypeId = _dietTypesService.ListDietTypes().First().Id
            };
        }

        public DietGoal DefaultDietGoal(int userId)
        {
            return new DietGoal
            {
                UserId = userId,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Current = 1,
                Target = 7,
                ReductionRate = ReductionRate.Weekly,
                TargetDietId = 3
            };
        }
    }
}
