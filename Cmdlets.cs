using HyperVTools;
using System.Management.Automation;
namespace HyperVToolsPowershell
{
    [Cmdlet(VerbsCommunications.Send,"VMChange")]
    public class SendVMChange : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public VirtualMachine VirtualMachine 
        {
            get { return vm; }
            set { vm = value; }
        }
        private VirtualMachine vm;


        protected override void ProcessRecord() 
        { 
        
        
        }
    }
}
