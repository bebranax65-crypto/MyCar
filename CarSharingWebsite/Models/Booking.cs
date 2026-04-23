using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Booking
    {
        [Key]
        public int ID_Booking { get; set; }

        public DateTime? start_datetime { get; set; }

        public DateTime? end_datetime { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [ForeignKey("User")]
        public int ID_User { get; set; }

        [ForeignKey("Car")]
        public int ID_Car { get; set; }

        public User? User { get; set; }
        public Car? Car { get; set; }
    }
}