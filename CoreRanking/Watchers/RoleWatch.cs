using CoreRanking.Data;
using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using PWToolKit;
using PWToolKit.API.Gamedbd;
using PWToolKit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Watchers
{
    public class RoleWatch
    {
        private static long lastSize;
        private static string path;
        private static ServerConnection server;
        private static Timer _RoleWatch;
        private static RankingDefinitions rankingDefs;
        public static async void Start(ServerConnection _pwServer, RankingDefinitions _rankingDefs)
        {
            rankingDefs = _rankingDefs;
            path = _pwServer.logsPath + "world2.formatlog";
            lastSize = await LoadLastSize("./Configurations/Internal/lastlog.size");
            server = _pwServer;

            PWGlobal.UsedPwVersion = server.PwVersion;

            _RoleWatch = new Timer(1000);
            _RoleWatch.Elapsed += RoleTick;
            _RoleWatch.Start();
        }

        private static async void RoleTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                long fileSize = await GetFileSize(path);

                if (fileSize > lastSize)
                {
                    await UploadRoleEvent(await ReadTail(path, UpdateLastFileSize(fileSize)));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }

        private static async Task<List<string>> ReadTail(string filename, long offset)
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
                logs = logs.Where(x => !x.Contains("\r")).Where(y => !string.IsNullOrEmpty(y)).ToList();

                GC.Collect();

                return logs;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static async Task DeleteRole(int roleId)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role RoleToRemove = db.Role.Where(x => x.RoleId.Equals(roleId)).FirstOrDefault();

                    if (RoleToRemove != null)
                    {
                        db.Role.Remove(RoleToRemove);

                        List<Battle> recordsToRemove = db.Battle.Where(x => x.KilledId.Equals(roleId)).ToList();
                        recordsToRemove.AddRange(db.Battle.Where(x => x.KillerId.Equals(roleId)).ToList());

                        db.Battle.RemoveRange(recordsToRemove);

                        await db.SaveChangesAsync();

                        LogWriter.Write($"O personagem {RoleToRemove.CharacterName} foi removido do Ranking, junto com os registros do seu PvP.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task RenameRole(int roleId)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role role = db.Role.Where(x => x.RoleId.Equals(roleId)).FirstOrDefault();

                    if (role != null)
                    {
                        string newName = GetRoleBase.Get(server.gamedbd, role.RoleId).Name;

                        role.CharacterName = newName;

                        await db.SaveChangesAsync();

                        LogWriter.Write($"O personagem {role.CharacterName} foi renomeado no Ranking.");
                    }
                    else
                    {
                        LogWriter.Write($"O personagem atual não existe no Ranking.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task LevelUpRole(int roleId, int curLevel)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role role = db.Role.Where(x => x.RoleId.Equals(roleId)).FirstOrDefault();

                    if (role != null)
                    {
                        role.Level = role.Level >= curLevel ? role.Level : curLevel;
                        role.LevelDate = DateTime.Now;

                        await db.SaveChangesAsync();

                        LogWriter.Write($"O personagem {role.CharacterName} upou para o nível {role.Level}.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }

        public static async Task CreateAccount(string ip, string login, int userId)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Account account = db.Account.Where(y => y.Id.Equals(userId)).FirstOrDefault();

                    if (account is null)
                    {
                        account = new Account
                        {
                            Id = userId,
                            Login = login,
                            Ip = ip
                        };

                        LogWriter.Write($"Conta {account.Id} criada. IP: {account.Ip} | Login: {account.Login}");

                        /*var accountRoles = GetUserRoles.Get(server.gamedbd, account.Id);

                        List<Role> rolesAccount = new List<Role>();
                        foreach (var role in accountRoles)
                        {
                            var currentRole = GetRoleData.Get(server.gdeliveryd, role.Item1);

                            rolesAccount.Add(new Role
                            {
                                AccountId = account.Id,
                                Account = account,
                                CharacterClass = currentRole.GRoleBase.Class.ToString(),
                                CharacterName = currentRole.GRoleBase.Name,
                                Level = currentRole.GRoleStatus.Level,
                                RoleId = currentRole.GRoleBase.Id
                            });

                            LogWriter.Write($"Personagem {currentRole.GRoleBase.Name}({currentRole.GRoleBase.Id}) foi adicionado ao ranking");
                        }

                        rolesAccount.ForEach(async x => await AddRoleIfNotExists(x));*/

                        await AddAccountIfNotExists(account);                        
                    }
                    else
                    {
                        account.Ip = ip;
                        account.Login = login;

                        LogWriter.Write($"A conta de login {login}({userId}) se conectou ao servidor com o IP {ip}");
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        public static async Task<Role> CreateRole(int roleId, ServerConnection pwServerConnection)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role roleData = null;

                    if (db.Role.Where(x => x.RoleId.Equals(roleId)).FirstOrDefault() == null)
                    {
                        roleData = new Role();

                        if (roleId >= 16)
                        {
                            GRoleData characterInfo = GetRoleData.Get(pwServerConnection.gamedbd, roleId);

                            if (characterInfo != null)
                            {
                                roleData.CharacterGender = characterInfo.GRoleBase.Gender == 0 ? "Male" : "Female";
                                roleData.Level = characterInfo.GRoleStatus.Level;
                                roleData.AccountId = characterInfo.GRoleBase.UserId;
                                roleData.RoleId = roleId;
                                roleData.CharacterClass = characterInfo.GRoleBase.Class.ToString();
                                roleData.CharacterName = characterInfo.GRoleBase.Name;

                                if (!db.Account.ToList().Select(x => x.Id).Contains(characterInfo.GRoleBase.UserId))
                                {
                                    await AddAccountIfNotExists(new Account
                                    {
                                        Id = characterInfo.GRoleBase.UserId
                                    });                                    
                                }

                                await AddRoleIfNotExists(roleData);
                                LogWriter.Write($"O personagem {roleData.CharacterName} foi incluído no Ranking.");
                            }                                                       
                        }                     
                    }

                    return roleData;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write($"Role ID ERROR: {roleId} => \n" + ex.ToString());
                return default;
            }
        }

        private static async Task UploadRoleEvent(List<string> logs)
        {
            try
            {
                foreach (var log in logs)
                {
                    int id = 0;

                    if (log.Contains("roleid="))
                        id = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"roleid=([0-9]*)").Value.Replace("roleid=", ""));

                    if (log.Contains("login:account"))
                    {
                        string _log = log.Replace(".", default);

                        string ip = System.Text.RegularExpressions.Regex.Match(_log, @"peer=([0-9]*)").Value.Replace("peer=", "").Trim();
                        string login = System.Text.RegularExpressions.Regex.Match(_log, @"account=[a-zA-Z0-9]*").Value.Replace("account=", "").Trim();
                        int userId = int.Parse(System.Text.RegularExpressions.Regex.Match(_log, @"userid=([0-9]*)").Value.Replace("userid=", ""));

                        await CreateAccount(ip, login, userId);
                    }
                    else if (log.Contains("deleterole"))
                    {
                        await DeleteRole(id);
                    }
                    else if (log.Contains("createrole-success"))
                    {
                        string _log = log.Replace(".", default);

                        string ip = System.Text.RegularExpressions.Regex.Match(_log, @"IP=([0-9]*)").Value.Replace("IP=", "").Trim();
                        string login = System.Text.RegularExpressions.Regex.Match(_log, @"account=[a-zA-Z0-9]*").Value.Replace("account=", "").Trim();
                        int userId = int.Parse(System.Text.RegularExpressions.Regex.Match(_log, @"userid=([0-9]*)").Value.Replace("userid=", ""));

                        await CreateAccount(ip, login, userId);
                        await CreateRole(id, server);
                    }
                    else if (log.Contains("dbplayerrename:"))
                    {
                        await RenameRole(id);
                    }
                    else if (log.Contains("formatlog:upgrade:roleid"))
                    {
                        int level = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"level=([0-9]*)").Value.Replace("level=", ""));
                        await LevelUpRole(id, level);
                    }
                    else if (log.Contains("formatlog:task") && log.Contains("msg=DeliverByAwardData: success = 1"))
                    {
                        int whichTask = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"taskid=([0-9]*)").Value.Replace("taskid=", "").Trim());

                        if (rankingDefs.QuestIdResetKDA.Equals(whichTask))
                        {
                            int whoCompleted = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"roleid=([0-9]*)").Value.Replace("roleid=", "").Trim());
                            await ResetKDA(whoCompleted);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static long UpdateLastFileSize(long fileSize)
        {
            long difference = fileSize - lastSize;
            lastSize = fileSize;

            return difference;
        }
        private static async Task<long> GetFileSize(string fileName)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("./Configurations/Internal/lastlog.size"))
                {
                    var fileSize = new System.IO.FileInfo(fileName).Length;

                    await sw.WriteAsync(fileSize.ToString());

                    return fileSize;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static async Task<long> LoadLastSize(string fileName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    long.TryParse((await sr.ReadLineAsync()).Trim(), out long result);

                    return result;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static async Task ResetKDA(int roleId)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role role = await db.Role.FindAsync(roleId);

                    role.Kill = 0;
                    role.Death = 0;

                    await db.SaveChangesAsync();

                    LogWriter.Write($"RoleWatch: {role.CharacterName}({role.RoleId}) acabou de zerar seu KDA por ter completado a missão {rankingDefs.QuestIdResetKDA}");
                    await WorldChatWatch.SendMessage($"&{role.CharacterName}& zerou seu KDA completando uma missão especial.", PWToolKit.Enums.BroadcastChannel.System, server);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        public static async Task AddRoleIfNotExists(Role role)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (db.Role.Where(x => x.RoleId.Equals(role.RoleId)).Count() <= 0)
                    {
                        db.Role.Add(role);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        public static async Task AddAccountIfNotExists(Account account)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (db.Account.Where(x => x.Id.Equals(account.Id)).Count() <= 0)
                    {
                        db.Account.Add(account);
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
