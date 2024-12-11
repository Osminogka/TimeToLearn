using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forums.DAL.SideModels
{
    public class ResponseArray<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "Invalid request";

        public List<T> Values { get; set; } = new List<T>();
    }
}
