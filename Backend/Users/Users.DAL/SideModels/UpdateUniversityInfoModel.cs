using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Models;

namespace Users.DAL.SideModels
{
    public class UpdateUniversityInfoModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Address? Address { get; set; }
    }
}
