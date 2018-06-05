namespace MealsService.Ingredients.Data
{
    public enum ConvertType
    {
        UNKNOWN = 0,
        SYSTEM_CONVERT = 1,
        UPSCALE = 2
    }

    public class MeasureConverter
    {
        public int Id { get; set; }
        public int SourceMeasureId { get; set; }
        public int TargetMeasureId { get; set; }
        public double Factor { get; set; }
        public ConvertType ConvertType { get; set; }

        //Relationships
        public MeasureType SourceMeasure { get; set; }
        public MeasureType TargetMeasureType { get; set; }
    }
}
