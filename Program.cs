using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Text;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Serialization;
namespace HyperVTools
{
    class HyperVTools
    {
#pragma warning disable CS8618
        //If this is attached to a cluster (will be buggy)
        private static string ClusterName;
        private static List<Server> NodeServers;
        private static Dictionary<Server, List<VirtualMachine>> ServerToVM = new();
        private static List<HardwareQueHTTP> QuedRequests = new();
        private static Boolean IsCluster = false;
#pragma warning restore CS8618
        //In the event we need to touch WMI/CIM to talk to the hypervisor(s) (it is ONLY CIM) (OH MY GOD THERES WAY TO MUCH CIM)
        private const string CIM_VRTNS = "root/virtualization/v2";
        private const string CIM_VMCLASS = "Msvm_ComputerSystem"; //The HyperVisors Root partition shows up in this class make sure to ignore it
        private const string CIM_VCPUCLASS = "Msvm_ProcessorSettingData";
        private const string CIM_MEMSETTINGS = "Msvm_MemorySettingData";
        private const string CIM_CLSVRTNS = "root/HyperVCluster/v2";
        private const string CIM_CLRNS = "root/mscluster";
        private const string CIM_CLNODECLASS = "MSCluster_Node";
        private const string CIM_VSMSCLASS = "CIM_VirtualSystemManagementService";
        private const string CIM_VSMSSETTINGCLASS = "CIM_ResourceAllocationSettingData";


        //A Spare Copy of the XML just incase enbedded in the file just in case
        private static readonly string MSVM_PROCSETTINGXMLSTRUCTURE = "<INSTANCE CLASSNAME=\"Msvm_ProcessorSettingData\">" +
                                                                      "<PROPERTY NAME=\"Caption\" TYPE=\"string\"><VALUE>Processor</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Description\" TYPE=\"string\"><VALUE>Settings for Microsoft Virtual Processor.</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ElementName\" TYPE=\"string\"><VALUE>Processor</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"InstanceID\" TYPE=\"string\"><VALUE>Microsoft:7F236D92-053F-4EB6-82E1-A76F1E443CE9\\b637f346-6a0e-4dec-af52-bd70cb80a21d\\0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Address\" TYPE=\"string\"></PROPERTY><PROPERTY NAME=\"AddressOnParent\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AllocationUnits\" TYPE=\"string\"><VALUE>percent / 1000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AutomaticAllocation\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AutomaticDeallocation\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY.ARRAY NAME=\"Connection\" TYPE=\"string\"></PROPERTY.ARRAY>" +
                                                                      "<PROPERTY NAME=\"ConsumerVisibility\" TYPE=\"uint16\"><VALUE>3</VALUE></PROPERTY>" +
                                                                      "<PROPERTY.ARRAY NAME=\"HostResource\" TYPE=\"string\"></PROPERTY.ARRAY>" +
                                                                      "<PROPERTY NAME=\"Limit\" TYPE=\"uint64\"><VALUE>100000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MappingBehavior\" TYPE=\"uint16\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"OtherResourceType\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Parent\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"PoolID\" TYPE=\"string\"><VALUE></VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Reservation\" TYPE=\"uint64\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ResourceSubType\" TYPE=\"string\"><VALUE>Microsoft:Hyper-V:Processor</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ResourceType\" TYPE=\"uint16\"><VALUE>3</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"VirtualQuantity\" TYPE=\"uint64\" MODIFIED=\"TRUE\"><VALUE>12</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"VirtualQuantityUnits\" TYPE=\"string\"><VALUE>count</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Weight\" TYPE=\"uint32\"><VALUE>100</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AllowACountMCount\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ApicMode\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"CpuBrandString\" TYPE=\"string\"><VALUE></VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"CpuGroupId\" TYPE=\"string\"><VALUE>00000000-0000-0000-0000-000000000000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"DisableSpeculationControls\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableHostResourceProtection\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableLegacyApicMode\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePageShattering\" TYPE=\"uint8\"><VALUE>2</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonArchPmu\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonIpt\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonLbr\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonPebs\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonPmu\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableSocketTopology\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnlightenmentSet\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ExposeVirtualizationExtensions\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ExtendedVirtualizationExtensions\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"HideHypervisorPresent\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"HwThreadsPerCore\" TYPE=\"uint64\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"L3CacheWays\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"L3ProcessorDistributionPolicy\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitCPUID\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitProcessorFeatures\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitProcessorFeaturesMode\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxClusterCountPerSocket\" TYPE=\"uint32\"><VALUE>4294967295</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxHwIsolatedGuests\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxNumaNodesPerSocket\" TYPE=\"uint64\"><VALUE>1</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxProcessorCountPerL3\" TYPE=\"uint32\"><VALUE>4294967295</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxProcessorsPerNumaNode\" TYPE=\"uint64\"><VALUE>20</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"PerfCpuFreqCapMhz\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ProcessorFeatureSet\" TYPE=\"string\"></PROPERTY></INSTANCE>";



