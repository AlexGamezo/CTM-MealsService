using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Models
{
    public class DietGoal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [IgnoreDataMember]
        public int UserId { get; set; }

        [IgnoreDataMember]
        public int TargetDietId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ReductionRate ReductionRate { get; set; }
        public int Target { get; set; }
        public int Current { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Updated { get; set; }

        //Navigation properties
        [IgnoreDataMember]
        [ForeignKey("TargetDietId")]
        public DietType TargetDietType { get; set; }

        public string TargetDiet => TargetDietType != null ? TargetDietType.Name : "";
    }

    public enum ReductionRate
    {
        Weekly,
        Biweekly,
        Monthly
    }
}
