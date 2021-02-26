using PWToolKit.Packets;

namespace CoreRanking.Model
{
    public class GProviderConfig : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GProviderConfig(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
