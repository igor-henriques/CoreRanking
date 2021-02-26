using CoreRanking.Model.ItensExchange;
using CoreRanking.Model.RankingPvP;
using PWToolKit.Enums;
using System.Collections.Generic;

namespace CoreRanking.Model
{
    public class PvPPrefs
    {
        public bool isMessageAllowed { get; set; }
        public bool isTriggerAllowed { get; set; }
        public bool isTrasferenceAllowed { get; set; }
        public BroadcastChannel Channel { get; set; }
        public string[] Messages { get; set; }
        public List<ItemAward> ItemsReward { get; set; }
        public int KillGold { get; set; }        
        public Elo Elos { get; set; }
        public int LevelDifference { get; set; }
        public int PointDifference { get; set; }
        public bool ShowKDA { get; set; }
        public PvPPrefs(bool isMessageAllowed, int BroadcastChannel, List<ItemAward> ItemsReward, int KillGold, string[] _messages, bool isTrasferenceAllowed, Elo elo, int levelDifference, int pointsDifference, bool showKDA, bool _isTriggerAllowed)
        {
            this.Elos = elo;
            this.isTrasferenceAllowed = isTrasferenceAllowed;
            this.KillGold = KillGold;
            this.isMessageAllowed = isMessageAllowed;
            this.ItemsReward = ItemsReward;
            this.Channel = (BroadcastChannel)BroadcastChannel;
            this.Messages = _messages;
            this.LevelDifference = levelDifference;
            this.PointDifference = pointsDifference;
            this.ShowKDA = showKDA;
            this.isTriggerAllowed = _isTriggerAllowed;
        }
    }
}
