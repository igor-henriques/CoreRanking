namespace CoreRanking.Model
{
    public class FirewallDefinitions
    {
        public bool Active { get; set; }
        public int Channel { get; set; }
        public int CheckInterval { get; set; }
        public int KillLimit { get; set; }
        public int TimeLimit { get; set; }
        public int BanTime { get; set; }
    }
}
