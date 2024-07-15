using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Dtos
{
    public class BaseUserPublishDto
    {
        public Guid OriginalId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Event { get; set; }
    }
}
