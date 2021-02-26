using CoreRanking.Model.Data;
using CoreRanking.Model.PwServer;
using CoreRanking.Model.RankingPvP.MultiplesKill;
using CoreRanking.Models;
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

namespace CoreRanking.Model
{
    public class PvPWatch
    {
        static private long lastSize;
        private static string path;
        static PwServerConnection pwServer;
        private static List<ClassPointConfig> classesPointsConfig;
        static Timer _PvPWatch;
        static PvPPrefs prefs;
        static LogWriter newLog;
        public static async void Start(PwServerConnection _pwServer, PvPPrefs _prefs, List<ClassPointConfig> _classesPointsConfig)
        {
            classesPointsConfig = _classesPointsConfig;
            prefs = _prefs;
            path = _pwServer.logsPath + "world2.formatlog";
            lastSize = await GetFileSize(path);
            pwServer = _pwServer;

            PWGlobal.UsedPwVersion = pwServer.PwVersion;

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
                    battleData.AddRange(await ReadTail(path, UpdateLastFileSize(fileSize), pwServer));

                    battleData = battleData.Where(x => x != null).ToList();

                    foreach (var battle in battleData)
                    {
                        newLog = new LogWriter($"At {battle.date}, player {battle.killerId} killed {battle.killedId}");

                        Tuple<Role, Role, int> battleResult = await UploadPvPEvent(battle, pwServer);

                        if (prefs.isMessageAllowed && battleResult.Item1 != null)
                        {
                            await WorldChatWatch.SendMessage(await BuildMessage(battleResult.Item2.CharacterName, battleResult.Item1.CharacterName, battleResult.Item3), prefs.Channel, pwServer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                newLog = new LogWriter(ex.ToString());
            }
        }

        public static async Task<List<Battle>> ReadTail(string filename, long offset, PwServerConnection pwServerConnection)
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
        public static async Task<Tuple<Role, Role, int>> UploadPvPEvent(Battle battleData, PwServerConnection pwServerConnection)
        {
            using (var db = new ApplicationDbContext())
            {
                db.Battle.Add(battleData);

                Role killedChar = db.Role.Where(x => x.RoleId.Equals(battleData.killedId)).FirstOrDefault();
                Role killerChar = db.Role.Where(x => x.RoleId.Equals(battleData.killerId)).FirstOrDefault();

                if (killedChar is null)
                {
                    killedChar = RoleWatch.CharacterInfo(battleData.killedId, "created", pwServerConnection, 0);

                    if (killedChar is null)
                    {
                        return new Tuple<Role, Role, int>(null, null, 0);
                    }
                    else
                    {
                        await RoleWatch.UploadRoleEvent(killedChar, pwServerConnection);
                    }                    
                }

                if (killerChar is null)
                {
                    killerChar = RoleWatch.CharacterInfo(battleData.killerId, "created", pwServerConnection, 0);

                    if (killerChar is null)
                    {
                        return new Tuple<Role, Role, int>(null, null, 0);
                    }
                    else
                    {
                        await RoleWatch.UploadRoleEvent(killerChar, pwServerConnection);
                    }
                }

                string killerIp = db.Account.Where(x => x.Id.Equals(killerChar.AccountId)).Select(x => x.Ip).FirstOrDefault();
                string killedIp = db.Account.Where(x => x.Id.Equals(killedChar.AccountId)).Select(x => x.Ip).FirstOrDefault();

                #region BAN_STUFF
                var banRecords = db.Banned.Where(x => x.RoleId.Equals(killerChar.RoleId)).ToList();

                if (banRecords.Count > 0)
                {
                    if (banRecords.Last().BanTime.Subtract(DateTime.Now) > TimeSpan.FromSeconds(0))
                    {
                        PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, $"Você não ganhará pontos nem kills até {banRecords.Last().BanTime} por ter sido punido por FreeKill.");
                        return new Tuple<Role, Role, int>(null, null, 0);
                    }
                }
                #endregion

                #region IP_STUFF
                bool someoneHasntIp = false;

                if (killerIp is null || killedIp is null)
                {
                    if (killedIp is null)
                        PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, $"Você não ganhará pontos por matar {killedChar.CharacterName} até que ele relogue a conta para entrar no Ranking.");

                    if (killerIp is null)
                        PrivateChat.Send(pwServer.gdeliveryd, killedChar.RoleId, $"Você não está participando do Ranking até que relogue sua conta.");

                    someoneHasntIp = true;
                }
                if (killedIp is null)
                {
                    PrivateChat.Send(pwServer.gdeliveryd, killedChar.RoleId, $"Você não está participando do Ranking até que relogue sua conta.");
                    someoneHasntIp = true;
                }

                if (someoneHasntIp)
                    return new Tuple<Role, Role, int>(null, null, 0);
                #endregion

                #region LEVEL_STUFF                
                if ((killerChar.Level - prefs.LevelDifference) >= killedChar.Level && killedChar.Level <= (killerChar.Level + prefs.LevelDifference))
                {
                    PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, $"Você não ganhará pontos por matar {killedChar.CharacterName} porque a diferença de nível entre vocês não está no intervalo permitido para contabilizar pontos.");
                    PrivateChat.Send(pwServer.gdeliveryd, killedChar.RoleId, $"Você não perdeu pontos por ser morto por {killerChar.CharacterName} porque a diferença de nível entre vocês não está no intervalo permitido para contabilizar pontos.");

                    newLog = new LogWriter($"{killerChar.CharacterName} tentou matar {killedChar.CharacterName}, que é {killerChar.Level - killedChar.Level} níveis menor");
                    return new Tuple<Role, Role, int>(null, null, 0);
                }
                #endregion

                #region POINT_STUFF
                if (killedChar.Points <= prefs.PointDifference)
                {
                    PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, $"Você não ganhará pontos por matar {killedChar.CharacterName} porque a pontuação dele no ranking está abaixo que a necessária.");
                    PrivateChat.Send(pwServer.gdeliveryd, killedChar.RoleId, $"Você não perdeu pontos por ser morto por {killerChar.CharacterName} porque a sua pontuação está abaixo que a necessária.");

                    return new Tuple<Role, Role, int>(null, null, 0);
                }
                #endregion

