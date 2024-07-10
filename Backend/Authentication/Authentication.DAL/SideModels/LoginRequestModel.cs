using System.ComponentModel.DataAnnotations;

namespace Authentication.DAL.Models
{
    public class LoginRequestModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
