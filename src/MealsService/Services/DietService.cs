using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Models;
using Microsoft.EntityFrameworkCore;

namespace MealsService.Services
{
    public class DietService
    {
        private MealsDbContext _dbContext;

        public DietService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<DietGoal> GetDietGoalsByUserId(int userId)
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
