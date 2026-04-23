using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarSharingWebsite.Models
{
    public class User
    {
        [Key]
        public int ID_User { get; set; }

        public string? Login { get; set; }

        public int? password { get; set; }

        [MaxLength(60)]
        public string? first_name { get; set; }

        [MaxLength(60)]
        public string? last_name { get; set; }

        [MaxLength(60)]
        public string? middle_name { get; set; }

        [MaxLength(20)]
        public string? phone { get; set; }

        [MaxLength(20)]
        public string? Role { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        public DateTime? registration_date { get; set; }

        public bool IsVerified { get; set; } = false;

        // Navigation properties
        public Client? Client { get; set; }
        public Administrator? Administrator { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    }
}