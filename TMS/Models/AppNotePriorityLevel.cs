using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMS.Models
{
    public class AppNotePriorityLevel
    {
        public int AppNotePriorityLevelId { get; set; }
        public String PriorityLevelName { get; set; }
        public List<AppNote> AppNotes { get; set; }
    }
}
