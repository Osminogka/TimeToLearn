using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.DAL.SideModels
{
    public class ResponseGetEnum<T>
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid Request";

        public IEnumerable<T> Enum { get; set; } = new List<T>();
    }
}
