using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealsService.Ingredients.Data;

namespace MealsService.ShoppingList.Data
{
    public class ShoppingListItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int IngredientId { get; set; }
        public int MeasureTypeId { get; set; }

        [StringLength(64)]
        //For manually added ingredients to not pollute ingredients
        public string IngredientName { get; set; }

        public float Amount { get; set; }
        public bool ManuallyAdded { get; set; }
        public bool Checked { get; set; }
        public bool Unused { get; set; }
        public DateTime WeekStart { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public Ingredient Ingredient { get; set; }
        public MeasureType MeasureType { get; set; } 
        public List<ShoppingListItemPreparation> ShoppingListItemPreparations { get; set; }
    }
}
