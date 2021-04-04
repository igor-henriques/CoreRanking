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
    class TransferWatch
    {
        static private long lastSize;
        private static string path;
        static ServerConnection pwServer;
        static System.Timers.Timer _ChatWatch;
        static RankingDefinitions prefs;
        static List<Transference> decodedMessages;

        public TransferWatch(ServerConnection _pwServer, RankingDefinitions _prefs)
        {
            prefs = _prefs;
            path = _pwServer.logsPath + "world2.chat";
            lastSize = GetFileSize(path).Result;
            pwServer = _pwServer;

            PWGlobal.UsedPwVersion = pwServer.PwVersion;

            _ChatWatch = new System.Timers.Timer(500);
            _ChatWatch.Elapsed += ChatTick;

            _ChatWatch.Start();
        }

        private async void ChatTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                long fileSize = await GetFileSize(path);

                if (fileSize > lastSize)
                {
                    decodedMessages = new List<Transference>();

                    decodedMessages.AddRange(await ReadTail(path, UpdateLastFileSize(fileSize), pwServer));

                    decodedMessages = decodedMessages.Where(x => x != null).ToList();
                    decodedMessages = decodedMessages.Where(x => x.idTo != 0).ToList();

                    if (prefs.isTrasferenceAllowed)
                    {
                        decodedMessages.ForEach(async x => await TransferPoints(x.idFrom, x.idTo, x.points));
                    }
                    else
                    {
                        decodedMessages.ForEach(x => PrivateChat.Send(pwServer.gdeliveryd, x.idFrom, "A transferência de pontos não está ativa."));
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task<bool> TransferPoints(int roleIdFrom, int roleIdTo, int points)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    Role roleFrom = db.Role.Where(x => x.RoleId.Equals(roleIdFrom)).FirstOrDefault();
                    if (roleFrom is null)
                    {
                        PrivateChat.Send(pwServer.gdeliveryd, roleIdFrom, "Você não está cadastrado(a) no ranking. Relogue sua conta para participar.");
                        return false;
                    }

                    Role roleTo = db.Role.Where(x => x.RoleId.Equals(roleIdTo)).FirstOrDefault();
                    if (roleTo is null)
                    {
                        PrivateChat.Send(pwServer.gdeliveryd, roleIdTo, "Você não está cadastrado(a) no ranking. Relogue sua conta para participar.");
                        return false;
                    }

                    if (roleTo.RoleId >= 1024)
                    {
                        if (points >= 1)
                        {
                            if (!roleFrom.Equals(roleTo))
                            {
                                if (roleFrom.Points >= points)
                                {
                                    roleTo.Points += points;
                                    roleFrom.Points -= points;
                                    await db.SaveChangesAsync();

                                    PrivateChat.Send(pwServer.gdeliveryd, roleTo.RoleId, $"{roleFrom.CharacterName} te enviou {points} pontos. Totalizam-te {roleTo.Points} pontos.");
                                    PrivateChat.Send(pwServer.gdeliveryd, roleFrom.RoleId, $"Você enviou {points} ponto(s) ao(à) jogador(a) {roleTo.CharacterName}. Totalizam-te {roleFrom.Points} pontos.");

                                    LogWriter.Write($"O personagem {roleFrom.CharacterName} enviou a {roleTo.CharacterName} {points} pontos. \n{roleTo.CharacterName}:{roleTo.Points}\n{roleFrom.CharacterName}:{roleFrom.Points}");

                                    return true;
                                }
                                else
                                {
                                    PrivateChat.Send(pwServer.gdeliveryd, roleFrom.RoleId, $"Você não tem pontos suficientes para realizar a transferência. Sua pontuação: {roleFrom.Points}. Pontuação necessária: {points}");
                                    LogWriter.Write($"O personagem {roleFrom.CharacterName} tentou enviar {points} a {roleTo.CharacterName}, mas não teve pontos suficientes.");
                                    return false;
                                }
                            }
                            else
                            {
                                PrivateChat.Send(pwServer.gdeliveryd, roleFrom.RoleId, "Você não pode transferir para você mesmo.");
                                LogWriter.Write($"{roleFrom.CharacterName} tentou enviar {points} a ele mesmo.");
                                return false;
                            }
                        }
                        else
                        {
                            PrivateChat.Send(pwServer.gdeliveryd, roleFrom.RoleId, $"Não é possível realizar a transferência do valor digitado. Use valores acima de 0.");
                            LogWriter.Write($"O personagem {roleFrom.CharacterName} tentou enviar transferir valores negativos.");
                            return false;
                        }
                    }
                    else
                    {
                        PrivateChat.Send(pwServer.gdeliveryd, roleFrom.RoleId, $"O jogador {roleTo.CharacterName} não está no ranking.");
                        LogWriter.Write($"{points} pontos tentaram ser transferido ao jogador {roleTo.CharacterName}, que não existe no ranking.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static async Task<List<Transference>> ReadTail(string filename, long offset, ServerConnection pwServerConnection)
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

                List<string> logs = Encoding.Default.GetString(bytes).Split(new string[] { "\n" }[0]).Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();
                List<Transference> decodedTransferences = new List<Transference>();

                GC.Collect();

                foreach (var log in logs)
                {
                    decodedTransferences.Add(await DecodeMessage(log));
                }

                return decodedTransferences;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }

        private static long UpdateLastFileSize(long fileSize)
        {
            long difference = fileSize - lastSize;
            lastSize = fileSize;

            return difference;
        }

        private static async Task<long> GetFileSize(string fileName)
        {
            return new System.IO.FileInfo(fileName).Length;
        }
        private static async Task<Transference> DecodeMessage(string encodedMessage)
        {
            try
            {
                Transference transf = new Transference();

                string message = Encoding.Unicode.GetString(Convert.FromBase64String(System.Text.RegularExpressions.Regex.Match(encodedMessage, @"msg=([\s\S]*)").Value.Replace("msg=", "")));

                if (message.Contains("!transferir") && !message.Contains("src=-1"))
                {
                    transf.idFrom = int.Parse(System.Text.RegularExpressions.Regex.Match(encodedMessage, @"src=([0-9]*)").Value.Replace("src=", "").Trim());
                    transf.idTo = 0;

                    message = message.Replace("!transferir", default).Trim();

                    if (message.Any(char.IsDigit) && message.Length > 0)
                    {
                        string pointString = System.Text.RegularExpressions.Regex.Match(message, @" \d+").Value.Trim();
                        message = System.Text.RegularExpressions.Regex.Replace(message, @" \d+", "");

                        transf.points = pointString.Length > 7 ? 9999999 : int.Parse(pointString);
                    }
                    else
                    {
                        transf.points = 1;
                    }

                    transf.idTo = GetRoleId.Get(pwServer.gamedbd, message.Trim());
                }
                else if (message.Trim().ToLower().Equals("!participar"))
                {
                    using (var db = new ApplicationDbContext())
                    {
                        int id = int.Parse(System.Text.RegularExpressions.Regex.Match(encodedMessage, @"src=([0-9]*)").Value.Replace("src=", "").Trim());

                        if (db.Role.Where(x => x.RoleId.Equals(id)).FirstOrDefault() is null)
                        {
                            Role response = await RoleWatch.CreateRole(id, pwServer);

                            if (response == null)
                            {
                                PrivateChat.Send(pwServer.gdeliveryd, id, "Houve um erro no seu cadastro. Entre em contato com a administração.");
                            }
                            else
                            {
                                PrivateChat.Send(pwServer.gdeliveryd, id, "Você está participando do ranking! Digite ! help para receber a lista de comandos disponíveis.");
                            }
                        }
                        else
                        {
                            PrivateChat.Send(pwServer.gdeliveryd, id, "Você já está participando do ranking. Digite ! help para receber a lista de comandos disponíveis.");
                        }
                    }
                }

                return transf;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
    }
}
