using System;

namespace CoreRanking.Model
{
    public class Battle
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public int killerId { get; set; }
        public int killedId { get; set; }
    }
}
