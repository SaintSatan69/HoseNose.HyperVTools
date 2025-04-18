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
        public VirtualMachine(Guid VMID, string NiceName,int coreCount,int gbmem) 
        { 
            Id = VMID;
            FriendlyName = NiceName;
            CoreCount = coreCount;
            GBMemory = gbmem;
        }
    }
}
