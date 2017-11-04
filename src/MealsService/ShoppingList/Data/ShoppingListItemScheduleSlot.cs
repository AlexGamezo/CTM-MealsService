using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealsService.Models;

namespace MealsService.ShoppingList.Data
{
    public class ShoppingListItemScheduleSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ShoppingListItemId { get; set; }
        public int ScheduleSlotId { get; set; }

        /// <summary>
        /// Relations
        /// </summary>
        public ShoppingListItem ShoppingListItem { get; set; }
        public ScheduleSlot ScheduleSlot { get; set; }
    }
}
