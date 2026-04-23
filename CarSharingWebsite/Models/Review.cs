using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Review
    {
        [Key]
        public int ID_review { get; set; }

        [MaxLength(200)]
        public string? comment { get; set; }

        public int? rating { get; set; }

        public DateTime? review_date { get; set; }

        [ForeignKey("User")]
        public int ID_User { get; set; }

        public User? User { get; set; }
    }
}
