
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Ingredients.Data
{
    public class MeasureType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }        //Ounces
        public string Short { get; set; }       //oz
    }
}
