
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace MealsService.Ingredients.Data
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
        public string Image { get; set; }
        public string Category => IngredientCategory?.Name ?? "";

        public List<string> Tags
        {
            get
            {
                return IngredientTags == null
                    ? new List<string>()
                    : IngredientTags.Select(it => it.Tag != null ? it.Tag.Name : "")
                        .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }
        }

        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        [ForeignKey("CategoryId")]
        public IngredientCategory IngredientCategory { get; set; }
        [IgnoreDataMember]
        public List<IngredientTag> IngredientTags { get; set; }
    }
}
