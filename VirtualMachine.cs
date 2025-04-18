namespace HyperVTools
{
    //The Plan is to have a list of all the VMS in the cluster/Stand Alone Host
    //and ever 100ms poll the cluster for each VM so that in the event the VM 
    //Goes Down It hold it hostage long enough to modify the hardware (will be 
    //Hard to do)
    public class VirtualMachine
    {
        public Guid Id { get; }
        public string FriendlyName { get; }
        public int CoreCount { get; private set; }
        public int PendingCoreCount { get; set; }
        public int GBMemory { get; private set; }
        public int PendingGBMemory { get; set; }
        public VirtualMachine() { }
    }
}
