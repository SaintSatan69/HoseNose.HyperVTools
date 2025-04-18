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
        private static Boolean IsCluster = false;
#pragma warning restore CS8618
        //In the event we need to touch WMI/CIM to talk to the hypervisor(s) (very likely)
        private const string CIM_VRTNS = "root/virtualization/v2";
        private const string CIM_VMCLASS = "Msvm_ComputerSystem";
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
            //Holy moley it works on a single server (my laptop)
            NodeServers = GetServers();
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
    }
}