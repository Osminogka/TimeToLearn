using System.ComponentModel.DataAnnotations;
using Core.DAL.Models;

namespace Core.DAL.Dtos
{
    public class ReadUniversityDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public long DirectorId { get; set; }

        public string DirectorUsername { get; set; } = string.Empty;

        public Address? Address { get; set; }

        [Required]
        public bool IsOpened { get; set; }
    }
}
