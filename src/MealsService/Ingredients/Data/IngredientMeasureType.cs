
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Ingredients.Data
{
    public class IngredientMeasureType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public int MeasureTypeId { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public Ingredient Ingredient { get; set; }
        public MeasureType MeasureType { get; set; }
    }
}
