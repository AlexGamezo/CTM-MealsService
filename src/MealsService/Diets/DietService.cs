using System;
using System.Collections.Generic;
using System.Linq;

using MealsService.Diets.Dtos;
using MealsService.Diets.Data;

namespace MealsService.Diets
{
    public class DietService
    {
        private Dictionary<int, List<int>> DefaultChangeDays = new Dictionary<int, List<int>>
        {
            //Monday
            { 1, new List<int>{ 0 } },
            //Monday & Thursday
            { 2, new List<int>{ 0, 3 } },
            //Monday, Thursday, & Sunday
            { 3, new List<int>{ 0, 3, 6 } },
            //Monday, Tuesday, Thursday, & Sunday
            { 4, new List<int>{ 0, 1, 3, 6 } },
            //Monday, Tuesday, Wednesday, Thursday, & Sunday
            { 5, new List<int>{ 0, 1, 2, 3, 6 } },
            //Monday, Tuesday, Wednesday, Thursday, Saturday, & Sunday
            { 6, new List<int>{ 0, 1, 2, 3, 5, 6 } },
            //Every day
            { 7, new List<int>{ 0, 1, 2, 3, 4, 5, 6 } },
        };

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

        public List<DietGoalDto> GetDietGoalsByUserId(int userId, DateTime? when = null)
        {
            var dietGoals = _repository.GetDietGoals(userId);

            if (!dietGoals.Any())
            {
                dietGoals.Add(_repository.DefaultDietGoal(userId));
            }
            else if (when.HasValue)
            {
                dietGoals.ForEach(g => g.Current = GetTargetForDiet(userId, g.TargetDietId, when));
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

            //TODO: This should be somehow based on weeks confirmed to be successes up to now, not a strict number of weeks passed
            //          to deal with people that are not engaged and come back 5 months later.
            var weeksPassed = (int) ((when - dietGoal.Updated).Value.TotalDays / 7);

            var scaled = dietGoal.Current + (weeksPassed / changeRate);

            return changeRate > 0 ? Math.Min(scaled, dietGoal.Target) : Math.Max(scaled, dietGoal.Target);
        }

        public List<int> GetChangeDays(int userId, DateTime when)
        {
            var primaryGoal = GetDietGoalsByUserId(userId, when).FirstOrDefault();

            var changeDays = _repository.GetChangeDays(userId, primaryGoal.Current);

            if (!changeDays.Any())
            {
                var earlierChangeDays = _repository.GetChangeDays(userId, primaryGoal.Current - 1);
                if (earlierChangeDays.Any())
                {
                    changeDays = earlierChangeDays;
                }
                else
                {
                    return DefaultChangeDays[primaryGoal.Current];
                }
            }

            var changeDaysOfWeek = changeDays.Select(d => d.DayOfWeek).ToList();

            if (changeDaysOfWeek.Count() < primaryGoal.Current)
            {
                changeDaysOfWeek.AddRange(DefaultChangeDays[primaryGoal.Current].Except(changeDaysOfWeek)
                    .Take(primaryGoal.Current - changeDaysOfWeek.Count()));
                _repository.SetChangeDays(userId, primaryGoal.Current, changeDaysOfWeek);
            }

            return changeDaysOfWeek;
        }

        public void SetChangeDays(int userId, DateTime when, List<int> changeDays)
        {
            var primaryGoal = GetDietGoalsByUserId(userId, when).FirstOrDefault();

            _repository.SetChangeDays(userId, primaryGoal.Current, changeDays);
        }

        public MenuPreferencesDto GetPreferences(int userId)
        {
            var preferences = _repository.GetMenuPreference(userId);

            return ToDto(preferences);
        }

        protected DietGoalDto ToDto(DietGoal model)
        {
            var dto = new DietGoalDto
            {
                TargetDietId = model.TargetDietId,
                Current = model.Current,
                Target = model.Target,
                ReductionRate = model.ReductionRate,
                Updated = (long)(new TimeSpan(model.Updated.Ticks)).TotalMilliseconds
            };

            return dto;
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
