using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CoreRanking.Model.RankingPvP.MultiplesKill
{
    public class PlayerControl
    {
        public Stopwatch Clock { get; set; }
        public Role Role { get; set; }
        public int Kills { get; set; }        
    }
}
