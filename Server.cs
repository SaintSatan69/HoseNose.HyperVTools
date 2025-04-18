namespace HyperVTools
{
    /// <summary>
    /// A Server name that is a Hyper-V Host that contains VM guests
    /// </summary>
    public class Server
    {
        public string ServerName { get; }
        public int NumberOfRoles { get; private set; }
        public Server(string serverName, int numberOfRoles  )
        {
            ServerName = serverName;
            NumberOfRoles = numberOfRoles;
        }
    }
}