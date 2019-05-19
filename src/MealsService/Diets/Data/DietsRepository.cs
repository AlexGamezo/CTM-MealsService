
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using MealsService.Recipes.Data;

namespace MealsService.Diets.Data
{
    public class DietsRepository
    {
        private MealsDbContext _context;
        private DietTypeService _dietTypesService;

        public DietsRepository(MealsDbContext context, DietTypeService dietTypesService)
        {
            _context = context;
            _dietTypesService = dietTypesService;
        }

        public MenuPreference GetMenuPreference(int userId)
        {
            var preference = _context.MenuPreferences.Find(userId);

            if(preference == null)
            {
                preference = DefaultMenuPreference(userId);
                _context.MenuPreferences.Add(preference);
                if(_context.SaveChanges() == 0)
                {
                    preference = null;
                }
            }

            return preference;
        }

        public List<DietGoal> GetDietGoals(int userId)
        {
            return _context.DietGoals.Where(g => g.UserId == userId).ToList();
        }

        public PrepPlan GetPrepPlan(int userId, int targetDays)
        {
            return _context.PrepPlans
                .Include(p => p.Generators)
                    .ThenInclude(g => g.Consumers)
                .Include(p => p.Consumers)
                .FirstOrDefault(d => d.UserId == userId && d.NumTargetDays == targetDays);
        }

        public void UpdatePrepPlan(PrepPlan plan, List<PrepPlanGenerator> removedGenerators, List<PrepPlanConsumer> removedConsumers)
        {
            if (removedConsumers != null && removedConsumers.Any())
            {
                _context.PrepPlanConsumers.RemoveRange(removedConsumers);
            }
            if (removedGenerators != null && removedGenerators.Any())
            {
                _context.PrepPlanGenerators.RemoveRange(removedGenerators);
            }

            if (plan != null)
            {
                if (plan.Id > 0 )
                {
                    _context.PrepPlans.Update(plan);
                }
                else
                {
                    _context.PrepPlans.Add(plan);
                }
            }

            _context.SaveChanges();
        }

        public void RemoveGenerators(List<PrepPlanConsumer> consumers)
        {
            _context.PrepPlanConsumers.RemoveRange(consumers);
            _context.SaveChanges();
        }

        public List<ChangeDay> GetChangeDays(int userId, int targetDays)
        {
            return _context.ChangeDays.Where(d => d.UserId == userId && d.TargetDays == targetDays).ToList();
        }

        public bool SetChangeDays(int userId, int targetDays, List<int> changeDaysOfWeek)
        {
            var changeDays = GetChangeDays(userId, targetDays);
            var changes = false;

            for (var i = 0; i < changeDaysOfWeek.Count; i++)
            {
                if (changeDays.Count < i + 1)
                {
                    _context.Add(new ChangeDay
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

            return !changes || _context.SaveChanges() > 0;
        }

        public bool AddDietGoal(int userId, DietGoal goal)
        {
            var goals = GetDietGoals(userId);

            if(goals.Any(g => g.TargetDietId == goal.TargetDietId))
            {
                return false;
            }

            goal.UserId = userId;

            _context.DietGoals.Add(goal);
            return _context.SaveChanges() > 0; 
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

            return _context.Entry(goal).State == EntityState.Unchanged || _context.SaveChanges() > 0;
        }

        public bool RemoveDietGoal(int userId, int targetDietId)
        {
            var goal = GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == targetDietId);

            if(goal == null)
            {
                return true;
            }

            _context.Remove(goal);
            return _context.SaveChanges() > 0;
        }

        public bool UpdateMenuPreference(int userId, MenuPreference update)
        {
            var preference = GetMenuPreference(userId);

            preference.RecipeStyle = update.RecipeStyle;
            preference.MealTypes = update.MealTypes;
            preference.ShoppingFreq = update.ShoppingFreq;

            return _context.Entry(preference).State == EntityState.Unchanged || _context.SaveChanges() > 0;
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
