using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Models
{
    public class IDataverseEnvironment
    {
        public required string EnvironmentId { get; set; }
        public required string FlowId { get; set; }

    }
}
