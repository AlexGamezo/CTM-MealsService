using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MealsService.Schedules.Data;

namespace MealsService.ShoppingList.Data
{
    [Table("ShoppingListItemPreparation")]
    public class ShoppingListItemPreparation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ShoppingListItemId { get; set; }
        public int PreparationId { get; set; }

        /// <summary>
        /// Relations
        /// </summary>
        public ShoppingListItem ShoppingListItem { get; set; }
        public Preparation Preparation { get; set; }
    }
}
