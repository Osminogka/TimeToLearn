using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Models;

namespace Users.DAL.Dtos
{
    public class ReadBaseUserDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public Address Address { get; set; }

        public bool IsTeacher { get; set; }

        public long? UniversityId { get; set; }
    }
}
