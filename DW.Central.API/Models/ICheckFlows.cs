using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Models
{
    public class ICheckFlows
    {
        public int successCount { get; set; }
        public int failureCount { get; set; }
        public int runningCount { get; set; }
        public int skippedCount { get; set; }
    }
}
