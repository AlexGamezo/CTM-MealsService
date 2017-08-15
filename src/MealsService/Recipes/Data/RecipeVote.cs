
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Recipes.Data
{
    public class RecipeVote
    {
        public enum VoteType
        {
            UNKNOWN = 0,
            LIKE = 1,
            HATE = 2
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }
        public int RecipeId { get; set; }

        public VoteType Vote { get; set; }
    }
}
