using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using MealsService.Recipes.Data;

namespace MealsService.Diets.Data
{
    public class MenuPreference
    {
        private string _mealTypesList;
        private List<MealType> _mealTypes;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        //Days per week for shopping (values = [1,7])
        public int ShoppingFreq { get; set; } = 1;

        public int CurrentDietTypeId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeStyle RecipeStyle { get; set; }

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        [NotMapped]
        public List<MealType> MealTypes {
            get
            {
                if (_mealTypes == null && !string.IsNullOrEmpty(_mealTypesList))
                {
                    _mealTypes = _mealTypesList.Split(',').Select(t => (MealType)int.Parse(t)).ToList();
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
        [StringLength(10)]
        public string MealTypesList
        {
            get { return _mealTypesList; }
            set
            {
                _mealTypesList = value;
                _mealTypes = value.Split(',').Select(t => (MealType) int.Parse(t)).ToList();
            }
        }
        
        [ForeignKey("CurrentDietTypeId")]
        public DietType CurrentDietType { get; set; }

    }

    public enum RecipeStyle
    {
        CheapNQuick,
        Healthy,
        ChefSpecial
    }
}
