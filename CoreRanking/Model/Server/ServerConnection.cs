using PWToolKit;

namespace CoreRanking.Model.Server
{
    public class ServerConnection
    {
        public string rootPath { get; set; }
        public string logsPath { get; set; }
        public PwVersion PwVersion { get; set; }
        public Gamedbd gamedbd { get; set; }
        public GProvider gprovider { get; set; }
        public GDeliveryd gdeliveryd { get; set; }

        public ServerConnection(string GamedbdHost, int GamedbdPort, string GProviderHost, int GProviderPort, string GDeliverydHost, int GDeliverydPort, PwVersion PwVersion, string logsPath)
        {
            this.logsPath = logsPath;
            this.PwVersion = PwVersion;
            gamedbd = new Gamedbd(GamedbdHost, GamedbdPort);
            gprovider = new GProvider(GProviderHost, GProviderPort);
            gdeliveryd = new GDeliveryd(GDeliverydHost, GDeliverydPort);
        }
    }    
}
