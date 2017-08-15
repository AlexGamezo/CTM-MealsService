
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealsService.Tags.Data;

namespace MealsService.Ingredients.Data
{
    public class IngredientTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TagId { get; set; }
        public int IngredientId { get; set; }

        public Tag Tag { get; set; }
        public Ingredient Ingredient { get; set; }
    }
}
