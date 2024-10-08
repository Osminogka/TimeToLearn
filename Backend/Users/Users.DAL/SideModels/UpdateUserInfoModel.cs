﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Models;

namespace Users.DAL.SideModels
{
    public class UpdateUserInfoModel
    {
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public Address? Address { get; set; }
    }
}