        private const string FORMAT_SPACE = "                                ";
        private static int POLL_TIME = 100;
        private static bool IsVerbose = true;
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ExcecptionHandler;
            for (int i = 0; i < args.Length; i++)
            {

                if (args[i] == "-cluster")
                {
                    IsCluster = true;
                    ClusterName = args[i + 1];
                }
                if (args[i] == "-polltime")
                {
                    try { string _ = args[i + 1]; } catch { Console.WriteLine("Invalid Or Missing Poll Time"); Environment.Exit(-1); }
                    POLL_TIME = Convert.ToInt32(args[i + 1]);
                }
                if (args[i] == "-verbose")
                {
                    IsVerbose = true;
                }
            }

            Thread HTTP = new(() =>
            {
                HttpListener Api = new HttpListener();
                try
                {
                    //We use local host as the client will initiate a remote powershell session over WSMAN/WINRM and invoke a webrequest to localhost and hopefully to this program, WSMAN will handle the auth from the calling place.
                    Api.Prefixes.Add("http://localhost:6969/");
                    Api.Start();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable To Start the HTTP Interface needed for people to schedual hardware changes reason is {ex.Message}");
                }
                while (true)
                {
                    try
                    {
                        HttpListenerContext context = Api.GetContext();
                        if (context.Request.HttpMethod != "POST")
                        {
                            //since is meant just to be able to add hardware chances to the que
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            context.Response.Close();
                        }
                        else
                        {
                            StreamReader request_stream = new StreamReader(context.Request.InputStream);
                            string value = request_stream.ReadToEnd();
                            //byte[] request_streambytes = new byte[request_stream.Length];
                            //request_stream.Read(request_streambytes, 0, (int)request_stream.Length);
                            //HardwareQueHTTP? que_resource = System.Text.Json.JsonSerializer.Deserialize<HardwareQueHTTP>(System.Text.Encoding.UTF8.GetString(request_streambytes));
                            HardwareQueHTTP? que_resource = System.Text.Json.JsonSerializer.Deserialize<HardwareQueHTTP>(value);
                            Debugger.Log(1,"",$"client sent {(que_resource != null)}::{value}{Environment.NewLine}");
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            context.Response.Close();
                            if (que_resource != null)
                            {
                                lock (QuedRequests)
                                {
                                    QuedRequests.Add(que_resource);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debugger.Log(1, "", $"Request has failed for reason{ex.Message}{Environment.NewLine}");
                    }

                }
            });
            HTTP.Start();
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
                //Console.WriteLine($"{server.ServerName}");
                foreach (VirtualMachine vm in ServerToVM[server])
                {
                    Console.WriteLine($"{server.ServerName}{FORMAT_SPACE.Substring(server.ServerName.Length - 6)}{vm.FriendlyName}{FORMAT_SPACE.Substring(0, FORMAT_SPACE.Length - vm.FriendlyName.Length)}{"      "}{vm.Id}");
                }
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.White;
            //Now the fun begins
            ulong _internal_counter = 0;
            ulong _epoch = 0;
            List<VirtualMachine> Processable_VMs = new();
            while (true)
            {
                foreach (KeyValuePair<Server, List<VirtualMachine>> entry in ServerToVM)
                {
                    Processable_VMs.Clear();
                    Processable_VMs = PollVirtualMachineStatus(entry);
                    Console.WriteLine();
                    foreach (VirtualMachine _vm in Processable_VMs)
                    {
                        Console.WriteLine($"VM: {_vm.FriendlyName} is in a state for processing");
                        ChangeVMhardware(entry.Key.ServerName, _vm);
                    }
                    Console.WriteLine();
                    ProcessVMs(entry.Value);
                }
                if (_internal_counter == ulong.MaxValue - 10)
                {
                    _internal_counter = 0;
                    _epoch++;
                }
                else
                {
                    _internal_counter++;
                }
                if (_internal_counter % 30 == 0) {
                    GC.Collect();
                }
                Thread.Sleep(POLL_TIME);
            }
        }
        /// <summary>
        /// The Function that logs the error without causing the whole program to crash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ExcecptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Debugger.Log(1, "", $"Error {e.ExceptionObject} {Environment.NewLine}");
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
                IEnumerable<CimInstance> ClassEnumerator = session.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS);
                int num_vms = 0;
                foreach (CimInstance instance in ClassEnumerator)
                {
                    if (instance.CimInstanceProperties["Caption"].Value.ToString() == "Virtual Machine")
                    {
                        num_vms++;
                    }
                }
                servers.Add(new Server(Environment.GetEnvironmentVariable("COMPUTERNAME").ToUpperInvariant(), num_vms));
                session.Close();
            }
            else
            {
                //We Talk to the cluster over CIM to enumerate the Servers and then Get their Number of roles
                CimSession ClusterIdentityCim = CimSession.Create(ClusterName);
                IEnumerable<CimInstance> Cluster_instance = ClusterIdentityCim.EnumerateInstances(CIM_CLRNS, CIM_CLNODECLASS);
                foreach (CimInstance NodeInstance in Cluster_instance)
                {
                    try
                    {
                        string Cur_node = NodeInstance.CimInstanceProperties["Name"].Value.ToString().ToUpperInvariant();
                        CimSession NodeIdentityCim = CimSession.Create(Cur_node);
                        IEnumerable<CimInstance> Node_roles = NodeIdentityCim.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS);
                        int Num_roles = 0;
                        foreach (CimInstance Node_enum_instance in Node_roles)
                        {
                            if (Node_enum_instance.CimInstanceProperties["Caption"].Value.ToString() == "Virtual Machine")
                            {
                                Num_roles++;
                            }
                        }
                        servers.Add(new Server(Cur_node, Num_roles));
                        NodeIdentityCim.Close();
                    }
                    catch (Exception e)
                    {
                        Debugger.Log(1, "", $"Node Processing Failed {e.Message}");
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
            IEnumerable<CimInstance> CimData_VM = NodeSession.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS);
            IEnumerable<CimInstance> Cimdata_CPU = NodeSession.EnumerateInstances(CIM_VRTNS, CIM_VCPUCLASS);
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
                VirtualMachines.Add(new VirtualMachine(Guid.Parse(VM_LOGICAL_NAME), VM_NAME, (int)num_CPU, (int)(GB_mem / 1024)));
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
            IEnumerable<CimInstance> cimdata = ServerCim.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS);
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
                    foreach (VirtualMachine vm in ServerToVMEntry.Value.Where(v => v.Id.ToString().ToUpperInvariant() == instance.CimInstanceProperties["Name"].Value.ToString()))
                    {
                        vm.IsIgnored = true;
                    }
                }
                else if (opstat[0] != 2)
                {
                    Console.WriteLine($"VM: {instance.CimInstanceProperties["ElementName"].Value} is unhealthy with an opcode of {opstat[0]}");
                    foreach (VirtualMachine vm in ServerToVMEntry.Value.Where(v => v.Id.ToString().ToUpperInvariant() == instance.CimInstanceProperties["Name"].Value.ToString()))
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
        /// Using the provided server that it knows the VMS might fully lives on modifies the instance of the VM for the next run of pollVM and ChangeVMHardware
        /// </summary>
        /// <param name="server"></param>
        /// <param name="Process_VMs"></param>
        public static void ProcessVMs(List<VirtualMachine> Process_VMs)
        {
            bool HasProcessedone = false;
            HardwareQueHTTP ProcessedThingToPopOffTheList = null;
            try
            {
                foreach (HardwareQueHTTP entry in QuedRequests)
                {
                    if (HasProcessedone)
                    {
                        break;
                    }
                    try
                    {
                        VirtualMachine? vm = Process_VMs.Where(v => v.Id.ToString().ToUpperInvariant() == entry.Guid.ToUpperInvariant()).First();
                        if (vm != null && vm.Id != Guid.Empty)
                        {
                            switch (entry.Hardware)
                            {
                                case "CPU":
                                    vm.PendingCoreCount = Convert.ToInt32(entry.Quantity);
                                    HasProcessedone = true;
                                    ProcessedThingToPopOffTheList = entry;
                                    break;
                                case "MEMORY":
                                    throw new NotImplementedException("Memory is not implemented yet");

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debugger.Log(1,"",$"Internal Try of Process VMS caught ({ex.Message}){Environment.NewLine}");
                        continue;
                    }
                }
                if (HasProcessedone && ProcessedThingToPopOffTheList != null)
                {
                    QuedRequests.Remove(ProcessedThingToPopOffTheList);
                }
            }
            catch (SynchronizationLockException ex)
            {
                Debugger.Log(1, "", $"Process VMs encountered the the Que to be locked by the HTTP thread during this run, it will be fine {ex.Message}");
            }
        }
        /// <summary>
        /// Probably Never going to be used but to not forget the way to invoke a CIM method against an object, if it is used bewarned its the same as unplugging the computer/turning off the VM not cleanly (Its really just KillVM)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="VM"></param>
        /// <returns></returns>
        public static int ChangeVMState(string server, VirtualMachine VM, VirtualMachine.VirtualMachineStates DesiredVMState)
        {
            CimSession session = CimSession.Create(server);
            IEnumerable<CimInstance> cimInstances = session.EnumerateInstances(CIM_VRTNS, CIM_VMCLASS);
            CimInstance VMInstance;
            try
            {
                VMInstance = cimInstances.Where(e => e.CimInstanceProperties["Name"].Value.ToString().ToUpperInvariant() == VM.Id.ToString().ToUpperInvariant()).First();
            }
            catch (Exception ex)
            {
                Debugger.Log(1, "", $"Failed to retrive VM {VM.FriendlyName} from Server {server} exception is {ex.Message} did the VM Move to a different server?");
                return -1;
            }
            CimMethodParametersCollection method_params = new CimMethodParametersCollection();
            CimMethodParameter param = CimMethodParameter.Create("RequestedState", (int)DesiredVMState, CimType.UInt16, CimFlags.None);
            method_params.Add(param);
            var result = session.InvokeMethod(VMInstance, "RequestStateChange", method_params);
            return (int)result.ReturnValue.Value;
        }
        /// <summary>
        /// This is an unholy abominiation to the eyes of the reader, pray tell why your here i hope you leave quickly before ending like the one crazy guy meme whos pointing at a whole heck of a lot of paper
        /// </summary>
        /// <param name="server"></param>
        /// <param name="vm"></param>
        private static void ChangeVMhardware(string server, VirtualMachine vm)
        {
            CimSession cimSession = CimSession.Create(server);
            CimInstance vsms = cimSession.EnumerateInstances(CIM_VRTNS, CIM_VSMSCLASS).Where(m => m.CimInstanceProperties["Name"].Value.ToString() == "vmms").First();
            var thing = vsms.CimClass.CimClassMethods["ModifyResourceSettings"].Parameters;
            string CIM_Q = $"SELECT * FROM Msvm_ComputerSystem WHERE ElementName = '{vm.FriendlyName}'";
            CimInstance VMRef;
            try { VMRef = cimSession.QueryInstances(CIM_VRTNS, "WQL", CIM_Q).First(); } catch (Exception ex) { Console.WriteLine($"Failed to retrive VM {ex.Message}"); return; }
            ;
            CimMethodParametersCollection cimMethodParameters = new CimMethodParametersCollection();
            CimMethodParameter? CPU_param = null;
            //I have to null these so i can't use an uninitalized value angering the compiler :(
            CimInstance? ciminstance_CPU_ALLOC = null;
            CimInstance? ciminstance_MEM_ALLOC = null;
            string string_CPU_ALLOC = "";
            string string_MEM_ALLOC = "";
            int _array_len = 0;
            //What in the unholy monster do i have to make to write settings down into the computer
            if (vm.PendingCoreCount > 0)
            {
                //"Msvm_SystemSettingData"
                IEnumerable<CimInstance> _temp = cimSession.EnumerateInstances(CIM_VRTNS, CIM_VSMSSETTINGCLASS).Where(c => c.CimInstanceProperties["ElementName"].Value != null && c.CimInstanceProperties["ElementName"].Value.ToString().ToUpperInvariant() == "PROCESSOR");
                ciminstance_CPU_ALLOC = _temp.Where(c => c.CimInstanceProperties["InstanceID"].Value.ToString() == @$"Microsoft:{vm.Id.ToString().ToUpperInvariant()}\b637f346-6a0e-4dec-af52-bd70cb80a21d\0").First();
                CimInstance vmsettingsInstance = cimSession.EnumerateAssociatedInstances(CIM_VRTNS, VMRef, null, "Msvm_VirtualSystemSettingData", null, null, null).First();
                CimInstance processsettings = cimSession.EnumerateAssociatedInstances(CIM_VRTNS, vmsettingsInstance, null, "CIM_ResourceAllocationSettingData", null, null, null).Where(e => e.CimInstanceProperties["ElementName"].Value.ToString().ToUpperInvariant() == "PROCESSOR").First();
                processsettings.CimInstanceProperties["VirtualQuantity"].Value = vm.PendingCoreCount;
                Console.WriteLine(processsettings.ToString());
                //cimMethodParameters.Add(CimMethodParameter.Create("ResourceSettings", new[] { processsettings.ToString() }, CimType.StringArray, CimFlags.None));
                //string CIM_STRING = $"Msvm_ProcessorSettingData:" +
                //    $"Caption = Processor," +
                //    $"Description = \"Settings for Microsoft Virtual Processor\"" +
                //    $"ElementName = \"Processor\"" +
                //    $"InstanceID = \"{processsettings.CimInstanceProperties["InstanceID"].Value}\"," +
                //    $"Address," +
                //    $"AddressOnParent," +
                //    $"AllocationUnits = \"percent / 1000\"," +
                //    $"AutomaticAllocation = True," +
                //    $"AutomaticDeallocation = True," +
                //    $"Connection," +
                //    $"ConsumerVisibility = 3," +
                //    $"Limit = 100000," +
                //    $"MappingBehavior," +
                //    $"OtherResourceType," +
                //    $"Parent," +
                //    $"PoolID = \"\"," +
                //    $"ResourceSubType = \"Microsoft:Hyper-V:Processor\"," +
                //    $"ResourceType = 3," +
                //    $"VirtualQuantity = {processsettings.CimInstanceProperties["VirtualQuantity"].Value}," +
                //    $"VirtualQuantityUnits = \"count\"," +
                //    $"Weight = 100," +
                //    $"AllowACountMCount = True," +
                //    $"ApicMode = 0," +
                //    $"CpuBrandString = \"\"," +
                //    $"CpuGroupId = \"00000000-0000-0000-0000-000000000000\"," +
                //    $"DisableSpeculationControls = False," +
                //    $"EnableHostResourceProtection = False," +
                //    $"EnableLegacyApicMode = False," +
                //    $"EnablePageShattering = 2," +
                //    $"EnablePerfmonArchPmu = False," +
                //    $"EnablePerfmonIpt = False," +
                //    $"EnablePerfmonLbr = False," +
                //    $"EnablePerfmonPebs = False," +
                //    $"EnablePerfmonPmu = False," +
                //    $"EnableSocketTopology = False," +
                //    $"EnlightenmentSet," +
                //    $"ExposeVirtualizationExtentions = False," +
                //    $"ExtendedVirtualizationExtensions = 0," +
                //    $"HideHypervisorPresent = False," +
                //    $"HwThreadsPerCore = 0," +
                //    $"L3CacheWays = 0," +
                //    $"L3ProcessorDistributionPolicy = 0," +
                //    $"LimitCPUID = False," +
                //    $"LimitProcessorFeatures = False," +
                //    $"LimitProcessorFeaturesMode = 0," +
                //    $"MaxClusterCountPerSocket = 4294967295," +
                //    $"MaxHwIsolatedGuests = 0," +
                //    $"MaxNumaNodesPerSocket = 1," +
                //    $"MaxProcessorCounterPerL3 = 4294967295," +
                //    $"MaxProcessorsPerNumaNode = 20," +
                //    $"PerfCpuFreqCapMhz = 0," +
                //    $"ProcessorFeatureSet";
                //string CIM_STRING = $"CIM_ResourceAllocationSettingData: (" +
                //    $"Caption = \"Processor\"" +
                //    $"Description" +
                //    $"InstanceID = {processsettings.CimInstanceProperties["InstanceID"].Value}" +
                //    $"ElementName = \"Processor\"" +
                //    $"ResourceType = 3," +
                //    $"OtherResourceType," +
                //    $"ResourceSubType = \"Microsoft:Hyper-V:Processor\"," +
                //    $"PoolID = \"\"," +
                //    $"ConsumerVisibility = 3," +
                //    $"HostResource," +
                //    $"AllocationUnits," +
                //    $"VirtualQuantity = {processsettings.CimInstanceProperties["VirtualQuantity"].Value}," +
                //    $"Reservation = 0," +
                //    $"Limit = 100000," +
                //    $"Weight = 100," +
                //    $"AutomaticAllocation = True," +
                //    $"AutomaticDeallocation = True," +
                //    $"Parent," +
                //    $"Connection," +
                //    $"Address," +
                //    $"MappingBehavior," +
                //    $"AddressOnParent," +
                //    $"VirtualQuantityUnits = \"count\"" +
                //    $")";
                //CimInstance out_resourceSetting = new("CIM_ResourceAllocationSettingData");
                //CimInstance out_concretejob = new("Cim_ConcreteJob");
                string CIM_STRING = GenerateHyperVXML(vm, VirtualMachine.HardwareType.CPU);
                cimMethodParameters.Add(CimMethodParameter.Create("ResourceSettings", new[] { CIM_STRING }, CimType.StringArray, CimFlags.In));
                //    cimMethodParameters.Add(CimMethodParameter.Create("ResultingResourceSettings", out_resourceSetting, CimType.ReferenceArray, CimFlags.Out));
                //    cimMethodParameters.Add(CimMethodParameter.Create("Job",out_concretejob,CimType.Reference,CimFlags.Out));
            }
            if (vm.PendingGBMemory > 0)
            {
                ciminstance_MEM_ALLOC = cimSession.EnumerateInstances(CIM_VRTNS, CIM_VSMSSETTINGCLASS).Where(c => (c.CimInstanceProperties["ElementName"].Value != null && c.CimInstanceProperties["ElementName"].Value.ToString().ToUpperInvariant() == "MEMORY" && c.CimInstanceProperties["InstanceID"].Value.ToString() == @$"Microsoft:{vm.Id.ToString().ToUpperInvariant()}\4764334d-e001-4176-82ee-5594ec9b530e")).First(); ;
            }
            if (ciminstance_CPU_ALLOC != null)
            {

                //ciminstance_CPU_ALLOC.CimInstanceProperties.Add(CimProperty.Create("ElementName","Processor",CimType.String,CimFlags.None));
                //ciminstance_CPU_ALLOC.CimInstanceProperties.Add(CimProperty.Create("VirtualQuantity", vm.PendingCoreCount, CimType.UInt64, CimFlags.None));
                //ciminstance_CPU_ALLOC.CimInstanceProperties["VirtualQuantity"].Value = vm.PendingCoreCount;

                //CimSerializer serializer = CimSerializer.Create();
                //cimMethodParameters.Add(CimMethodParameter.Create("ResourceSettings", new[] {System.Text.Encoding.UTF8.GetString(serializer.Serialize(ciminstance_CPU_ALLOC,InstanceSerializationOptions.None)) },CimType.StringArray,CimFlags.None));
                //cimMethodParameters.Add(CimMethodParameter.Create("ResourceSettings", new[] { System.Text.Json.JsonSerializer.Serialize(ciminstance_CPU_ALLOC.CimInstanceProperties) }, CimType.StringArray, CimFlags.None));
                //string Json_CPU_serial = System.Text.Json.JsonSerializer.Serialize(ciminstance_CPU_ALLOC);
                //string[] json_string = new string[1];
                //json_string[0] = Json_CPU_serial;
                ////CPU_param = CimMethodParameter.Create("ResourceSettings[]",ciminstance_CPU_ALLOC.CimInstanceProperties.AsEnumerable().ToList(),CimType.StringArray,CimFlags.Parameter);
                //CPU_param = CimMethodParameter.Create("ResourceSettings[]",json_string, CimType.StringArray, CimFlags.Parameter);
                //cimMethodParameters.Add(CPU_param);
                _array_len++;
            }
            if (ciminstance_MEM_ALLOC != null)
            {
                ciminstance_MEM_ALLOC.CimInstanceProperties["VirtualQuantity"].Value = vm.PendingGBMemory * 1024;
                _array_len++;
            }
            if (cimMethodParameters.Count > 0)
            {
                var result = cimSession.InvokeMethod(vsms, "ModifyResourceSettings", cimMethodParameters);
                if ((uint)result.ReturnValue.Value == 0)
                {
                    if (vm.PendingCoreCount != 0)
                    {
                        vm.UpdateVirtualMachine("CPU", vm.PendingCoreCount);
                    }
                    if (vm.PendingGBMemory != 0)
                    {
                        vm.UpdateVirtualMachine("MEMORY", vm.PendingGBMemory);
                    }
                    vm.PendingCoreCount = 0;
                    vm.PendingGBMemory = 0;
                }
                if (IsVerbose)
                {
                    Console.WriteLine(result.ReturnValue.Value);
                }
            }
            cimSession.Close();
        }
        private static string SerializeCimInstance(CimInstance instance, VirtualMachine.HardwareType SettingType)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (SettingType == VirtualMachine.HardwareType.CPU)
            {
                stringBuilder.AppendLine("<INSTANCE CLASSNAME=\"Msvm_ProcessorSettingData\">");
            }
            else if (SettingType == VirtualMachine.HardwareType.MEMORY)
            {
                stringBuilder.AppendLine("<INSTANCE CLASSNAME=\"Msvm_MemorySettingData\"");
            }
            foreach (var property in instance.CimInstanceProperties)
            {
                if (property.Value != null)
                {
                    stringBuilder.AppendLine($"  <PROPERTY NAME=\"{property.Name}\" TYPE=\"string\"");
                    stringBuilder.AppendLine($"    <VALUE>{property.Value}</VALUE>");
                    stringBuilder.AppendLine("  </PROPERTY");
                }
            }
            stringBuilder.AppendLine("</INSTANCE>");
            return stringBuilder.ToString();
        }
        private static string GenerateHyperVXML(VirtualMachine vm, VirtualMachine.HardwareType settingtype)
        {
            if (settingtype == VirtualMachine.HardwareType.CPU)
            {
                return "<INSTANCE CLASSNAME=\"Msvm_ProcessorSettingData\">" +
                                                                      "<PROPERTY NAME=\"Caption\" TYPE=\"string\"><VALUE>Processor</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Description\" TYPE=\"string\"><VALUE>Settings for Microsoft Virtual Processor.</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ElementName\" TYPE=\"string\"><VALUE>Processor</VALUE></PROPERTY>" +
                                                                      $"<PROPERTY NAME=\"InstanceID\" TYPE=\"string\"><VALUE>Microsoft:{vm.Id.ToString().ToUpperInvariant()}\\b637f346-6a0e-4dec-af52-bd70cb80a21d\\0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Address\" TYPE=\"string\"></PROPERTY><PROPERTY NAME=\"AddressOnParent\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AllocationUnits\" TYPE=\"string\"><VALUE>percent / 1000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AutomaticAllocation\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AutomaticDeallocation\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY.ARRAY NAME=\"Connection\" TYPE=\"string\"></PROPERTY.ARRAY>" +
                                                                      "<PROPERTY NAME=\"ConsumerVisibility\" TYPE=\"uint16\"><VALUE>3</VALUE></PROPERTY>" +
                                                                      "<PROPERTY.ARRAY NAME=\"HostResource\" TYPE=\"string\"></PROPERTY.ARRAY>" +
                                                                      "<PROPERTY NAME=\"Limit\" TYPE=\"uint64\"><VALUE>100000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MappingBehavior\" TYPE=\"uint16\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"OtherResourceType\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Parent\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"PoolID\" TYPE=\"string\"><VALUE></VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Reservation\" TYPE=\"uint64\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ResourceSubType\" TYPE=\"string\"><VALUE>Microsoft:Hyper-V:Processor</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ResourceType\" TYPE=\"uint16\"><VALUE>3</VALUE></PROPERTY>" +
                                                                      $"<PROPERTY NAME=\"VirtualQuantity\" TYPE=\"uint64\" MODIFIED=\"TRUE\"><VALUE>{vm.PendingCoreCount}</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"VirtualQuantityUnits\" TYPE=\"string\"><VALUE>count</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"Weight\" TYPE=\"uint32\"><VALUE>100</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"AllowACountMCount\" TYPE=\"boolean\"><VALUE>true</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ApicMode\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"CpuBrandString\" TYPE=\"string\"><VALUE></VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"CpuGroupId\" TYPE=\"string\"><VALUE>00000000-0000-0000-0000-000000000000</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"DisableSpeculationControls\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableHostResourceProtection\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableLegacyApicMode\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePageShattering\" TYPE=\"uint8\"><VALUE>2</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonArchPmu\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonIpt\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonLbr\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonPebs\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnablePerfmonPmu\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnableSocketTopology\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"EnlightenmentSet\" TYPE=\"string\"></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ExposeVirtualizationExtensions\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ExtendedVirtualizationExtensions\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"HideHypervisorPresent\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"HwThreadsPerCore\" TYPE=\"uint64\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"L3CacheWays\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"L3ProcessorDistributionPolicy\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitCPUID\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitProcessorFeatures\" TYPE=\"boolean\"><VALUE>false</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"LimitProcessorFeaturesMode\" TYPE=\"uint8\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxClusterCountPerSocket\" TYPE=\"uint32\"><VALUE>4294967295</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxHwIsolatedGuests\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxNumaNodesPerSocket\" TYPE=\"uint64\"><VALUE>1</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxProcessorCountPerL3\" TYPE=\"uint32\"><VALUE>4294967295</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"MaxProcessorsPerNumaNode\" TYPE=\"uint64\"><VALUE>20</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"PerfCpuFreqCapMhz\" TYPE=\"uint32\"><VALUE>0</VALUE></PROPERTY>" +
                                                                      "<PROPERTY NAME=\"ProcessorFeatureSet\" TYPE=\"string\"></PROPERTY></INSTANCE>";
            }
            if (settingtype == VirtualMachine.HardwareType.MEMORY)
            {
                throw new NotImplementedException("Haven't formated the XML for the Memory : (");
            }
            return "No implement setting type given";
        }
    }
}