using System.ComponentModel.DataAnnotations;
using Core.DAL.Models;

namespace Core.DAL.Dtos
{
    public class CreateUniversityDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        public Address? Address { get; set; }

        [Required]
        public bool IsOpened { get; set; }
    }
}
