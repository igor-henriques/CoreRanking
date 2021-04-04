using CoreRanking.Data;
using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using PWToolKit.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Watchers
{
    public class FirewallWatch
    {
        BroadcastChannel Channel { get; set; }
        public bool IsActived { get; set; }
        public int KillsToActive { get; set; }
        public int TimeToActive { get; set; }
        public int BanTime { get; set; }

        static Timer FirewallCheck;
        public ServerConnection server;

        public FirewallWatch(FirewallDefinitions defs, ServerConnection pwServer)
        {
            this.Channel = (BroadcastChannel)defs.Channel;
            this.IsActived = defs.Active;
            this.KillsToActive = defs.KillLimit;
            this.TimeToActive = defs.TimeLimit + 3;
            this.BanTime = defs.BanTime;
            this.server = pwServer;

            FirewallCheck = new Timer(5000);
            FirewallCheck.Elapsed += FirewallWatcher;

            if (IsActived)
            {
                FirewallCheck.Start();
            }
        }
        private async void FirewallWatcher(object sender, ElapsedEventArgs e)
        {
            await Check();
        }

        private async Task Check()
        {
            using (var db = new ApplicationDbContext())
            {
                DateTime FromThere = DateTime.Now.Subtract(TimeSpan.FromSeconds(TimeToActive));

                List<Battle> allBattles = db.Battle.Where(x => x.Date >= FromThere).ToList();

                List<int> uniqueBattles = allBattles.Select(x => x.KillerId).Distinct().ToList();
                List<List<Battle>> battleFromPlayer = new List<List<Battle>>();

                foreach (var battle in uniqueBattles)
                {
                    List<Battle> _playerBattle = new List<Battle>();

                    foreach (var playerBattle in allBattles.Where(x => x.KillerId.Equals(battle)).ToList())
                    {
                        _playerBattle.Add(allBattles.Where(y => y.KillerId.Equals(battle)).Skip(_playerBattle.Count).FirstOrDefault());
                    }

                    battleFromPlayer.Add(_playerBattle);
                }

                List<Battle> toRemove = new List<Battle>();

                foreach (var playerToAnalize in battleFromPlayer)
                {
                    bool wasBanned = false;
                    var suspectBattles = GetFrequencies(playerToAnalize.Select(x => x.KilledId).ToList());

                    foreach (var suspectBattle in suspectBattles)
                    {
                        if (suspectBattle.Value >= KillsToActive)
                        {                            
                            await Ban(playerToAnalize.First().KillerId);
                            wasBanned = true;
                            toRemove.AddRange(playerToAnalize);
                        }
                    }

                    if (wasBanned)
                    {
                        await WorldChatWatch.SendMessage($"RANKING FIREWALL: {db.Role.Where(x => x.RoleId.Equals(playerToAnalize.First().KillerId)).FirstOrDefault().CharacterName} foi punido devido a FreeKill.", Channel, server);                        
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
            try
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

                    LogWriter.Write($"Role {roleId} foi banido por {BanTime} segundos devido ao Firewall de PvP.");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }                    
        }
        private async Task RemoveRecords(List<Battle> BattleToRemove)
        {
            try
            {
                if (BattleToRemove.Count > 0)
                {
                    using (var db = new ApplicationDbContext())
                    {
                        db.Battle.RemoveRange(BattleToRemove);

                        BattleToRemove.ForEach(x => x.KilledRole.Death += 1);
                        BattleToRemove.ForEach(x => x.KillerRole.Kill -= 1);

                        LogWriter.Write($"Ranking Firewall: foram removidos {BattleToRemove.Count} registros de PvP.");

                        await db.SaveChangesAsync();
                    }
                }                
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }            
        }
    }
}
