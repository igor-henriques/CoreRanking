using CoreRanking.Model;
using CoreRanking.Data;
using CoreRanking.Model.ItensExchange;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using CoreRanking.Model.RankingPvE;
using Microsoft.EntityFrameworkCore;
using PWToolKit;
using PWToolKit.API.Gamedbd;
using PWToolKit.API.GDeliveryd;
using PWToolKit.API.GProvider;
using PWToolKit.Enums;
using PWToolKit.Hooks;
using PWToolKit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreRanking.Model.Data;

namespace CoreRanking.Watchers
{
    public class WorldChatWatch
    {
        private static ServerConnection server;
        private static WorldChatHook chatHook;
        private static RankingDefinitions rankingDefs;
        private DateTime lastTopRank;
        private DateTime lastTopLevel;
        private static Dictionary<string, string> translateClassName = new Dictionary<string, string>();
        List<Role> GMRoles;

        public WorldChatWatch(ServerConnection _server, RankingDefinitions _rankingDefs)
        {
            try
            {
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
                translateClassName.Add("Reaper", "TM");
                translateClassName.Add("Ghost", "RT");

                PWGlobal.UsedPwVersion = (PwVersion)_server.PwVersion;
                lastTopRank = new DateTime(1990, 1, 1);
                lastTopLevel = new DateTime(1990, 1, 1);
                rankingDefs = _rankingDefs;
                server = _server;

                GMRoles = LoadGMsRoles(File.ReadAllLinesAsync("./Configurations/GMAccounts.conf").Result.Select(int.Parse).ToList()).Result;

                chatHook = new WorldChatHook();
                chatHook.OnNewMessage += HookMessage;

                Connect();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        public void Connect()
        {
            chatHook.Connect(server.gdeliveryd);
        }

        private async void HookMessage(PWToolKit.Models.WorldChatMessage message)
        {
            if (rankingDefs.isTriggerAllowed)
            {
                if (message.Text.Trim().Contains("!ponto"))
                {
                    await _GetPoints(message);
                }
                else if (message.Text.Trim().Contains("!kill"))
                {
                    await _GetKill(message);
                }
                else if (message.Text.Trim().Contains("!kda"))
                {
                    await _GetKDA(message);
                }
                else if (message.Text.Trim().Contains("!atividade"))
                {
                    await GetActivity(message);
                }
                else if (message.Text.Trim().Contains("!toprank level"))
                {
                    await GetTopLevel(message);
                }
                else if (message.Text.Trim().Contains("!toprank"))
                {
                    await GetTopPvP(message);
                }
                else if (message.Text.Trim().Contains("!reward"))
                {
                    await GetReward(message);
                }
                else if (message.Text.Trim().Contains("!itens"))
                {
                    await SendItemsAvailable(message.RoleID);
                }
                else if (message.Text.Trim().Contains("!help"))
                {
                    await SendHelpMessages(message);
                }
            }
        }
        private async Task _GetPoints(WorldChatMessage message)
        {
            try
            {
                string roleName = message.Text.Replace("!ponto", default).Trim();
                roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

                using (var db = new ApplicationDbContext())
                {
                    Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (roleToDisplay != null)
                    {
                        await SendMessage($"{roleToDisplay.CharacterName} possui {await GetPoints(roleName)} pontos.", rankingDefs.Channel, server);
                    }
                    else
                    {
                        if (roleName.Equals("s"))
                        {
                            PrivateChat.Send(server.gdeliveryd, message.RoleID, $"Talvez você tenha digitado o comando errado. Tente !ponto. Sem 's'");
                        }
                        else
                        {
                            PrivateChat.Send(server.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        private async Task _GetKill(WorldChatMessage message)
        {
            try
            {
                string roleName = message.Text.Replace("!kill", default).Trim();
                roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

                using (var db = new ApplicationDbContext())
                {
                    Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (roleToDisplay != null)
                    {
                        await SendMessage($"{roleToDisplay.CharacterName} possui {roleToDisplay.Kill} kills.", rankingDefs.Channel, server);
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        private async Task _GetKDA(WorldChatMessage message)
        {
            try
            {
                string roleName = message.Text.Replace("!kda", default).Trim();
                roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

                using (var db = new ApplicationDbContext())
                {
                    Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (roleToDisplay != null)
                    {
                        await SendMessage($"{roleToDisplay.CharacterName} possui {(await GetKDA(roleName)).ToString("0.00")} KDA.", rankingDefs.Channel, server);
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        private async Task GetActivity(WorldChatMessage message)
        {
            try
            {
                string roleName = message.Text.Replace("!atividade", default).Trim();
                roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

                using (var db = new ApplicationDbContext())
                {
                    Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (roleToDisplay != null)
                    {
                        double interacoes = await GetInteractions(roleName);
                        double kills = roleToDisplay.Kill;

                        await SendMessage($"{roleToDisplay.CharacterName} possui {interacoes} participações no PVP. {kills} Kills; {interacoes - kills} Mortes.", rankingDefs.Channel, server);
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        private async Task GetTopLevel(WorldChatMessage message)
        {
            try
            {
                int requesterRoleId = message.RoleID;
                string classe = message.Text.Replace("!toprank level", default).Trim();
                double cooldown = DateTime.Now.Subtract(lastTopLevel).TotalSeconds;

                if (lastTopLevel.Year.Equals(1990) || cooldown > 30)
                {
                    if (classe.Length > 0 && string.IsNullOrEmpty(ConvertClassToGameStructure(classe)))
                    {
                        PrivateChat.Send(server.gdeliveryd, requesterRoleId, $"A classe {classe} não existe.");
                    }
                    else
                    {
                        await GetLevelPodium(classe);
                        lastTopLevel = DateTime.Now;
                    }
                }
                else
                {
                    PrivateChat.Send(server.gdeliveryd, requesterRoleId, $"O pedido está em tempo de espera. Tente novamente em {((cooldown - 30) * -1).ToString("0")} segundos.");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }


        }
        private async Task GetTopPvP(WorldChatMessage message)
        {
            try
            {
                int requesterRoleId = message.RoleID;
                double cooldown = DateTime.Now.Subtract(lastTopRank).TotalSeconds;

                if (lastTopRank.Year.Equals(1990) || cooldown > 30)
                {
                    string classe = message.Text.Replace("!toprank", default).Trim();

                    List<Role> topPlayers = await GetPodium(classe);

                    if (topPlayers.Count > 0)
                    {
                        topPlayers.ForEach(async x => await SendMessage($"{topPlayers.IndexOf(x) + 1}º lugar: {x.CharacterName}. Kills {x.Kill}", rankingDefs.Channel, server));
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, requesterRoleId, "Ainda não há jogadores suficientes para compor o ranking especificado.");
                    }

                    lastTopRank = DateTime.Now;
                }
                else
                {
                    PrivateChat.Send(server.gdeliveryd, requesterRoleId, $"O pedido está em tempo de espera. Tente novamente em {((cooldown - 30) * -1).ToString("0")} segundos.");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private async Task SendHelpMessages(WorldChatMessage message)
        {
            try
            {
                PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !ponto serve para mostrar quantos pontos algum char tem. Exemplo: !ponto Ironside");
                PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !kill serve para mostrar quantas kills algum char tem. Exemplo: !kill Ironside");
                PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !atividade serve para mostrar quantas participações no pvp algum char tem. Exemplo: !atividade Ironside");
                PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !kda mostra sua relação de kill sobre morte. Quanto mais alto, melhor. Exemplo: !kda Ironside");
                PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !toprank serve para mostrar quantas kills os primeiros do ranking tem. É possível filtrar por classe e level. Exemplos: !toprank ou !toprank wr ou !toprank level");

                if (rankingDefs.ItemsReward.Count >= 1)
                {
                    PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !reward serve para resgatar seus pontos por algum item.");
                    PrivateChat.Send(server.gdeliveryd, message.RoleID, "O comando !itens serve para mostrar todos os itens elegíveis para trocar por pontos.");
                }

                if (rankingDefs.isTrasferenceAllowed)
                {
                    PrivateChat.Send(server.gdeliveryd, message.RoleID, $"O comando transferir serve para transferir seus pontos para algum personagem. Exemplo: ! transferir Ironside 10.");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }
        private async Task GetReward(WorldChatMessage message)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (rankingDefs.ItemsReward.Count > 0)
                    {
                        string sentence = message.Text.Trim().Replace("!reward", default).Trim();
                        int amount = sentence.Any(char.IsDigit) ? int.Parse(System.Text.RegularExpressions.Regex.Match(sentence, @"\d+").Value) : 1;
                        sentence = sentence.Replace(amount.ToString(), default).Trim();

                        if (rankingDefs.ItemsReward.Select(x => x.Name).Contains(sentence.Trim()))
                        {
                            if (amount <= 99999)
                            {
                                ItemAward itemChoosed = rankingDefs.ItemsReward.Where(x => x.Name.Contains(sentence.Trim())).FirstOrDefault();

                                Role currentUser = db.Role.Where(x => x.RoleId.Equals(message.RoleID)).FirstOrDefault();

                                if (currentUser is null)
                                {
                                    PrivateChat.Send(server.gdeliveryd, message.RoleID, "Você não está cadastrado(a) no ranking. Relogue sua conta para participar.");
                                }

                                int Points = currentUser.Points;

                                if (Points >= itemChoosed.Cost * amount)
                                {
                                    GRoleInventory item = new GRoleInventory()
                                    {
                                        Id = itemChoosed.Id,
                                        MaxCount = 99999,
                                        Pos = GetRolePocket.Get(server.gamedbd, currentUser.RoleId).Items.Length + 1,
                                        Proctype = 0
                                    };

                                    if (itemChoosed.Stack > amount)
                                    {
                                        item.Count = amount;

                                        SysSendMail.Send(server.gdeliveryd, currentUser.RoleId, "RECOMPENSA DE PVP", "Sua recompensa!", item);
                                    }
                                    else
                                    {
                                        item.Count = 1;

                                        for (int i = 0; i < amount; i++)
                                        {
                                            SysSendMail.Send(server.gdeliveryd, currentUser.RoleId, "RECOMPENSA DE PVP", "Sua recompensa!", item);
                                        }
                                    }

                                    currentUser.Points -= itemChoosed.Cost * amount;
                                    PrivateChat.Send(server.gdeliveryd, currentUser.RoleId, $"Sua recompensa foi entregue. Em sua Caixa de Correios deve haver {amount}x {itemChoosed.Name}({itemChoosed.Cost * amount} pontos). Te restam {currentUser.Points} pontos.");

                                    await db.SaveChangesAsync();
                                }
                                else
                                {
                                    PrivateChat.Send(server.gdeliveryd, message.RoleID, @$"Você não tem pontos suficientes para resgatar ""{sentence}"". Necessita de {itemChoosed.Cost * amount} ponto(s).");
                                }
                            }
                            else
                            {
                                PrivateChat.Send(server.gdeliveryd, message.RoleID, $"Não extrapole os limites.");
                            }
                        }
                        else
                        {
                            string displayMssage = sentence.Length <= 0 ? "É necessário especificar o nome do item a ser recebido" : @$"O item ""{sentence}"" não está elegível para recompensa.";
                            PrivateChat.Send(server.gdeliveryd, message.RoleID, displayMssage + ". Itens disponíveis:");
                            await SendItemsAvailable(message.RoleID);
                        }
                    }
                    else
                    {
                        PrivateChat.Send(server.gdeliveryd, message.RoleID, "Não há itens disponíveis para recompensa.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private async Task SendItemsAvailable(int RoleId)
        {
            try
            {
                foreach (var item in rankingDefs.ItemsReward)
                {
                    PrivateChat.Send(server.gdeliveryd, RoleId, $"Item: {item.Name}. Custo: {item.Cost} ponto(s).");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        public static async Task<double> GetKDA(string roleName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    double kda = 0;

                    Role role = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (role != null)
                    {
                        double kills = role.Kill;
                        double deaths = role.Death;

                        if (kills is 0)
                            kda = 0;

                        if (deaths is 0 && kills > 0)
                            kda = kills;

                        if (kills > 0 && deaths > 0)
                            kda = kills / deaths;
                    }

                    return kda;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private async Task<double> GetInteractions(string roleName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    int kills = 0, deaths = 0;

                    Role role = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (role != null)
                    {
                        kills = role.Kill;
                        deaths = role.Death;
                    }

                    return kills + deaths;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private async Task<int> GetPoints(string roleName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    int Points = 0;

                    Role requestRole = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                    if (requestRole != null)
                    {
                        if (requestRole.RoleId > 0)
                            Points = db.Role.Where(x => x.RoleId.Equals(requestRole.RoleId)).Select(x => x.Points).FirstOrDefault();
                    }

                    return Points;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }



            return default;
        }
        private async Task<List<Role>> GetPodium(string classe)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    List<Role> topPlayers = new List<Role>();
                    string convertedClass = ConvertClassToGameStructure(classe);

                    if (!string.IsNullOrEmpty(convertedClass))
                    {
                        topPlayers = await db.Role.
                            Where(x => x.CharacterClass.ToUpper().Equals(convertedClass.ToUpper())).
                            OrderByDescending(x => x.Kill).
                            Take(rankingDefs.AmountPlayersOnPodium).
                            ToListAsync();

                    }
                    else
                    {
                        topPlayers = await db.Role.
                            OrderByDescending(x => x.Kill).
                            Take(rankingDefs.AmountPlayersOnPodium).
                            ToListAsync();
                    }

                    return topPlayers.Where(x => x.Kill > 0).ToList();
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private async Task GetLevelPodium(string classe)
        {
            try
            {
                List<Role> roles;

                using (var db = new ApplicationDbContext())
                {
                    await EnsureGMLevelDown(GMRoles);

                    //Se alguma informação de classe foi providenciada
                    if (!string.IsNullOrEmpty(classe))
                    {                        
                        var listByClass = await db.Role.
                            Where(x => x.CharacterClass.Equals(ConvertClassToGameStructure(classe), StringComparison.Ordinal)).
                            OrderByDescending(y => y.Level).ThenByDescending(y => y.LevelDate).Take(rankingDefs.AmountPlayersOnPodium).ToListAsync();

                        roles = listByClass;
                    }
                    else
                    {
                        roles = await db.Role.OrderByDescending(x => x.Level).ThenByDescending(y => y.LevelDate).Take(rankingDefs.AmountPlayersOnPodium).ToListAsync();                        
                    }

                    roles = roles.Where(x => x.Level > 0).ToList();

                    roles.ForEach(async x => await SendMessage($"{roles.IndexOf(x) + 1}º lugar: {x.CharacterName}. Nível {x.Level}", rankingDefs.Channel, server));

                    LogWriter.Write($"'!toprank {classe}' utilizado");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        static async Task<List<Role>> LoadGMsRoles(List<int> GMUsers)
        {
            List<Role> gmRoles = new List<Role>();
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    foreach (var user in GMUsers)
                    {
                        List<int> gmRolesId = GetUserRoles.Get(server.gamedbd, user).Select(x => x.Item1).ToList();

                        foreach (var gmRoleId in gmRolesId)
                        {
                            gmRoles.Add(db.Role.Where(x => x.RoleId.Equals(gmRoleId)).FirstOrDefault());

                            LogWriter.Write($"Char GM {gmRoleId} carregado");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return gmRoles.Where(x => x != null).ToList();
        }

        /// <summary>
        /// Traduz as iniciais de cada classe para o nome original da classe, utilizado na estrutura do jogo. Ex.: EP = Priest
        /// </summary>
        /// <param name="classInitials">Sigla que representa a classe</param>
        /// <returns></returns>
        public static string ConvertClassToGameStructure(string classInitials)
        {
            try
            {
                return translateClassName.Where(x => x.Value.ToUpper().Equals(classInitials.Trim().ToUpper())).Select(y => y.Key).FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        /// <summary>
        /// Traduz o nome de cada classe utilizada na estrutura do jogo para o conhecido comumente.
        /// </summary>
        /// <param name="classFullName">Nome inteiro da classe</param>
        /// <returns></returns>
        public static string ConvertClassFromGameStructure(string classFullName)
        {
            try
            {
                return translateClassName.Where(x => x.Key.ToUpper().Equals(classFullName.Trim().ToUpper())).Select(y => y.Value).FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }

        public static async Task EnsureGMLevelDown(List<Role> GMs)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    List<Role> gmRoles = await db.Role.Where(x => GMs.Select(y => y.RoleId).Contains(x.RoleId)).ToListAsync();

                    gmRoles.ForEach(x => x.Level = 0);

                    await db.SaveChangesAsync();

                    LogWriter.Write("Foi atribuído nível 0 a todos personagens de GMs no ranking.");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }

        public static async Task SendMessage(string message, BroadcastChannel channel, ServerConnection server) => ChatBroadcast.Send(server.gprovider, channel, $"{(channel.Equals(BroadcastChannel.System) ? rankingDefs.MessageColor : default)}{message}");
    }
}