                if (!killerIp.Equals(killedIp))
                {                    
                    killedChar.Points -= classesPointsConfig.Where(x => x.Ocuppation.Equals(ConvertClass(killedChar.CharacterClass))).Select(x => x.onDeath).FirstOrDefault();
                    killerChar.Points += classesPointsConfig.Where(x => x.Ocuppation.Equals(ConvertClass(killerChar.CharacterClass))).Select(x => x.onKill).FirstOrDefault();

                    GiveCash(killerChar.AccountId);

                    await db.SaveChangesAsync();

                    int kills = db.Battle.Where(x => x.killerId.Equals(battleData.killerId)).Count();

                    await MultipleKillWatch.Trigger(killerChar);

                    return new Tuple<Role, Role, int>(killedChar, killerChar, kills);
                }
                else
                {
                    if (killedIp is null)
                    {
                        PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, "Relogue sua conta para participar do Ranking e ganhar pontos.");
                        PrivateChat.Send(pwServer.gdeliveryd, killedChar.RoleId, "Relogue sua conta para participar do Ranking e ganhar pontos.");
                    }

                    newLog = new LogWriter($"{killedChar.CharacterName}: Matar personagens que estão na mesma rede de internet que você não contabiliza pontos.");
                    PrivateChat.Send(pwServer.gdeliveryd, killerChar.RoleId, "Matar personagens que estão na mesma rede de internet que você não contabiliza pontos.");
                    return new Tuple<Role, Role, int>(null, null, 0);
                }
            }
        }
        public static void GiveCash(int toUserId)
        {
            if (prefs.KillGold > 0)
            {
                DebugAddCash.Add(pwServer.gamedbd, toUserId, prefs.KillGold * 1000);
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
                    date = when,
                    killerId = _killerId,
                    killedId = _killedId
                };
            }
            else
            {
                return null;
            }
        }
        private static async Task<string> BuildMessage(string killer, string dead, int kills)
        {
            Random rand = new Random();

            string message = prefs.Messages[rand.Next(0, prefs.Messages.Length - 1)].Replace("$killer", killer).Replace("$dead", dead) + ". Kills: " + kills + (prefs.ShowKDA ? $". KDA: {(await WorldChatWatch.GetKDA(killer)).ToString("0.00")}" : default);
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
