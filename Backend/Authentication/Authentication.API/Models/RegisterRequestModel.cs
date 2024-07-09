using System.ComponentModel.DataAnnotations;

namespace Authentication.API.Models
{
    public class RegisterRequestModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
