
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Diets.Data
{
    /// <summary>
    /// Diet Types (Meaty, Fishy, Veggie, Vegan, etc)
    /// </summary>
    public class DietType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(30)]
        public string Name { get; set; }

        [StringLength(30)]
        public string ShortDescription { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }
}
