using System.Diagnostics;

namespace CoreRanking.Model.RankingPvP
{
    public class PlayerControl
    {
        public Stopwatch Clock { get; set; }
        public Role Role { get; set; }
        public int Kills { get; set; }        
    }
}
