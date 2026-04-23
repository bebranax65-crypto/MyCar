using System.ComponentModel.DataAnnotations;

namespace CarSharingWebsite.Models
{
    public class Location
    {
        [Key]
        public int ID_Map { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Map_point { get; set; }
    }
}
