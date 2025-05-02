using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVTools
{
    public class HardwareQueHTTP
    {
        public string Guid {  get; set; }
        public string Name { get; set; }
        public string Hardware {  get; set; }
        public string Quantity { get; set; }
        public HardwareQueHTTP(string guid,string name, string hardware, string quantity)
        {
            Guid = guid;
            Name = name;
            Hardware = hardware;
            Quantity = quantity;
        }
#pragma warning disable
        //Paramles ctor for json serial
        public HardwareQueHTTP() { }
#pragma warning enable
    }
}
