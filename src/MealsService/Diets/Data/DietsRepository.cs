using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using MealsService.Recipes.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Diets.Data
{
    public class DietsRepository
    {
        private IServiceProvider _serviceProvider;
        private DietTypeService _dietTypesService;

        public DietsRepository(IServiceProvider serviceProvider, DietTypeService dietTypesService)
        {
            _serviceProvider = serviceProvider;
            _dietTypesService = dietTypesService;
        }

        public MenuPreference GetMenuPreference(int userId)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();
            var preference = context.MenuPreferences.Find(userId);

            if(preference == null)
            {
                preference = DefaultMenuPreference(userId);
                context.MenuPreferences.Add(preference);
                if(context.SaveChanges() == 0)
                {
                    preference = null;
                }
            }

            return preference;
        }

        public List<DietGoal> GetDietGoals(int userId)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();
            return context.DietGoals.Where(g => g.UserId == userId).ToList();
        }

        public PrepPlan GetPrepPlan(int userId, int targetDays)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();
            return context.PrepPlans
                .Include(p => p.Generators)
                    .ThenInclude(g => g.Consumers)
                .Include(p => p.Consumers)
                .FirstOrDefault(d => d.UserId == userId && d.NumTargetDays == targetDays);
        }

        public void UpdatePrepPlan(PrepPlan plan, List<PrepPlanGenerator> removedGenerators, List<PrepPlanConsumer> removedConsumers)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            if (removedConsumers != null && removedConsumers.Any())
            {
                context.PrepPlanConsumers.RemoveRange(removedConsumers);
            }
            if (removedGenerators != null && removedGenerators.Any())
            {
                context.PrepPlanGenerators.RemoveRange(removedGenerators);
            }

            if (plan != null)
            {
                if (plan.Id > 0 )
                {
                    context.PrepPlans.Update(plan);
                }
                else
                {
                    context.PrepPlans.Add(plan);
                }
            }

            context.SaveChanges();
        }

        public void RemoveGenerators(List<PrepPlanConsumer> consumers)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            context.PrepPlanConsumers.RemoveRange(consumers);
            context.SaveChanges();
        }

        public List<ChangeDay> GetChangeDays(int userId, int targetDays)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            return context.ChangeDays.Where(d => d.UserId == userId && d.TargetDays == targetDays).ToList();
        }

        public bool SetChangeDays(int userId, int targetDays, List<int> changeDaysOfWeek)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            var changeDays = GetChangeDays(userId, targetDays);
            var changes = false;

            for (var i = 0; i < changeDaysOfWeek.Count; i++)
            {
                if (changeDays.Count < i + 1)
                {
                    context.Add(new ChangeDay
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

            return !changes || context.SaveChanges() > 0;
        }

        public bool AddDietGoal(int userId, DietGoal goal)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            var goals = GetDietGoals(userId);

            if(goals.Any(g => g.TargetDietId == goal.TargetDietId))
            {
                return false;
            }

            goal.UserId = userId;

            context.DietGoals.Add(goal);
            return context.SaveChanges() > 0; 
        }

        public bool UpdateDietGoal(int userId, DietGoal update)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            var goal = GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == update.TargetDietId);

            if (goal == null)
            {
                return false;
            }

            goal.ReductionRate = update.ReductionRate;
            goal.Target = update.Target;
            goal.Current = update.Current;

            return context.Entry(goal).State == EntityState.Unchanged || context.SaveChanges() > 0;
        }

        public bool RemoveDietGoal(int userId, int targetDietId)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();
            var goal = GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == targetDietId);

            if(goal == null)
            {
                return true;
            }

            context.Remove(goal);
            return context.SaveChanges() > 0;
        }

        public bool UpdateMenuPreference(int userId, MenuPreference update)
        {
            var context = _serviceProvider.GetService<MealsDbContext>();

            var preference = GetMenuPreference(userId);

            preference.RecipeStyle = update.RecipeStyle;
            preference.MealTypes = update.MealTypes;
            preference.ShoppingFreq = update.ShoppingFreq;

            return context.Entry(preference).State == EntityState.Unchanged || context.SaveChanges() > 0;
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
