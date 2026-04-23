using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Tariff
    {
        [Key]
        public int ID_Tariff { get; set; }

        [MaxLength(50)]
        public string? Name { get; set; }

        [Column(TypeName = "money")]
        public decimal? price_per_minute { get; set; }

        [Column(TypeName = "money")]
        public decimal? price_per_hour { get; set; }

        public int? min_duration { get; set; }

        [ForeignKey("Car")]
        public int ID_Car { get; set; }

        public Car? Car { get; set; }
    }
}
