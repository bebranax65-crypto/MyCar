using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Client
    {
        [Key]
        [ForeignKey("User")]
        public int ID_User { get; set; }

        [MaxLength(20)]
        public string? license_number { get; set; }

        [Column(TypeName = "money")]
        public decimal? Balance { get; set; }

        public User? User { get; set; }
    }
}
