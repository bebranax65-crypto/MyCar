using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Payment
    {
        [Key]
        public int ID_payment { get; set; }

        [Column(TypeName = "money")]
        public decimal? amount { get; set; }

        public DateTime? payment_datetime { get; set; }

        [MaxLength(30)]
        public string? payment_method { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }
    }
}
