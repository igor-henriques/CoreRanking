using PWToolKit.Packets;

namespace CoreRanking.Model
{
    public class GDeliverydConfig : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GDeliverydConfig(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}