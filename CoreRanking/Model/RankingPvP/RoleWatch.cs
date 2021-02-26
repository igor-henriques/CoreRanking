using CoreRanking.Model.Data;
using CoreRanking.Models;
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

namespace CoreRanking.Model
{
    public class RoleWatch
    {
        static private long lastSize;
        private static string path;
        static PwServerConnection pwServer;
        static Timer _RoleWatch;
        static LogWriter newLog;
        public static async void Start(PwServerConnection _pwServer)
        {
            path = _pwServer.logsPath + "world2.formatlog";
            lastSize = await GetFileSize(path); ;
            pwServer = _pwServer;

            PWGlobal.UsedPwVersion = pwServer.PwVersion;

            _RoleWatch = new Timer(500);
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
                    List<Role> roleData = new List<Role>();
                    roleData.AddRange(await ReadTail(path, UpdateLastFileSize(fileSize), pwServer));

                    roleData = roleData.Where(x => x != null).ToList();

                    roleData.ForEach(async x => await UploadRoleEvent(x, pwServer));
                }
            }
            catch (Exception ex)
            {
                newLog = new LogWriter(ex.ToString());
            }            
        }

        public static async Task<List<Role>> ReadTail(string filename, long offset, PwServerConnection pwServerConnection)
        {
            byte[] bytes;

            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(offset * -1, SeekOrigin.End);

                bytes = new byte[offset];
                fs.Read(bytes, 0, (int)offset);
            }

            List<string> logs = Encoding.Default.GetString(bytes).Split(new string[] { "\n" }[0]).ToList();
            List<Role> battleResponse = new List<Role>();

            GC.Collect();

            foreach (var log in logs)
            {
                battleResponse.Add(await GatherRoleData(log, pwServerConnection));
            }

            return battleResponse;
        }
        private static async Task<Role> GatherRoleData(string log, PwServerConnection pwServerConnection)
        {
            int level = 0;
            string evento = log.Contains("deleterole") ? "deleted" : log.Contains("createrole-success:") ? "created" : default;

            if (log.Contains("login:account"))
            {
                await CreateAccount(log);
            }

            if (log.Contains("deleterole"))
            {
                evento = "deleted";
            }

            if (log.Contains("createrole-success"))
            {
                evento = "created";
            }

            if (log.Contains("dbplayerrename:"))
            {
                evento = "renamed";
            }

            if (log.Contains("formatlog:upgrade:roleid"))
            {
                evento = "upgrade";
                level = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"level=([0-9]*)").Value.Replace("level=", ""));
            }

            if (!string.IsNullOrEmpty(evento))
            {
                int id = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"roleid=([0-9]*)").Value.Replace("roleid=", ""));
                
                return CharacterInfo(id, evento, pwServerConnection, level);
            }

            return null;
        }
        public static async Task CreateAccount(string log)
        {
            try
            {
                log = log.Replace(".", default);

                string ip = System.Text.RegularExpressions.Regex.Match(log, @"peer=([0-9]*)").Value.Replace("peer=", "").Trim();
                string login = System.Text.RegularExpressions.Regex.Match(log, @"account=[a-zA-Z0-9]*").Value.Replace("account=", "").Trim();
                int userId = int.Parse(System.Text.RegularExpressions.Regex.Match(log, @"userid=([0-9]*)").Value.Replace("userid=", ""));


                using (var db = new ApplicationDbContext())
                {
                    Account account = db.Account.Where(x => x.Login.ToLower().Equals(login, StringComparison.Ordinal)).Where(y => y.Id.Equals(userId)).FirstOrDefault();

                    if (account is null)
                    {
                        account = new Account
                        {
                            Id = userId,
                            Login = login,
                            Ip = ip
                        };

                        await db.Account.AddAsync(account);
                    }
                    else
                    {
                        account.Ip = ip;
                        account.Login = login;
                    }

                    newLog = new LogWriter($"A conta de login {login}({userId}) se conectou ao servidor com o IP {ip}");

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                newLog = new LogWriter(ex.ToString());
            }            
        }
        public static Role CharacterInfo(int roleId, string evento, PwServerConnection pwServerConnection, int level)
        {
            try
            {
                Role roleData = new Role();

                if (roleId >= 16)
                {
                    GRoleData characterInfo = GetRoleData.Get(pwServerConnection.gamedbd, roleId);

                    roleData.CharacterGender = characterInfo.GRoleBase.Gender == 0 ? "Male" : "Female";
                    roleData.Level = level <= 0 ? characterInfo.GRoleStatus.Level : level;
                    roleData.AccountId = characterInfo.GRoleBase.UserId;
                    roleData.RoleId = roleId;
                    roleData.CharacterClass = evento.Equals("deleted") ? default : characterInfo.GRoleBase.Class.ToString();
                    roleData.CharacterName = evento.Equals("deleted") ? default : characterInfo.GRoleBase.Name;
                    roleData.Evento = evento;

                    using (var db = new ApplicationDbContext())
                    {
                        if (db.Account.Where(x => x.Id.Equals(roleData.AccountId)).FirstOrDefault() == null)
                        {
                            Account newAcount = new Account();
                            newAcount.Id = roleData.AccountId;
                            newAcount.Login = "";
                            newAcount.Ip = null;
                        }
                    }
                }

                return roleData;
            }
            catch (Exception ex)
            {
                newLog = new LogWriter($"Role ID ERROR: {roleId} => " + ex.ToString());

                return null;
            }            
        }

        public static async Task UploadRoleEvent(Role roleData, PwServerConnection pwServerConnection)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (roleData.Evento.Equals("created"))
                    {
                        if (db.Role.Where(x => x.RoleId.Equals(roleData.RoleId)).FirstOrDefault() == null)
                        {
                            db.Role.Add(roleData);
                            newLog = new LogWriter($"O personagem {roleData.CharacterName} foi incluído no Ranking.");
                        }
                    }
                    else if (roleData.Evento.Equals("deleted"))
                    {
                        Role RoleToRemove = db.Role.Where(x => x.RoleId.Equals(roleData.RoleId)).FirstOrDefault();

                        if (RoleToRemove != null)
                            db.Role.Remove(RoleToRemove);

                        List<Battle> recordsToRemove = db.Battle.Where(x => x.killedId.Equals(roleData.RoleId)).ToList();
                        recordsToRemove.AddRange(db.Battle.Where(x => x.killerId.Equals(roleData.RoleId)).ToList());

                        db.Battle.RemoveRange(recordsToRemove);
                        
                        newLog = new LogWriter($"O personagem {roleData.CharacterName} foi removido do Ranking, junto com os registros do seu PvP.");

                    }
                    else if (roleData.Evento.Equals("renamed"))
                    {
                        var role = db.Role.Where(x => x.RoleId.Equals(roleData.RoleId)).FirstOrDefault();

                        if (role != null)
                        {
                            string newName = GetRoleBase.Get(pwServerConnection.gamedbd, roleData.RoleId).Name;

                            role.CharacterName = role.CharacterName != newName ? newName : role.CharacterName;
                            newLog = new LogWriter($"O personagem {roleData.CharacterName} foi renomeado no Ranking.");
                        }
                        else
                        {
                            newLog = new LogWriter($"O personagem atual não existe no Ranking.");
                        }
                    }
                    else if (roleData.Evento.Equals("upgrade"))
                    {
                        var role = db.Role.Where(x => x.RoleId.Equals(roleData.RoleId)).FirstOrDefault();
                        role.Level = role != null ? roleData.Level : 0;
                        newLog = new LogWriter($"O personagem {roleData.CharacterName} upou para o nível {role.Level}.");
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                newLog = new LogWriter(ex.ToString());
            }            
        }
        public static async Task<Role> AddRole(Role role)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.Role.Contains(role))
                {
                    await db.Role.AddAsync(role);
                }

                await db.SaveChangesAsync();

                return role;
            }
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
