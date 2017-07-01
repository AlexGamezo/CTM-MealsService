using MealsService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Responses.Diets
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
