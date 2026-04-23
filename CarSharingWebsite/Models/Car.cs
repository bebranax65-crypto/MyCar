using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Car
    {
        [Key]
        public int ID_Car { get; set; }

        [MaxLength(10)]
        public string? plate_number { get; set; }

        [MaxLength(20)]
        public string? LicensePlate { get; set; }

        [MaxLength(40)]
        public string? Brand { get; set; }

        [MaxLength(30)]
        public string? Model { get; set; }

        public int? Year { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? condition { get; set; }

        [MaxLength(30)]
        public string? status { get; set; }

        [MaxLength(255)]
        public string? PhotoUrl { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [ForeignKey("User")]
        public int ID_User { get; set; }

        public User? User { get; set; }

        // Navigation properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();
    }
}