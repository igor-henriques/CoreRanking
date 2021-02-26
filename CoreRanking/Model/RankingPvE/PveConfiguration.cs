namespace CoreRanking.Model.RankingPvE
{
    public class PveConfiguration
    {
        public bool isActive { get; set; }
        public double herbPoint { get; set; }
        public double mineralPoint { get; set; }

        public PveConfiguration(bool _isActive, double _herbPoint, double _mineralPoint)
        {
            isActive = _isActive;
            herbPoint = _herbPoint;
            mineralPoint = _mineralPoint;
        }
    }
}
