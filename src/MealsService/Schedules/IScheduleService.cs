using System.Collections.Generic;
using System.Threading.Tasks;

using NodaTime;

using MealsService.Requests;
using MealsService.Responses.Schedules;
using MealsService.Schedules.Data;
using MealsService.Schedules.Dtos;

namespace MealsService.Schedules
{
    public interface IScheduleService
    {
        /// <summary>
        /// Get user's schedule for the given day. This will NOT generate if missing
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        Task<List<ScheduleDayDto>> GetScheduleAsync(int userId, LocalDate start);
        /// <summary>
        /// Retrieves preparation, by id, verifying that it belongs to given user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="preparationId"></param>
        /// <returns></returns>
        PreparationDto GetPreparation(int userId, int preparationId);

        /// <summary>
        /// Set or Replace the recipe for a given preparation.
        /// Will update all meals attached to this preparation
        /// Validate:
        /// * slot belongs to specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prepId"></param>
        /// <param name="recipeId"></param>
        /// <returns></returns>
        Task SetPreparationRecipeAsync(int userId, int prepId, int recipeId);
        /// <summary>
        /// Set the number of servings for a given meal. Validate the slot belongs to specified user
        /// Validate:
        /// * slot belongs to specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="slotId"></param>
        /// <param name="numServings"></param>
        /// <returns></returns>
        Task<bool> UpdateServings(int userId, int slotId, int numServings);
        
        /// <summary>
        /// Set the day to be a Challenge Day. Does not add meal(s) to the day
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="date"></param>
        /// <param name="isChallenge"></param>
        /// <returns></returns>
        //Task<ScheduleDayDto> SetChallengeDayAsync(int userId, LocalDate date, bool isChallenge);
        
            
            
            /// <summary>
        /// Set the confirmation of the meal.
        /// Validate:
        /// * slot belongs to specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="mealId"></param>
        /// <param name="confirm"></param>
        /// <returns></returns>
        Task<bool> ConfirmMealAsync(int userId, int mealId, ConfirmStatus confirm);
        /// <summary>
        /// Move a meal to a given day.
        /// Validate:
        /// * meal is not before the preparation
        /// * slot belongs to specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="slotId"></param>
        /// <param name="dayId"></param>
        /// <returns></returns>
        Task<bool> MoveMealAsync(int userId, int slotId, int dayId);
        /// <summary>
        /// Move preparation to a given day.
        /// Validate:
        /// * slot belongs to specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prepId"></param>
        /// <param name="dayId"></param>
        /// <returns></returns>
        Task<bool> MovePreparationAsync(int userId, int prepId, int dayId);


        Task GenerateScheduleAsync(int userId, LocalDate start, LocalDate end, GenerateScheduleRequest request);
        
        
        
        
        ScheduleDayDto ToScheduleDayDto(ScheduleDay day);
        MealDto ToMealDto(Meal meal);
        PreparationDto ToPreparationDto(Preparation prep);
    }
}