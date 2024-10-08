﻿using System.ComponentModel.DataAnnotations;
using Users.DAL.Models;

namespace Users.DAL.Dtos
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

        [Required]
        public Address Address { get; set; }

        [Required]
        public bool IsOpened { get; set; }
    }
}
