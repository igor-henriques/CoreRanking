using System;
using System.Collections.Generic;
using System.Text;

namespace CoreRanking.Model.RankingFirewall
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
