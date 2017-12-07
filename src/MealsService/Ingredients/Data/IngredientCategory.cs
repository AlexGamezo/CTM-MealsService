using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Ingredients.Data
{
    public class IngredientCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [StringLength(32)]
        public string Name { get; set; }
        
        public int Order { get; set; }
    }
}
