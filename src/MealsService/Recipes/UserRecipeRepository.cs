using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;

namespace MealsService.Recipes
{
    public class UserRecipeRepository : IUserRecipeRepository
    {
        private MealsDbContext _dbContext;

        public UserRecipeRepository(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<RecipeVote> GetUserVotes(int userId)
        {
            return _dbContext.RecipeVotes.Where(v => v.UserId == userId).ToList();
        }

        public List<RecipeVote> GetRecipeVotes(int recipeId)
        {
            return _dbContext.RecipeVotes.Where(v => v.RecipeId == recipeId).ToList();
        }


        public bool SaveVote(RecipeVote vote)
        {
            if (vote.Id > 0)
            {
                var tracked = _dbContext.ChangeTracker.Entries<RecipeVote>()
                    .FirstOrDefault(m => m.Entity.Id == vote.Id);
                if (tracked != null)
                {
                    _dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                _dbContext.RecipeVotes.Attach(vote);
                _dbContext.Entry(vote).State = EntityState.Modified;
            }
            else
            {
                _dbContext.RecipeVotes.Add(vote);
            }

            return _dbContext.SaveChanges() > 0;
        }
    }
}
