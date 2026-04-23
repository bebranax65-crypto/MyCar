using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Rental
    {
        [Key]
        public int ID_Rental { get; set; }

        public DateTime? start_time { get; set; }

        public DateTime? end_time { get; set; }

        [Column(TypeName = "money")]
        public decimal? total_cost { get; set; }

        [MaxLength(20)]
        public string? status { get; set; }

        [ForeignKey("User")]
        public int ID_User { get; set; }

        [ForeignKey("Car")]
        public int ID_Car { get; set; }

        [ForeignKey("Tariff")]
        public int ID_Tariff { get; set; }

        public User? User { get; set; }
        public Car? Car { get; set; }
        public Tariff? Tariff { get; set; }
    }
}
