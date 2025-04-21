namespace HyperVTools
{
    //The plan is to have a list of all the VMS in the cluster/stand alone host
    //and every 100ms poll the cluster for each VM so that in the event the VM 
    //goes down it will hold it hostage long enough to modify the hardware (will be 
    //Hard to do)
    public class VirtualMachine
    {
        public Guid Id { get; }
        public string FriendlyName { get; }
        public int CoreCount { get; private set; }
        public int PendingCoreCount { get; set; } = 0;
        public int GBMemory { get; private set; }
        public int PendingGBMemory { get; set; } = 0;
        public bool IsIgnored { get; set;}
        public VirtualMachineStates VirtualMachineState { get; set; }
        public VirtualMachine(Guid VMID, string NiceName,int coreCount,int gbmem) 
        { 
            Id = VMID;
            FriendlyName = NiceName;
            CoreCount = coreCount;
            GBMemory = gbmem;
        }
        public void UpdateVirtualMachine(string Hardware,int value)
        {
            switch (Hardware)
            {
                case "CPU":
                    this.CoreCount = value;
                    this.PendingCoreCount = 0;
                    break;
                case "MEMORY":
                    this.GBMemory = value;
                    this.PendingGBMemory = 0;
                    break;
            }
        }
        public enum VirtualMachineStates
        {
            UNKNOWN = 0,
            OK = 2,
            SHUTDOWN = 3,
        }
        public enum RequestableStates
        {
            Enabled = 2,
            Disabled = 3,
            ShutDown = 4,
            Offline = 6,
            Reboot = 10,
            Reset = 11
        }
    }
}
