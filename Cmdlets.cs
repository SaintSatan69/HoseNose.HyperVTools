using HyperVTools;
using Microsoft.Management.Infrastructure;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
namespace HyperVToolsPowershell
{
    /// <summary>
    /// The cmdlet to communicate with the Process running on either the cluster or independant node
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "VMChange")]
    public class SendVMChange : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public VirtualMachine VirtualMachine
        {
            get { return vm; }
            set { vm = value; }
        }
        [Parameter(Mandatory = true, Position = 1)]
        public string Server
        {
            get { return server; }
            set { server = value; }
        }
        [Parameter(Mandatory = true)]
        [ValidateSet(new string[] { "CPU", "MEMORY" })]
        public string HardwareType { get { return hardwaretobemodified; } set { hardwaretobemodified = value; } }
        [Parameter(Mandatory = true)]
        public int Quantity { get { return _resource_quantity; } set { _resource_quantity = value; } }

        private VirtualMachine vm;
        private string server;
        private string hardwaretobemodified;
        private int _resource_quantity;

        protected override void ProcessRecord()
        {
            if (Quantity > 1024)
            {
                throw new InvalidOperationException("Quantity Must be 1< x <1024");
            }
            //the WSMAN URI so we can go talk to the remote server by spawning powershell
            Uri remotewsman = new Uri($"http://{server}:5985/WSMAN");
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(remotewsman);
            connectionInfo.OperationTimeout = 10 * 60 * 1000;
            connectionInfo.OpenTimeout = 1 * 60 * 1000;
            HardwareQueHTTP payload = new HardwareQueHTTP(vm.Id.ToString().ToUpperInvariant(), vm.FriendlyName, hardwaretobemodified, _resource_quantity.ToString());

            using (Runspace remoterunspace = RunspaceFactory.CreateRunspace(connectionInfo))
            {
                remoterunspace.Open();
                using (PowerShell powershell = PowerShell.Create())
                {
                    powershell.Runspace = remoterunspace;
                    powershell.AddCommand("Invoke-WebRequest")
                    .AddParameter("-Uri", "http://localhost:6969/hypervschedular/")
                    .AddParameter("-Method", "POST")
                    .AddParameter("-Body", $"{System.Text.Json.JsonSerializer.Serialize(payload)}");
                    Collection<PSObject> results = powershell.Invoke();
                }
            }
        }
    }
    /// <summary>
    /// The Cmdlet to get the VMs in a state that the que system wants
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "VirtualMachinesForQue")]
    public class GetVirtualMachinesForQue : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ComputerName
        {
            get { return TargetServer; }
            set { TargetServer = value; }
        }

        private const string CIM_VRTNS = "root/virtualization/v2";
        private const string CIM_VMCLASS = "Msvm_ComputerSystem";
        private const string CIM_VCPUCLASS = "Msvm_ProcessorSettingData";
        private const string CIM_MEMSETTINGS = "Msvm_MemorySettingData";
        private string TargetServer;
        protected override void ProcessRecord()
        {
            CimSession simsesh = CimSession.Create(TargetServer);
            List<VirtualMachine> vms = new List<VirtualMachine>();
            IEnumerable<CimInstance> Cpu_settings = simsesh.EnumerateInstances(CIM_VRTNS, CIM_VCPUCLASS);
            IEnumerable<CimInstance> mem_settings = simsesh.EnumerateInstances(CIM_VRTNS, CIM_MEMSETTINGS);
            foreach (CimInstance vm in simsesh.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS).Where(e => e.CimInstanceProperties["ElementName"].Value.ToString() != TargetServer))
            {
                WriteVerbose($"Processing vm {vm.CimInstanceProperties["ElementName"].Value}");
                int cpu_count = Convert.ToInt32((Cpu_settings.Where(c => c.CimInstanceProperties["InstanceID"].Value.ToString() == $@"Microsoft:{vm.CimInstanceProperties["Name"].Value}\b637f346-6a0e-4dec-af52-bd70cb80a21d\0").First()).CimInstanceProperties["VirtualQuantity"].Value);
                WriteVerbose($"VM has {cpu_count} vCPU");
                int mem_count = (int)(((ulong)(mem_settings.Where(m => m.CimInstanceProperties["InstanceID"].Value.ToString() == $@"Microsoft:{vm.CimInstanceProperties["Name"].Value}\4764334d-e001-4176-82ee-5594ec9b530e").First()).CimInstanceProperties["VirtualQuantity"].Value) / 1024);
                WriteVerbose($"VM has {mem_count}GB of memory");
                vms.Add(new VirtualMachine(Guid.Parse(vm.CimInstanceProperties["Name"].Value.ToString()), vm.CimInstanceProperties["ElementName"].Value.ToString(), cpu_count, mem_count));
            }
            WriteObject(vms);
        }
    }
}