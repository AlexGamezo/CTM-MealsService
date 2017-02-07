using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Models
{
    public class MenuPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        //Days per week for shopping (values = [1,7])
        public int ShoppingFreq { get; set; } = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public MealStyle MealStyle { get; set; }
    }

    public enum MealStyle
    {
        CheapNQuick,
        Healthy,
        ChefSpecial
    }
}
