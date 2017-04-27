using System.Collections.Generic;
using System.Linq;
using MealsService.Models;
using Microsoft.EntityFrameworkCore;
using MealsService.Responses;

namespace MealsService.Services
{
    public class DietService
    {
        private MealsDbContext _dbContext;
        private DietTypeService _dietTypeService;

        public DietService(MealsDbContext dbContext, DietTypeService dietTypeService)
        {
            _dbContext = dbContext;
            _dietTypeService = dietTypeService;
        }

        public bool UpdatePreferences(int userId, MenuPreferencesDto updateRequest)
        {
            var preferences = GetPreferences(userId);

            preferences.MealStyle = updateRequest.MealStyle;
            preferences.MealTypes = updateRequest.MealTypes;
            preferences.ShoppingFreq = updateRequest.ShoppingFrequency;

            return _dbContext.Entry(preferences).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public bool UpdateDietGoals(int userId, MenuPreferencesDto updateRequest)
        {
            var dietGoals = GetDietGoalsByUserId(userId);
            var changes = false;

            for(var i = 0; i < updateRequest.DietGoals.Count; i++)
            {
                var update = updateRequest.DietGoals[i];
                if(dietGoals.Count > i)
                {
                    var targetDiet = _dietTypeService.GetDietType(update.TargetDiet);
                    if (dietGoals[i].Current != update.Current)
                    {
                        dietGoals[i].Current = update.Current;
                        changes = true;
                    }
                    if (dietGoals[i].Target != update.Target)
                    {
                        dietGoals[i].Target = update.Target;
                        changes = true;
                    }
                    if (dietGoals[i].ReductionRate != update.ReductionRate)
                    {
                        dietGoals[i].ReductionRate = update.ReductionRate;
                        changes = true;
                    }
                    if (targetDiet != null && dietGoals[i].TargetDietId != targetDiet.Id)
                    {
                        dietGoals[i].TargetDietId = targetDiet.Id;
                        changes = true;
                    }
                }
                else
                {
                    _dbContext.Add(new DietGoal
                    {
                        UserId = userId,
                        Current = update.Current,
                        Target = update.Target,
                        ReductionRate = update.ReductionRate,
                        TargetDietId = _dietTypeService.GetDietType(update.TargetDiet).Id
                    });
                    changes = true;
                }
            }

            if(updateRequest.DietGoals.Count < dietGoals.Count)
            {
                for(var j = updateRequest.DietGoals.Count; j < dietGoals.Count; j++)
                {
                    _dbContext.Remove(dietGoals[j]);
                }
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public List<DietGoal> GetDietGoalsByUserId(int userId)
        {
            var dietGoals = _dbContext.DietGoals.Include(dg => dg.TargetDietType)
                .Where(dg => dg.UserId == userId).ToList();

            if (!dietGoals.Any())
            {
                _dbContext.DietGoals.Add(new DietGoal
                {
                    UserId = userId,
                    Current = 7,
                    ReductionRate = ReductionRate.Monthly,
                    Target = 7,
                    TargetDietId = 1
                });
                _dbContext.SaveChanges();

                dietGoals = _dbContext.DietGoals.Include(dg => dg.TargetDietType)
                .Where(dg => dg.UserId == userId).ToList();
            }

            return dietGoals;
        }

        public MenuPreference GetPreferences(int userId)
        {
            var preferences = _dbContext.MenuPreferences.FirstOrDefault(p => p.UserId == userId);

            if (preferences == null)
            {
                preferences = new MenuPreference
                {
                    UserId = userId,
                    MealStyle = MealStyle.ChefSpecial,
                    ShoppingFreq = 1
                };
                _dbContext.MenuPreferences.Add(preferences);
                _dbContext.SaveChanges();
            }

            return preferences;
        }
    }
}
