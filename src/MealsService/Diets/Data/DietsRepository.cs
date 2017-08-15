
using MealsService.Recipes.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MealsService.Diets.Data
{
    public class DietsRepository
    {
        private MealsDbContext _context;

        public DietsRepository(MealsDbContext context)
        {
            _context = context;
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

        public bool AddDietGoal(int userId, DietGoal goal)
        {
            var goals = GetDietGoals(userId);

            if(goals.Any(g => g.TargetDietId == goal.TargetDietId))
            {
                return false;
            }

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

            preference.MealStyle = update.MealStyle;
            preference.MealTypes = update.MealTypes;
            preference.ShoppingFreq = update.ShoppingFreq;

            return _context.Entry(preference).State == EntityState.Unchanged || _context.SaveChanges() > 0;
        }

        public MenuPreference DefaultMenuPreference(int userId)
        {
            return new MenuPreference
            {
                UserId = userId,
                MealStyle = MealStyle.ChefSpecial,
                MealTypes = new List<Meal.Type> { Meal.Type.Dinner },
                CurrentDietTypeId = _context.DietTypes.FirstOrDefault().Id
            };
        }
    }
}
