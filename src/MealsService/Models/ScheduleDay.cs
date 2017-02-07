
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Models
{
    /// <summary>
    /// Container for full-day meal schedule for a specific user.
    /// Identifies the Diet Type the day is supposed to be.
    /// * ManyToMany Ingredients
    /// * HasMany MealKeywords
    /// * ManyToMany DietType
    /// </summary>
    public class ScheduleDay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public int DietTypeId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public List<ScheduleSlot> ScheduleSlots { get; set; }
        public DietType DietType { get; set; }
    }
}
