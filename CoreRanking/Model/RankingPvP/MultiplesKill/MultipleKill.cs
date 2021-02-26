using PWToolKit.Enums;

namespace CoreRanking.Model.RankingPvP.MultiplesKill
{
    public class MultipleKill
    {
        public bool IsActive { get; set; }
        public bool IsMessageAllowed { get; set; }
        public BroadcastChannel Channel { get; set; }
        public DoubleKill DoubleKill { get; set; }
        public TripleKill TripleKill { get; set; }
        public QuadraKill QuadraKill { get; set; }
        public PentaKill PentaKill { get; set; }
    }
}
