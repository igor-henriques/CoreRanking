using System;
using System.Collections.Generic;
using System.Text;

namespace CoreRanking.Model.RankingPvE
{
    public class Collect
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int RoleId { get; set; }
        public int Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
