using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using MealsService.Diets.Data;

namespace MealsService.Diets.Dtos
{
    public class DietGoalDto
    {
        public int TargetDietId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ReductionRate ReductionRate { get; set; }

        public int Target { get; set; }
        public int Current { get; set; }
        
        public long Updated { get; set; }
        
    }
}
