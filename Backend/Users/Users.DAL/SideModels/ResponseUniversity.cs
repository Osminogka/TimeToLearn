using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Dtos;

namespace Users.DAL.SideModels
{
    public class ResponseUniversity
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid Request";

        public ReadUniversityDto UniversityDto { get; set; } = new ReadUniversityDto();
    }
}
