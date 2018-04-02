using System;
using System.Collections.Generic;
using System.Linq;

using MealsService.Diets.Dtos;
using MealsService.Diets.Data;
using MealsService.Recipes.Data;

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

            preference.RecipeStyle = update.RecipeStyle;
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

            if (dietGoal == null)
            {
                dietGoal = new DietGoal
                {
                    ReductionRate = ReductionRate.Monthly,
                    Target = 1,
                    Current = 1,
                    TargetDietId = 3,
                    UserId = userId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };
            }

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

        public PrepPlan GetPrepPlan(int userId, DateTime when)
        {
            var primaryGoal = GetDietGoalsByUserId(userId, when).FirstOrDefault();
            var prepPlan = _repository.GetPrepPlan(userId, primaryGoal.Current);

            if (prepPlan == null)
            {
                var prevPlan = _repository.GetPrepPlan(userId, primaryGoal.Current - 1);

                if (prevPlan != null)
                {
                    prepPlan = prevPlan;
                }
                else
                {
                    prepPlan = new PrepPlan
                    {
                        UserId = userId,
                        NumTargetDays = primaryGoal.Current,
                        Generators = new List<PrepPlanGenerator>(),
                        Consumers = new List<PrepPlanConsumer>()
                    };
                }
            }

            var changeDays = GetChangeDays(userId, when);
            var prefs = GetPreferences(userId);

            var daysPlanned = prepPlan.Consumers.Select(c => c.DayOfWeek).Distinct().ToList();
            var missingDayCount = primaryGoal.Current - daysPlanned.Count();

            for (var i = 0; i < missingDayCount; i++)
            {
                var addedDay = changeDays.Except(daysPlanned).First();
                daysPlanned.Add(addedDay);

                foreach (var mealType in prefs.MealTypes)
                {
                    var generator = new PrepPlanGenerator
                    {
                        DayOfWeek = addedDay,
                        MealType = mealType,
                        NumServings = 2,
                        Consumers = new List<PrepPlanConsumer>()
                    };

                    generator.Consumers.Add(
                        new PrepPlanConsumer
                        {
                            DayOfWeek = addedDay,
                            MealType = mealType,
                            NumServings = 2,
                            Generator = generator
                        });

                    prepPlan.Generators.Add(generator);
                    prepPlan.Consumers.AddRange(generator.Consumers);
                }
            }

            return prepPlan;
        }

        public List<PrepPlanDay> GetPrepPlanDtos(int userId, DateTime when)
        {
            var prepPlan = GetPrepPlan(userId, when);

            var consumerDays = prepPlan.Consumers.Select(c => c.DayOfWeek).Distinct().OrderBy(d => d).ToList();
            var prepPlanDays = new List<PrepPlanDay>();

            for (var i = 0; i < consumerDays.Count(); i++)
            {
                var day = new PrepPlanDay
                {
                    DayOfWeek = consumerDays[i],
                    Meals = new List<PrepPlanMeal>()
                };

                var dayConsumers = prepPlan.Consumers.Where(c => c.DayOfWeek == day.DayOfWeek).ToList();

                for (var j = 0; j < dayConsumers.Count; j++)
                {
                    var generator = dayConsumers[j].Generator ?? prepPlan.Generators.FirstOrDefault(g => g.Id == dayConsumers[j].GeneratorId);
                    var meal = new PrepPlanMeal
                    {
                        MealType = dayConsumers[j].MealType,
                        NumServings = dayConsumers[j].NumServings,
                        PreppedDay = generator.DayOfWeek,
                        PreppedMeal = generator.MealType
                    };
                    day.Meals.Add(meal);
                }

                day.Meals = day.Meals.OrderBy(m => m.MealType).ToList();
                prepPlanDays.Add(day);
            }

            return prepPlanDays;
        }

        public void UpdatePrepPlanDays(int userId, List<PrepPlanDay> days)
        {
            var newGenerators = FromDtos(days);

            var plan = GetPrepPlan(userId, DateTime.UtcNow);
            var removedGenerators = new List<PrepPlanGenerator>();
            var removedConsumers = new List<PrepPlanConsumer>();

            for (var i = 0; i < newGenerators.Count; i++)
            {
                if (plan.Generators.Count > i)
                {
                    plan.Generators[i].DayOfWeek = newGenerators[i].DayOfWeek;
                    plan.Generators[i].MealType = newGenerators[i].MealType;
                    plan.Generators[i].NumServings = newGenerators[i].NumServings;
                }
                else
                {
                    plan.Generators.Add(new PrepPlanGenerator
                    {
                        DayOfWeek = newGenerators[i].DayOfWeek,
                        MealType = newGenerators[i].MealType,
                        NumServings = newGenerators[i].NumServings,
                        Consumers = new List<PrepPlanConsumer>()
                    });
                }

                for (var j = 0; j < newGenerators[i].Consumers.Count; j++)
                {
                    if (plan.Generators[i].Consumers.Count > j)
                    {
                        plan.Generators[i].Consumers[j].MealType = newGenerators[i].Consumers[j].MealType;
                        plan.Generators[i].Consumers[j].DayOfWeek = newGenerators[i].Consumers[j].DayOfWeek;
                        plan.Generators[i].Consumers[j].NumServings = newGenerators[i].Consumers[j].NumServings;
                    }
                    else
                    {
                        plan.Generators[i].Consumers.Add(new PrepPlanConsumer
                        {
                            MealType = newGenerators[i].Consumers[j].MealType,
                            DayOfWeek = newGenerators[i].Consumers[j].DayOfWeek,
                            NumServings = newGenerators[i].Consumers[j].NumServings
                        });
                    }
                }

                if (newGenerators[i].Consumers.Count < plan.Generators[i].Consumers.Count)
                {
                    var diff = plan.Generators[i].Consumers.Count - newGenerators[i].Consumers.Count;
                    removedConsumers.AddRange(plan.Generators[i].Consumers.TakeLast(diff).Where(c => c.Id > 0));
                    plan.Generators[i].Consumers.RemoveRange(newGenerators[i].Consumers.Count, diff);
                }
            }

            if (newGenerators.Count < plan.Generators.Count)
            {
                var diff = plan.Generators.Count - newGenerators.Count;
                removedGenerators.AddRange(plan.Generators.TakeLast(diff).Where(g => g.Id > 0));
                plan.Generators.RemoveRange(newGenerators.Count, diff);
            }

            plan.Consumers = plan.Generators.SelectMany(g => g.Consumers).ToList();

            _repository.UpdatePrepPlan(plan, removedGenerators, removedConsumers);
        }

        public List<PrepPlanGenerator> FromDtos(List<PrepPlanDay> prepPlanDays)
        {
            var generators = new Dictionary<int, Dictionary<MealType, PrepPlanGenerator>>();

            foreach (var day in prepPlanDays)
            {
                foreach (var meal in day.Meals)
                {
                    if (meal.PreppedDay == day.DayOfWeek && meal.PreppedMeal == meal.MealType)
                    {
                        var generator = new PrepPlanGenerator
                        {
                            DayOfWeek = day.DayOfWeek,
                            MealType = meal.MealType,
                            NumServings = 0,
                            Consumers = new List<PrepPlanConsumer>()
                        };

                        if (!generators.ContainsKey(day.DayOfWeek))
                        {
                            generators.Add(day.DayOfWeek, new Dictionary<MealType, PrepPlanGenerator>());
                        }
                        if (!generators[day.DayOfWeek].ContainsKey(meal.MealType))
                        {
                            generators[day.DayOfWeek].Add(meal.MealType, generator);
                        }
                        else
                        {
                            throw new Exception("Should only be one meal per MealType");
                        }
                    }

                    var consumer = new PrepPlanConsumer
                    {
                        DayOfWeek = day.DayOfWeek,
                        MealType = meal.MealType,
                        NumServings = meal.NumServings
                    };
                    generators[meal.PreppedDay][meal.PreppedMeal].Consumers.Add(consumer);
                    generators[meal.PreppedDay][meal.PreppedMeal].NumServings =
                        generators[meal.PreppedDay][meal.PreppedMeal].Consumers.Sum(c => c.NumServings);
                }
            }

            return generators.SelectMany(d => d.Value.Values).ToList();
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
                RecipeStyle = model.RecipeStyle,
                MealTypes = model.MealTypes,
                CurrentDietId = model.CurrentDietTypeId
            };
        }

        protected PrepPlanGeneratorDto ToDto(PrepPlanGenerator generator)
        {
            return new PrepPlanGeneratorDto
            {
                DayOfWeek = generator.DayOfWeek,
                MealType = generator.MealType,
                NumServings = generator.NumServings,
                Consumers = generator.Consumers.Select(c => ToDto(c)).ToList()
            };
        }

        protected PrepPlanConsumerDto ToDto(PrepPlanConsumer consumer)
        {
            return new PrepPlanConsumerDto
            {
                DayOfWeek = consumer.DayOfWeek,
                MealType = consumer.MealType,
                NumServings = consumer.NumServings
            };
        }
    }
}
