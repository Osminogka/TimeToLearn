using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forums.DAL.SideModels
{
    public class CommentInfo
    {
        public long PostId { get; set; }

        public bool IsTopic { get; set; }

        public string UniversityName { get; set; } = string.Empty;

        public long OriginalId { get; set; }
    }
}
