using PWToolKit.Packets;

namespace CoreRanking.Model
{
    public class GamedbdConfig : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GamedbdConfig(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
