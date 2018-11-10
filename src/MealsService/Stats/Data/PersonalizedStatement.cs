
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Stats.Data
{
    public class PersonalizedStatement
    {
        public int Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ImpactType ImpactType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CalculationType CalcType { get; set; }

        public string RefUrl { get; set; }

        public string Alt { get; set; }

        public string Text { get; set; }
    }
}
