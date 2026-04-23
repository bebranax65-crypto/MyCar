using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarSharingWebsite.Models
{
    public class Administrator
    {
        [Key]
        [ForeignKey("User")]
        public int ID_User { get; set; }

        public bool? manage_cars { get; set; }
        public bool? manage_tariffs { get; set; }
        public bool? _view_statistics { get; set; }
        public bool? control_rentals { get; set; }

        public User? User { get; set; }
    }
}
