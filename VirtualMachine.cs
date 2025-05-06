namespace HyperVTools
{
    //The plan is to have a list of all the VMS in the cluster/stand alone host
    //and every 100ms poll the cluster for each VM so that in the event the VM 
    //goes down it will hold it hostage long enough to modify the hardware (will be 
    //Hard to do)
    public class VirtualMachine
    {
        public Guid Id { get; } = Guid.Empty;
        public string FriendlyName { get; }
        public int CoreCount { get; private set; }
        public int PendingCoreCount { get; set; } = 0;
        public int GBMemory { get; private set; }
        public int PendingGBMemory { get; set; } = 0;
        public bool IsIgnored { get; set;}
        public bool IsShuttingDown { get; set; } = false;
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
        //All the states we would be looking for to be able to process requests
        public enum VirtualMachineStates
        {
            UNKNOWN = 0,
            OK = 2,
            OFF = 3,
            SHUTDOWN = 4
        }
        /// <summary>
        /// All the Avaiable states a VM can be issued to move to (without code spagetti), Disabled instantly turns if off so be carefull
        /// </summary>
        public enum RequestableStates
        {
            Enabled = 2,
            Disabled = 3,
            ShutDown = 4,
            Offline = 6,
            Reboot = 10,
            Reset = 11
        }
        public enum HardwareType
        {
            CPU,
            MEMORY
        }
    }
}
