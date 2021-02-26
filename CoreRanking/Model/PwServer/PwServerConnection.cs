using PWToolKit;

namespace CoreRanking.Model
{
    public class PwServerConnection
    {
        public string rootPath { get; set; }
        public string logsPath { get; set; }
        public PwVersion PwVersion { get; set; }
        public GamedbdConfig gamedbd { get; set; }
        public GProviderConfig gprovider { get; set; }
        public GDeliverydConfig gdeliveryd { get; set; }

        public PwServerConnection(string GamedbdHost, int GamedbdPort, string GProviderHost, int GProviderPort, string GDeliverydHost, int GDeliverydPort, PwVersion PwVersion, string rootPath, string logsPath)
        {
            this.logsPath = logsPath;
            this.rootPath = rootPath;
            this.PwVersion = PwVersion;
            gamedbd = new GamedbdConfig(GamedbdHost, GamedbdPort);
            gprovider = new GProviderConfig(GProviderHost, GProviderPort);
            gdeliveryd = new GDeliverydConfig(GDeliverydHost, GDeliverydPort);
        }
    }    
}
