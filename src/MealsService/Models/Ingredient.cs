
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace MealsService.Models
{
    /// <summary>
    /// Ingredient that can be associated with many Meals
    /// </summary>
    public class Ingredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brief { get; set; }
        public string Description { get; set; }
        public string Category => IngredientCategory?.Name ?? "";
        
        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        [ForeignKey("CategoryId")]
        public IngredientCategory IngredientCategory { get; set; }
    }
}
