using System.Diagnostics;
using Microsoft.Management.Infrastructure;
namespace HyperVTools
{
    class HyperVTools
    {
#pragma warning disable CS8618
        //If this is attached to a cluster (will be buggy)
        private static string ClusterName;
        private static List<Server> NodeServers;
        private static Dictionary<Server, List<VirtualMachine>> ServerToVM;
        private static Boolean IsCluster = false;
#pragma warning restore CS8618
        //In the event we need to touch WMI/CIM to talk to the hypervisor(s) (very likely)
        private const string CIM_VRTNS = "root/virtualization/v2";
        private const string CIM_VMCLASS = "Msvm_ComputerSystem"; //The HyperVisors Root partition shows up in this class make sure to ignore it
        private const string CIM_VCPUCLASS = "Msvm_Processor";
        private const string CIM_MEMSETTINGS = "Msvm_MemorySettingData";
        private const string CIM_CLSVRTNS = "root/HyperVCluster/v2";
        private const string CIM_CLRNS = "root/mscluster";
        private const string CIM_CLNODECLASS = "MSCluster_Node";
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ExcecptionHandler;
            for (int i = 0; i < args.Length; i++) {

                if (args[i] == "-cluster") { 
                    IsCluster = true;
                    ClusterName = args[i + 1];
                }
            }
            NodeServers = GetServers();
            //With the Current Nodes, we now need to actually get each servers VMS and the current state and periodically poll the node for updates
        }
        /// <summary>
        /// The Function that logs the error without causing the whole program to crash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ExcecptionHandler(object sender, UnhandledExceptionEventArgs e) {
            Debugger.Log(1,"",$"Error {e.ExceptionObject} {Environment.NewLine}");
            Console.WriteLine(e);
        }
        //Retrives all servers it needs to poke for VMS and their states
        public static List<Server> GetServers()
        {
            List<Server> servers = new List<Server>();
            if (!IsCluster)
            {
                //We still will use the list (saves logic in the future) for the local machine
                CimSession session = CimSession.Create("localhost");
                IEnumerable<CimInstance> ClassEnumerator = session.EnumerateInstances(CIM_VRTNS,CIM_VMCLASS);
                int num_vms = 0;
                foreach (CimInstance instance in ClassEnumerator)
                {
                    if (instance.CimInstanceProperties["Caption"].Value.ToString() == "Virtual Machine")
                    {
                        num_vms++;
                    }
                }
                servers.Add(new Server(Environment.GetEnvironmentVariable("COMPUTERNAME").ToUpperInvariant(),num_vms));
            }
            else
            {
                //We Talk to the cluster over CIM to enumerate the Servers and then Get their Number of roles
                CimSession ClusterIdentityCim = CimSession.Create(ClusterName);
                IEnumerable<CimInstance> Cluster_instance = ClusterIdentityCim.EnumerateInstances(CIM_CLRNS,CIM_CLNODECLASS);
                foreach (CimInstance NodeInstance in Cluster_instance)
                {
                    try
                    {
                        string Cur_node = NodeInstance.CimInstanceProperties["Name"].Value.ToString().ToUpperInvariant();
                        CimSession NodeIdentityCim = CimSession.Create(Cur_node);
                        IEnumerable<CimInstance> Node_roles = NodeIdentityCim.EnumerateInstances(CIM_VRTNS,CIM_VMCLASS);
                        int Num_roles = 0;
                        foreach (CimInstance Node_enum_instance in Node_roles)
                        {
                            if (Node_enum_instance.CimInstanceProperties["Caption"].Value.ToString() == "Virtual Machine")
                            {
                                Num_roles++;
                            }
                        }
                        servers.Add(new Server(Cur_node,Num_roles));
                    }
                    catch (Exception e) {
                        Debugger.Log(1,"",$"Node Processing Failed {e.Message}");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            return servers;
        }
        public static List<VirtualMachine> GetVirtualMachines(string Server)
        {
            List<VirtualMachine> VirtualMachines = new List<VirtualMachine>();
            //the same thing we do to get the amount of VMs when initally learning the servers
            CimSession NodeSession = CimSession.Create(Server);
            IEnumerable<CimInstance> CimData_VM = NodeSession.EnumerateInstances(CIM_VRTNS,CIM_VMCLASS);
            IEnumerable<CimInstance> Cimdata_CPU = NodeSession.EnumerateInstances(CIM_VRTNS,CIM_VCPUCLASS);
            IEnumerable<CimInstance> Cimdata_MEM = NodeSession.EnumerateInstances(CIM_VRTNS, CIM_MEMSETTINGS);
            foreach (CimInstance VM_instance in CimData_VM)
            {
                string VM_NAME = VM_instance.CimInstanceProperties["ElementName"].Value.ToString();
                //The logical name is just its GUID
                string VM_LOGICAL_NAME = VM_instance.CimInstanceProperties["Name"].Value.ToString();
                //This is cursed
                int num_CPU = (Cimdata_CPU.Where(e => e.CimInstanceProperties["SystemName"].Value.ToString() == VM_LOGICAL_NAME)).Count();
                //This is even MORE cursed
                int GB_mem = (int)(Cimdata_MEM.Where(m => m.CimInstanceProperties["InstanceID"].Value.ToString() == $"Microsoft:{VM_LOGICAL_NAME}\\4764334d-e001-4176-82ee-5594ec9b530e")).First().CimInstanceProperties["VirtualQuantity"].Value;
                VirtualMachines.Add(new VirtualMachine(Guid.Parse(VM_LOGICAL_NAME),VM_NAME,num_CPU,GB_mem / 1024));
            }
            return VirtualMachines;
        }
    }
}