using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class Address
    {
        [Required]
        [MaxLength(50)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Street { get; set; } = string.Empty;
    }

}
