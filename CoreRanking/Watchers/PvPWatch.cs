using CoreRanking.Data;
using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using PWToolKit;
using PWToolKit.API.Gamedbd;
using PWToolKit.API.GDeliveryd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Watchers
{
    public class PvPWatch
    {
        static private long lastSize;
        private static string path;
        static ServerConnection server;
        private static List<ClassPointConfig> classesPointsConfig;
        static Timer _PvPWatch;
        static RankingDefinitions prefs;
        public static async void Start(ServerConnection _server, RankingDefinitions _prefs, List<ClassPointConfig> _classesPointsConfig)
        {
            classesPointsConfig = _classesPointsConfig;
            prefs = _prefs;
            path = _server.logsPath + "world2.formatlog";
            lastSize = await GetFileSize(path);
            server = _server;

            PWGlobal.UsedPwVersion = server.PwVersion;

            _PvPWatch = new Timer(500);
            _PvPWatch.Elapsed += PvPTick;
            _PvPWatch.Start();
        }

        private static async void PvPTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                long fileSize = await GetFileSize(path);

                if (fileSize > lastSize)
                {
                    List<Battle> battleData = new List<Battle>();
                    battleData.AddRange(await ReadTail(path, UpdateLastFileSize(fileSize), server));

                    battleData = battleData.Where(x => x != null).ToList();

                    foreach (var battle in battleData)
                    {
                        LogWriter.Write($"At {battle.Date}, player {battle.KillerId} killed {battle.KilledId}");

                        Tuple<Role, Role, int> battleResult = await UploadPvPEvent(battle);

                        if (battleResult != null)
                            await MultipleKillWatch.Trigger(battleResult.Item2);

                        if (prefs.isMessageAllowed && battleResult != null)
                        {
                            await WorldChatWatch.SendMessage(await BuildMessage(battleResult.Item2.CharacterName, battleResult.Item1.CharacterName, battleResult.Item3), prefs.Channel, server);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }

        public static async Task<List<Battle>> ReadTail(string filename, long offset, ServerConnection ServerConnection)
        {
            try
            {
                byte[] bytes;

                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(offset * -1, SeekOrigin.End);

                    bytes = new byte[offset];
                    fs.Read(bytes, 0, (int)offset);
                }

                List<string> logs = Encoding.Default.GetString(bytes).Split(new string[] { "\n" }[0]).ToList();
                List<Battle> battlesResponse = new List<Battle>();

                GC.Collect();

                foreach (var log in logs)
                {
                    battlesResponse.Add(await GatherPvPData(log));
                }

                return battlesResponse;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        public static async Task<Tuple<Role, Role, int>> UploadPvPEvent(Battle battleData)
        {
            try
            {
                var verifiedData = await EnsureRoleCreation(battleData);

                using (var db = new ApplicationDbContext())
                {
                    db.Battle.Add(battleData);

                    await db.SaveChangesAsync();
                }

                if (verifiedData is null)
                    return default;

                if (await CheckBan(verifiedData))
                    return default;

                if (await CheckIP(verifiedData))
                    return default;

                if (await CheckLevelRange(verifiedData))
                    return default;

                if (await CheckPointLimit(verifiedData))
                    return default;

                return await LastChecks(verifiedData);                                
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static async Task<Tuple<Role, Role, int>> LastChecks(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role KillerRole = await db.Role.FindAsync(battleData.KillerId);
                    Role KilledRole = await db.Role.FindAsync(battleData.KilledId);

                    if (KilledRole.Points > prefs.MinimumPoints)
                    {
                        KillerRole.Points += classesPointsConfig.Where(x => x.Ocuppation.Equals(ConvertClass(KillerRole.CharacterClass))).Select(x => x.onKill).FirstOrDefault();
                        KillerRole.Kill += 1;
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, KilledRole.RoleId, "Você não contará pontos no PvP porque atingiu o limite mínimo de pontos. PS: você ainda perderá pontos ao morrer.");
                        PrivateChat.Send(server.gdeliveryd, KillerRole.RoleId, $"Você não ganhará pontos no PvP ao matar {KilledRole.CharacterName} porque o(a) jogador(a) atingiu o limite mínimo de pontos para ser válido no PvP.");

                        LogWriter.Write($"Personagem {KilledRole.CharacterName} atingiu o limite mínimo de pontos ({prefs.MinimumPoints} e por isso não perdeu mais pontos por ter morrido.");
                    }

                    KilledRole.Death += 1;
                    KilledRole.Points -= classesPointsConfig.Where(x => x.Ocuppation.Equals(ConvertClass(KilledRole.CharacterClass))).Select(x => x.onDeath).FirstOrDefault();

                    GiveCash(KillerRole.AccountId);

                    await db.SaveChangesAsync();

                    return new Tuple<Role, Role, int>(KilledRole, KillerRole, KillerRole.Kill);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }

        /// <summary>
        /// Checa se o personagem já alcançou o limite mínimo de pontos pré-estipulado para que se pare de perder pontos.
        /// </summary>
        /// <param name="battleData"></param>
        /// <returns></returns>
        private static async Task<bool> CheckPointLimit(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (battleData.KilledRole.Points <= prefs.PointDifference)
                    {
                        PrivateChat.Send(server.gdeliveryd, battleData.KillerRole.RoleId, $"Você não ganhará pontos por matar {battleData.KilledRole.CharacterName} porque a pontuação dele no ranking está abaixo que a necessária.");
                        PrivateChat.Send(server.gdeliveryd, battleData.KilledRole.RoleId, $"Você não perdeu pontos por ser morto por {battleData.KillerRole.CharacterName} porque a sua pontuação está abaixo que a necessária.");

                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return true;
        }
        private static async Task<bool> CheckLevelRange(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if ((battleData.KillerRole.Level - prefs.LevelDifference) >= battleData.KilledRole.Level && battleData.KilledRole.Level <= (battleData.KillerRole.Level + prefs.LevelDifference))
                    {
                        PrivateChat.Send(server.gdeliveryd, battleData.KillerRole.RoleId, $"Você não ganhará pontos por matar {battleData.KilledRole.CharacterName} porque a diferença de nível entre vocês não está no intervalo permitido para contabilizar pontos.");
                        PrivateChat.Send(server.gdeliveryd, battleData.KilledRole.RoleId, $"Você não perdeu pontos por ser morto por {battleData.KillerRole.CharacterName} porque a diferença de nível entre vocês não está no intervalo permitido para contabilizar pontos.");

                        LogWriter.Write($"{battleData.KillerRole.CharacterName} tentou matar {battleData.KilledRole.CharacterName}, que é {battleData.KillerRole.Level - battleData.KilledRole.Level} níveis menor");
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return true;
        }
        private static async Task<bool> CheckIP(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    string killerIp = db.Account.Where(x => x.Id.Equals(battleData.KillerRole.AccountId)).Select(x => x.Ip).FirstOrDefault();
                    string killedIp = db.Account.Where(x => x.Id.Equals(battleData.KilledRole.AccountId)).Select(x => x.Ip).FirstOrDefault();

                    bool someoneHasntIp = false;
                    bool someoneHasZeroIP = false;

                    if (killerIp is null || killedIp is null)
                    {
                        if (killedIp is null)
                            PrivateChat.Send(server.gdeliveryd, battleData.KillerRole.RoleId, $"Você não ganhará pontos por matar {battleData.KilledRole.CharacterName} até que ele relogue a conta para entrar no Ranking.");

                        if (killerIp is null)
                            PrivateChat.Send(server.gdeliveryd, battleData.KilledRole.RoleId, $"Você não está participando do Ranking até que relogue sua conta.");

                        someoneHasntIp = true;
                    }
                    else
                    {
                        if (killedIp.Equals("0"))
                        {
                            PrivateChat.Send(server.gdeliveryd, battleData.KilledId, "Relogue sua conta para participar do Ranking e ganhar pontos.");
                            someoneHasZeroIP = true;
                        }

                        if (killerIp.Equals("0"))
                        {
                            PrivateChat.Send(server.gdeliveryd, battleData.KilledId, "Relogue sua conta para participar do Ranking e ganhar pontos.");
                            someoneHasZeroIP = true;
                        }

                        if (killedIp.Equals(killerIp) && !killedIp.Equals("0"))
                        {
                            PrivateChat.Send(server.gdeliveryd, battleData.KilledId, "Matar personagens que estão na mesma rede de internet que você não contabiliza pontos.");
                            LogWriter.Write($"{battleData.KilledRole.CharacterName}: Matar personagens que estão na mesma rede de internet que você não contabiliza pontos.");
                        }

                    }

                    if (someoneHasntIp || someoneHasZeroIP)
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return true;
        }
        private static async Task<bool> CheckBan(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    var banRecords = db.Banned.Where(x => x.RoleId.Equals(battleData.KillerId)).ToList();

                    if (banRecords.Count > 0)
                    {
                        if (banRecords.Last().BanTime.Subtract(DateTime.Now) > TimeSpan.FromSeconds(0))
                        {
                            PrivateChat.Send(server.gdeliveryd, battleData.KillerId, $"Você não ganhará pontos nem kills até {banRecords.Last().BanTime} por ter sido punido por FreeKill.");
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return true;
        }
        private static async Task<Battle> EnsureRoleCreation(Battle battleData)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role killedChar = db.Role.Where(x => x.RoleId.Equals(battleData.KilledId)).FirstOrDefault();
                    Role killerChar = db.Role.Where(x => x.RoleId.Equals(battleData.KillerId)).FirstOrDefault();

                    if (killedChar is null)
                    {
                        killedChar = await RoleWatch.CreateRole(battleData.KilledId, server);

                        if (killedChar is null)
                        {
                            return default;
                        }
                    }

                    if (killerChar is null)
                    {
                        killerChar = await RoleWatch.CreateRole(battleData.KillerId, server);

                        if (killerChar is null)
                        {
                            return default;
                        }
                    }

                    return new Battle { KilledRole = killedChar, KillerRole = killerChar, KilledId = killedChar.RoleId, KillerId = killerChar.RoleId };
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static void GiveCash(int toUserId)
        {
            if (prefs.KillGold > 0)
            {
                DebugAddCash.Add(server.gamedbd, toUserId, prefs.KillGold * 1000);
            }
        }
        public static string ConvertClass(string fullClassName)
        {
            Dictionary<string, string> translateClassName = new Dictionary<string, string>();

            translateClassName.Add("Warrior", "WR");
            translateClassName.Add("Mage", "MG");
            translateClassName.Add("Shaman", "PSY");
            translateClassName.Add("Druid", "WF");
            translateClassName.Add("Werewolf", "WB");
            translateClassName.Add("Assassin", "MC");
            translateClassName.Add("Archer", "EA");
            translateClassName.Add("Priest", "EP");
            translateClassName.Add("Guardian", "SK");
            translateClassName.Add("Mystic", "MS");
            translateClassName.Add("Ghost", "RT");
            translateClassName.Add("Reaper", "TM");

            return translateClassName[fullClassName];
        }
        private static async Task<Battle> GatherPvPData(string log)
        {
            if (log.Contains("die:roleid") && !log.Contains("attacker=-"))
            {
                DateTime when = Convert.ToDateTime(System.Text.RegularExpressions.Regex.Match(log, @"(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})").Value);
                int _killerId = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"attacker=([0-9]*)").Value.Replace("attacker=", ""));
                int _killedId = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"roleid=([0-9]*)").Value.Replace("roleid=", ""));

                return new Battle
                {
                    Date = when,
                    KillerId = _killerId,
                    KilledId = _killedId
                };
            }

            return default;
        }
        private static async Task<string> BuildMessage(string killer, string dead, int kills)
        {
            string message;

            if (prefs.Channel is PWToolKit.Enums.BroadcastChannel.System)
            {
                message = prefs.Messages.ElementAt(new Random().Next(prefs.Messages.Length)).Replace("$killer", $"&{killer}&").Replace("$dead", $"&{dead}&") + (prefs.ShowKDA ? $". Kills: {kills}. KDA: {(await WorldChatWatch.GetKDA(killer)).ToString("0.00")}" : default);
            }
            else
            {
                message = prefs.Messages.ElementAt(new Random().Next(prefs.Messages.Length)).Replace("$killer", $"{killer}").Replace("$dead", $"{dead}") + (prefs.ShowKDA ? $". Kills: {kills}. KDA: {(await WorldChatWatch.GetKDA(killer)).ToString("0.00")}" : default);
            }

            return message;
        }
        private static long UpdateLastFileSize(long fileSize)
        {
            long difference = fileSize - lastSize;
            lastSize = fileSize;

            return difference;
        }

        public static async Task<long> GetFileSize(string fileName)
        {
            return new System.IO.FileInfo(fileName).Length;
        }
    }
}
