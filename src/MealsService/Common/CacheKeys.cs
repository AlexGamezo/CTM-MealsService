using System;

namespace MealsService.Common
{
    public static class CacheKeys
    {
        public static class Schedule
        {
            public static string UserSchedule(int userId, DateTime start) => $"sched.{userId}.{start:yyyy-MM-dd}";

        }

        public static class Recipes
        {
            public static string AllRecipes => $"recipes";
            public static string UserVotes(int userId) => $"votes.{userId}";
            public static string RecentGenerations(int userId) => $"recipes.recent.{userId}";
        }

        public static class DietTypes
        {
            public static string DietTypeList => "diettypes";
        }

        public static class Tags
        {
            public static string TagsList => "tagslist";
        }

        public static class Ingredients
        {
            public static string IngredientsList => "ingredients";
            public static string IngredientCategoriesList => "ingredientcats";
        }
    }
}
