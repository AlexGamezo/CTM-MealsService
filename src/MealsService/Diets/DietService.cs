using System;
using System.Collections.Generic;
using System.Linq;

using MealsService.Diets.Dtos;
using MealsService.Diets.Data;

namespace MealsService.Diets
{
    public class DietService
    {
        private DietsRepository _repository;
        private DietTypeService _dietTypeService;

        public DietService(MealsDbContext dbContext, DietTypeService dietTypeService)
        {
            _repository = new DietsRepository(dbContext);
            _dietTypeService = dietTypeService;
        }

        public bool UpdatePreferences(int userId, MenuPreferencesDto update)
        {
            var preference = _repository.GetMenuPreference(userId);

            preference.MealStyle = update.MealStyle;
            preference.MealTypes = update.MealTypes;
            preference.ShoppingFreq = update.ShoppingFreq;
            preference.CurrentDietTypeId = update.CurrentDietId;

            return _repository.UpdateMenuPreference(userId, preference);
        }

        public bool UpdateDietGoals(int userId, List<DietGoalDto> updates)
        {
            var dietGoals = _repository.GetDietGoals(userId);

            var currentDietTypeIds = dietGoals.Select(g => g.TargetDietId).ToList();
            var incomingDietTypeIds = updates.Select(g => g.TargetDietId).ToList();

            var removedGoals = dietGoals.Where(g => !incomingDietTypeIds.Contains(g.TargetDietId)).ToList();

            var success = true;
            foreach (var goal in removedGoals)
            {
                success = _repository.RemoveDietGoal(userId, goal.TargetDietId);

                if (!success)
                {
                    break;
                }
            }

            foreach(var goal in updates)
            {
                if (!success)
                {
                    break;
                }

                //TODO: Move to a "FromDto" method, for consistent single-point conversion
                var fromDto = new DietGoal
                {
                    UserId = userId,
                    TargetDietId = goal.TargetDietId,
                    Target = goal.Target,
                    Current = goal.Current,
                    ReductionRate = goal.ReductionRate
                };

                if (!currentDietTypeIds.Contains(goal.TargetDietId))
                {
                    success = _repository.AddDietGoal(userId, fromDto);
                }
                else
                {
                    success = _repository.UpdateDietGoal(userId, fromDto);
                }
            }

            return success;
        }

        public List<DietGoalDto> GetDietGoalsByUserId(int userId)
        {
            var dietGoals = _repository.GetDietGoals(userId);

            if (!dietGoals.Any())
            {
                dietGoals.Add(_repository.DefaultDietGoal(userId));
            }

            return dietGoals.Select(ToDto).ToList();
        }

        public int GetTargetForDiet(int userId, int dietTypeId, DateTime? when = null)
        {
            var dietGoal = _repository.GetDietGoals(userId).FirstOrDefault(g => g.TargetDietId == dietTypeId);

            if (!when.HasValue)
            {
                when = DateTime.UtcNow;
            }

            var changeRate = dietGoal.Current < dietGoal.Target ? 1 : -1;

            if (dietGoal.ReductionRate == ReductionRate.Biweekly) changeRate *= 2;
            if (dietGoal.ReductionRate == ReductionRate.Monthly) changeRate *= 4;

            var weeksPassed = (int) ((when - dietGoal.Updated).Value.TotalDays / 7);

            var scaled = dietGoal.Current + (weeksPassed / changeRate);

            return Math.Max(scaled, dietGoal.Target);
        }

        public MenuPreferencesDto GetPreferences(int userId)
        {
            var preferences = _repository.GetMenuPreference(userId);

            return ToDto(preferences);
        }

        protected DietGoalDto ToDto(DietGoal model)
        {
            return new DietGoalDto
            {
                TargetDietId = model.TargetDietId,
                Current = model.Current,
                Target = model.Target,
                ReductionRate = model.ReductionRate,
                Updated = (long)(new TimeSpan(model.Updated.Ticks)).TotalMilliseconds
            };
        }

        protected MenuPreferencesDto ToDto(MenuPreference model)
        {
            return new MenuPreferencesDto
            {
                ShoppingFreq = model.ShoppingFreq,
                MealStyle = model.MealStyle,
                MealTypes = model.MealTypes,
                CurrentDietId = model.CurrentDietTypeId
            };
        }
    }
}
