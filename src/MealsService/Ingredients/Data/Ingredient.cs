using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace MealsService.Ingredients.Data
{
    /// <summary>
    /// Ingredient that can be associated with many Recipes
    /// </summary>
    public class Ingredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(40)]
        public string Name { get; set; }

        [StringLength(80)]
        public string Brief { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [StringLength(80)]
        public string Image { get; set; }
        public string Category => IngredientCategory?.Name ?? "";
        //public bool KitchenStaple { get; set; }
        public int? CategoryId { get; set; }

        public List<string> Tags
        {
            get
            {
                return IngredientTags?
                    .Select(it => it.Tag != null ? it.Tag.Name : "")
                   .Where(t => !string.IsNullOrWhiteSpace(t))
                   .ToList() ?? new List<string>();
            }
        }

        public string MeasurementType { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        [ForeignKey("CategoryId")]
        public IngredientCategory IngredientCategory { get; set; }
        [IgnoreDataMember]
        public List<IngredientTag> IngredientTags { get; set; } = new List<IngredientTag>();
        //[IgnoreDataMember]
        //public List<IngredientMeasureType> IngredientMeasureTypes { get; set; } = new List<IngredientMeasureType>();
    }
}
