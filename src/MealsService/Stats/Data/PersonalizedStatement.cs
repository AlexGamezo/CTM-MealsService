
namespace MealsService.Stats.Data
{
    public class PersonalizedStatement
    {
        public int Id { get; set; }

        public ImpactType ImpactType { get; set; }

        public CalculationType CalcType { get; set; }

        public string RefUrl { get; set; }

        public string Alt { get; set; }

        public string Text { get; set; }
    }
}
