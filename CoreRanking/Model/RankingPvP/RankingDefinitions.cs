using CoreRanking.Model.ItensExchange;
using PWToolKit.Enums;
using System.Collections.Generic;

namespace CoreRanking.Model.RankingPvP
{
    public class RankingDefinitions
    {
        public bool isMessageAllowed { get; }
        public bool isTriggerAllowed { get; }
        public bool isTrasferenceAllowed { get; }
        public BroadcastChannel Channel { get; }
        public string[] Messages { get; }
        public List<ItemAward> ItemsReward { get; }
        public int KillGold { get; }
        public Elo Elos { get; }
        public int LevelDifference { get; }
        public int PointDifference { get; }
        public int MinimumPoints { get; }
        public bool ShowKDA { get; }
        public int AmountPlayersOnPodium { get; }
        public string MessageColor { get; }
        public List<int> Emojis { get; }
        public int QuestIdResetKDA { get; }
        public RankingDefinitions(bool isMessageAllowed, int BroadcastChannel, List<ItemAward> ItemsReward, int KillGold, string[] _messages, bool isTrasferenceAllowed, Elo elo, int levelDifference, int pointsDifference, bool showKDA, bool _isTriggerAllowed, int minimumPoints, int amountPlayersOnPodium, string _messageColor, List<int> _emojis, int questIdResetKDA)
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
            this.MinimumPoints = minimumPoints;
            this.AmountPlayersOnPodium = amountPlayersOnPodium;
            this.MessageColor = _messageColor;
            this.Emojis = _emojis;
            this.QuestIdResetKDA = questIdResetKDA;
        }
    }
}
