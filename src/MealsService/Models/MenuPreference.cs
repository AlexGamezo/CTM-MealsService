using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Models
{
    public class MenuPreference
    {
        private string _mealTypesList;
        private List<Meal.Type> _mealTypes;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        //Days per week for shopping (values = [1,7])
        public int ShoppingFreq { get; set; } = 1;

        [JsonConverter(typeof(StringEnumConverter))]
        public MealStyle MealStyle { get; set; }

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        [NotMapped]
        public List<Meal.Type> MealTypes { get
            {
                if (_mealTypes == null && !string.IsNullOrEmpty(_mealTypesList))
                {
                    _mealTypes = _mealTypesList.Split(',').Select(t => (Meal.Type)int.Parse(t)).ToList();
                }
                return _mealTypes;
            }
            set
            {
                _mealTypes = value;
                _mealTypesList = string.Join(",", _mealTypes.Select(t => (int)t));
            }
        }

        [IgnoreDataMember]
        public string MealTypesList
        {
            get { return _mealTypesList; }
            set
            {
                _mealTypesList = value;
                _mealTypes = value.Split(',').Select(t => (Meal.Type) int.Parse(t)).ToList();
            }
        }
    }

    public enum MealStyle
    {
        CheapNQuick,
        Healthy,
        ChefSpecial
    }
}
