using PWToolKit.Packets;

namespace CoreRanking.Model.Server
{
    public class GDeliveryd : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GDeliveryd(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}