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
    public class PSGadgetsHttp
    {
        public string SchedularGuid { get; set; } = "";
        public int QueCount { get; set; }

        public int ServerCount { get; set; }
        public int VmsProcessableCount { get; set; }
        public bool IsProcessingVM { get; set; }
        public int VmRetryQueCount { get; set; }
        public TimeSpan RunTime { get; set; } = DateTime.Now - (HyperVTools.StartTime ?? DateTime.MinValue);
        public PSGadgetsHttp(string ServerSchedularGUID, int queCount, int serverCount, int vmsProcessable, bool IsProcessing, int vmRetryCount)
        {
            SchedularGuid = ServerSchedularGUID;
            QueCount = queCount;
            ServerCount = serverCount;
            VmsProcessableCount = vmsProcessable;
            IsProcessingVM = IsProcessing;
            VmRetryQueCount = vmRetryCount;
        }

#pragma warning disable
        public PSGadgetsHttp (){ }
#pragma warning enable
    }
}
