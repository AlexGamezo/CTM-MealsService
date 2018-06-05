
using System.Collections.Generic;

namespace MealsService.Stats.Data
{
    public class ImpactStatement
    {
        public enum Type
        {
            UNKNOWN = 0,
            AIR = 1,
            WATER = 2,
            ANIMAL = 3,
            HEALTH = 4
        }

        public int Id { get; set; }

        public Type ImpactType { get; set; }

        public string RefUrl { get; set; }

        public string Alt { get; set; }

        public string Text { get; set; }

        public string ParametersRaw { get; set; }

        //public List<TextParameter> Parameters { get; set; }
    }
}
