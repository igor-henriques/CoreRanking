using CoreRanking.Model.Data;
using CoreRanking.Model.PwServer;
using CoreRanking.Model.RankingFirewall;
using CoreRanking.Models;
using PWToolKit.API.GDeliveryd;
using PWToolKit.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Model
{
    public class CoreRankingFirewall
    {
        BroadcastChannel Channel { get; set; }
        public bool IsActived { get; set; }
        public int KillsToActive { get; set; }
        public int TimeToActive { get; set; }
        public int BanTime { get; set; }

        static Timer FirewallCheck;
        public PwServerConnection PwServer;
        static LogWriter newLog;

        public CoreRankingFirewall(FirewallDefinitions defs, PwServerConnection pwServer)
        {
            this.Channel = (BroadcastChannel)defs.Channel;
            this.IsActived = defs.Active;
            this.KillsToActive = defs.KillLimit;
            this.TimeToActive = defs.TimeLimit + 3;
            this.BanTime = defs.BanTime;
            this.PwServer = pwServer;

            FirewallCheck = new Timer(5000);
            FirewallCheck.Elapsed += FirewallWatch;

            if (IsActived)
            {
                FirewallCheck.Start();
            }
        }
        private async void FirewallWatch(object sender, ElapsedEventArgs e)
        {
            await Check();
        }

        private async Task Check()
        {
            using (var db = new ApplicationDbContext())
            {
                DateTime FromThere = DateTime.Now.Subtract(TimeSpan.FromSeconds(TimeToActive));

                List<Battle> allBattles = db.Battle.Where(x => x.date >= FromThere).ToList();
                List<int> uniqueBattles = allBattles.Select(x => x.killerId).Distinct().ToList();
                List<List<Battle>> battleFromPlayer = new List<List<Battle>>();

                foreach (var battle in uniqueBattles)
                {
                    List<Battle> _playerBattle = new List<Battle>();

                    foreach (var playerBattle in allBattles.Where(x => x.killerId.Equals(battle)).ToList())
                    {
                        _playerBattle.Add(allBattles.Where(y => y.killerId.Equals(battle)).Skip(_playerBattle.Count).FirstOrDefault());
                    }

                    battleFromPlayer.Add(_playerBattle);
                }

                List<Battle> toRemove = new List<Battle>();

                foreach (var playerToAnalize in battleFromPlayer)
                {
                    bool wasBanned = false;
                    var suspectBattles = GetFrequencies(playerToAnalize.Select(x => x.killedId).ToList());

                    foreach (var suspectBattle in suspectBattles)
                    {
                        if (suspectBattle.Value >= KillsToActive)
                        {                            
                            await Ban(playerToAnalize.First().killerId);
                            wasBanned = true;
                            toRemove.AddRange(playerToAnalize);
                        }
                    }

                    if (wasBanned)
                    {
                        await WorldChatWatch.SendMessage($"RANKING FIREWALL: {db.Role.Where(x => x.RoleId.Equals(playerToAnalize.First().killerId)).FirstOrDefault().CharacterName} foi punido devido a FreeKill.", Channel, PwServer);                        
                    }
                }

                await RemoveRecords(toRemove);
            }
        }
        static Dictionary<int, int> GetFrequencies(List<int> values)
        {
            var result = new Dictionary<int, int>();
            foreach (int value in values)
            {
                if (result.TryGetValue(value, out int count))
                {
                    result[value] = count + 1;
                }
                else
                {
                    result.Add(value, 1);
                }
            }

            return result;
        }

        private async Task Ban(int roleId)
        {
            using (var db = new ApplicationDbContext())
            {
                var roleBans = db.Banned.Where(x => x.RoleId.Equals(roleId)).Count();
                roleBans = roleBans is 0 ? 1 : roleBans;

                db.Banned.Add(new Banned
                {
                    RoleId = roleId,
                    BanTime = DateTime.Now.AddSeconds(3600 * roleBans)
                });

                await db.SaveChangesAsync();
                
                newLog = new LogWriter($"Role {roleId} foi banido por {BanTime} segundos devido ao Firewall de PvP.");
            }            
        }
        private async Task RemoveRecords(List<Battle> BattleToRemove)
        {
            using (var db = new ApplicationDbContext())
            {
                db.Battle.RemoveRange(BattleToRemove);

                await db.SaveChangesAsync();
            }
        }
    }
}
