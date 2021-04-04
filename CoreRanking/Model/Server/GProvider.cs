using PWToolKit.Packets;

namespace CoreRanking.Model.Server
{
    public class GProvider : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GProvider(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
