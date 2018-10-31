
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace MealsService.Stats.Data
{
    public enum ImpactType
    {
        UNKNOWN = 0,
        AIR = 1,
        WATER = 2,
        ANIMAL = 3,
        HEALTH = 4,
        FOOD_AVAILABILITY = 5,
        ENVIRONMENT = 6
    }

    public enum CalculationType
    {
        UNKNOWN = 0,
        STATIC = 1,
        PER_MEAL = 2,
        PER_WEEK_DAYS = 3
    }

    public class TextParameter
    {
        public string Key { get; set; }
        public float Multiplier { get; set; }
        public string Format { get; set; }
        public float Threshold { get; set; }
    }

    public class ImpactStatement
    {
        private List<TextParameter> _parameters;

        public int Id { get; set; }

        public ImpactType ImpactType { get; set; }

        public CalculationType CalcType { get; set; }

        public string RefUrl { get; set; }

        public string Alt { get; set; }

        public string Text { get; set; }

        public string ParametersRaw { get; set; }

        [NotMapped]
        public List<TextParameter> Parameters {
            get
            {
                if (_parameters == null)
                {
                    if (ParametersRaw == null)
                    {
                        _parameters = new List<TextParameter>();
                    }
                    else
                    {
                        _parameters = JsonConvert.DeserializeObject<List<TextParameter>>(ParametersRaw);
                    }
                }

                return _parameters;
            }
            set
            {
                _parameters = value;

                if (_parameters == null || !_parameters.Any())
                {
                    Parameters = null;
                }
                else
                {
                    ParametersRaw = JsonConvert.SerializeObject(_parameters);
                }
            }
        }
    }
}
