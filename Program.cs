using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Microsoft.Management.Infrastructure;
namespace HyperVTools
{
    class HyperVTools
    {
#pragma warning disable CS8618
        //If this is attached to a cluster (will be buggy)
        private static string ClusterName;
        private static List<Server> NodeServers;
        private static Dictionary<Server, List<VirtualMachine>> ServerToVM = new();
        private static Boolean IsCluster = false;
#pragma warning restore CS8618
        //In the event we need to touch WMI/CIM to talk to the hypervisor(s) (very likely)
        private const string CIM_VRTNS = "root/virtualization/v2";
        private const string CIM_VMCLASS = "Msvm_ComputerSystem"; //The HyperVisors Root partition shows up in this class make sure to ignore it
        private const string CIM_VCPUCLASS = "Msvm_ProcessorSettingData";
        private const string CIM_MEMSETTINGS = "Msvm_MemorySettingData";
        private const string CIM_CLSVRTNS = "root/HyperVCluster/v2";
        private const string CIM_CLRNS = "root/mscluster";
        private const string CIM_CLNODECLASS = "MSCluster_Node";

        private const string FORMAT_SPACE = "                                ";
        private static int POLL_TIME = 100;
        private static bool IsVerbose = true;
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ExcecptionHandler;
            for (int i = 0; i < args.Length; i++) {

                if (args[i] == "-cluster") { 
                    IsCluster = true;
                    ClusterName = args[i + 1];
                }
                if (args[i] == "-polltime")
                {
                    try { string _ = args[i + 1]; } catch{ Console.WriteLine("Invalid Or Missing Poll Time");Environment.Exit(-1); }
                    POLL_TIME = Convert.ToInt32(args[i + 1]);
                }
                if (args[i] == "-verbose")
                {
                    IsVerbose = true;
                }
            }
            NodeServers = GetServers();
            //With the Current Nodes, we now need to actually get each servers VMS and the current state and periodically poll the node for updates
            foreach (Server server in NodeServers)
            {
                ServerToVM.Add(server, GetVirtualMachines(server.ServerName));
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Intial VM Data Gathered");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Server{FORMAT_SPACE}VMName{FORMAT_SPACE}VMGuid");
            Console.WriteLine($"------{FORMAT_SPACE}------{FORMAT_SPACE}------");
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (Server server in ServerToVM.Keys)
            {
                Console.WriteLine($"{server.ServerName}");
                foreach (VirtualMachine vm in ServerToVM[server])
                {
                    Console.WriteLine($"      {FORMAT_SPACE}{vm.FriendlyName}{FORMAT_SPACE.Substring(0,FORMAT_SPACE.Length - vm.FriendlyName.Length)}{"      "}{vm.Id}");
                }
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.White;
            //Now the fun begins
            BigInteger _internal_counter = 0;
            List<VirtualMachine> Processable_VMs = new();
            while (true)
            {
                foreach (KeyValuePair<Server,List<VirtualMachine>> entry in ServerToVM) {
                    Processable_VMs = PollVirtualMachineStatus(entry);
                    Console.WriteLine();
                    foreach (VirtualMachine _vm in Processable_VMs)
                    {
                        Console.WriteLine($"VM: {_vm.FriendlyName} is in a state for processing");
                    }
                    Console.WriteLine();
                }
                
                _internal_counter++;
                Thread.Sleep(POLL_TIME);
            }
        }
        /// <summary>
        /// The Function that logs the error without causing the whole program to crash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ExcecptionHandler(object sender, UnhandledExceptionEventArgs e) {
            Debugger.Log(1,"",$"Error {e.ExceptionObject} {Environment.NewLine}");
            Console.WriteLine(e.ExceptionObject);
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
                session.Close();
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
                ClusterIdentityCim.Close();
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
                if (VM_NAME == Server)
                {
                    continue;
                }
                //The logical name is just its GUID
                string VM_LOGICAL_NAME = VM_instance.CimInstanceProperties["Name"].Value.ToString();
                //This is cursed
                ulong num_CPU = (ulong)(Cimdata_CPU.Where(e => e.CimInstanceProperties["InstanceID"].Value.ToString() == $@"Microsoft:{VM_LOGICAL_NAME}\b637f346-6a0e-4dec-af52-bd70cb80a21d\0")).First().CimInstanceProperties["VirtualQuantity"].Value;
                //This is even MORE cursed
                ulong GB_mem = (ulong)(Cimdata_MEM.Where(m => m.CimInstanceProperties["InstanceID"].Value.ToString() == @$"Microsoft:{VM_LOGICAL_NAME}\4764334d-e001-4176-82ee-5594ec9b530e")).First().CimInstanceProperties["VirtualQuantity"].Value;
                VirtualMachines.Add(new VirtualMachine(Guid.Parse(VM_LOGICAL_NAME),VM_NAME,(int)num_CPU,(int)(GB_mem / 1024)));
            }
            NodeSession.Close();
            return VirtualMachines;
        }
        /// <summary>
        /// Polls the given server for all its VMS again, but instead of returning all the them returns a list of the VM that may be actionable against. Will Write to the console if a VM enters a weird state or a backup state in which it will refuse to touch it.
        /// </summary>
        /// <param name="server"></param>
        public static List<VirtualMachine> PollVirtualMachineStatus(KeyValuePair<Server, List<VirtualMachine>> ServerToVMEntry)
        {
            List<VirtualMachine> Processable_VirtualMachines = new();
            CimSession ServerCim = CimSession.Create(ServerToVMEntry.Key.ServerName);
            IEnumerable<CimInstance> cimdata = ServerCim.EnumerateInstances(CIM_VRTNS,CIM_VMCLASS);
            foreach (CimInstance instance in cimdata)
            {
                if (instance.CimInstanceProperties["ElementName"].Value.ToString() == ServerToVMEntry.Key.ServerName)
                {
                    continue;
                }
                ushort[] opstat = (ushort[])instance.CimInstanceProperties["OperationalStatus"].Value;
                if (opstat.Length > 1)
                {
                    Console.WriteLine($"VM: {instance.CimInstanceProperties["ElementName"].Value} has a sub opcode of {opstat[1]}");
                    //this snippet will enumerate the VMs in the list to find the ones polled and mark the ones doing things like backing up as ignored to prevent this program from making their state worse
                    foreach (VirtualMachine vm in ServerToVMEntry.Value.Where(v => v.Id.ToString() == instance.CimInstanceProperties["Name"].Value.ToString()))
                    {
                        vm.IsIgnored = true;
                    }
                }
                else if (opstat[0] != 2)
                {
                    Console.WriteLine($"VM: {instance.CimInstanceProperties["ElementName"].Value} is unhealthy with an opcode of {opstat[0]}");
                    foreach (VirtualMachine vm in ServerToVMEntry.Value.Where(v => v.Id.ToString() == instance.CimInstanceProperties["Name"].Value.ToString()))
                    {
                        vm.IsIgnored = true;
                    }
                }
                else
                {
                    Console.WriteLine($"VM: {instance.CimInstanceProperties["ElementName"].Value} is healthy");
                    foreach (VirtualMachine vm in ServerToVMEntry.Value.Where(v => v.Id.ToString().ToUpperInvariant() == instance.CimInstanceProperties["Name"].Value.ToString()))
                    {
                        vm.IsIgnored = false;
                    }
                }
            }
            var filtered_cim_disabled_VMs = cimdata.Where(cd => (cd.CimInstanceProperties["EnabledState"].Value.ToString() == "3" || cd.CimInstanceProperties["EnabledState"].Value.ToString() == "4"));
            //What in the world was i thinking, i just need a list of all VMS that have gone into the shutdown state or powered off state, while also not being a VM thats in an ignored state
            Processable_VirtualMachines = ServerToVMEntry.Value.Where(vm => ((!vm.IsIgnored) && (filtered_cim_disabled_VMs.Where(cid => cid.CimInstanceProperties["Name"].Value.ToString() == vm.Id.ToString().ToUpperInvariant()).Any()))).ToList();
            ServerCim.Close();
            return Processable_VirtualMachines;
        }
        /// <summary>
        /// Using the provided server that it knows the VMS might fully lives on  attempts to update the hardware if it has fully entered the stopped state.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="Process_VMs"></param>
        public static void ProcessVMs(string server, List<VirtualMachine> Process_VMs)
        {

        }
        /// <summary>
        /// Probably Never going to be used but to not forget the way to invoke a CIM method against an object, if it is used bewarned its the same as unplugging the computer/turning off the VM not cleanly (Its really just KillVM)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="VM"></param>
        /// <returns></returns>
        public static int ChangeVMState(string server,VirtualMachine VM,VirtualMachine.VirtualMachineStates DesiredVMState) { 
            CimSession session = CimSession.Create(server);
            IEnumerable<CimInstance> cimInstances = session.EnumerateInstances(CIM_VRTNS,CIM_VMCLASS);
            CimInstance VMInstance;
            try
            {
                VMInstance = cimInstances.Where(e => e.CimInstanceProperties["Name"].Value.ToString().ToUpperInvariant() == VM.Id.ToString().ToUpperInvariant()).First();
            }
            catch (Exception ex)
            {
                Debugger.Log(1,"",$"Failed to retrive VM {VM.FriendlyName} from Server {server} exception is {ex.Message} did the VM Move to a different server?");
                return -1;
            }
            CimMethodParametersCollection method_params = new CimMethodParametersCollection();
            CimMethodParameter param = CimMethodParameter.Create("RequestedState",(int)DesiredVMState,CimType.UInt16,CimFlags.None);
            method_params.Add(param);
            var result = session.InvokeMethod(VMInstance,"RequestStateChange",method_params);
            return (int)result.ReturnValue.Value;
        }
    }
}