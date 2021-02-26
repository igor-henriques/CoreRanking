using CoreRanking.Model.ItensExchange;
using CoreRanking.Model.Watchers;
using CoreRanking.Models;
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
using System.Linq;
using System.Threading.Tasks;

namespace CoreRanking.Model.PwServer
{
    public class WorldChatWatch
    {
        private static PwServerConnection pwServerConnection;
        private static List<ClassPointConfig> classesPointsConfig;
        private static WorldChatHook chatHook;
        private static PvPPrefs PvPprefs;
        private DateTime lastTopRank;
        private DateTime lastTopLevel;
        private DateTime lastPoint;
        private static Dictionary<string, string> translateClassName = new Dictionary<string, string>();

        public WorldChatWatch(PwServerConnection _pwServerConnection, List<ClassPointConfig> _classesPointsConfig, PvPPrefs _pvpPrefs)
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

            PWGlobal.UsedPwVersion = (PwVersion)_pwServerConnection.PwVersion;
            lastTopRank = new DateTime(1990, 1, 1);
            lastTopLevel = new DateTime(1990, 1, 1);
            lastPoint = new DateTime(1990, 1, 1);
            PvPprefs = _pvpPrefs;
            pwServerConnection = _pwServerConnection;
            classesPointsConfig = _classesPointsConfig;

            chatHook = new WorldChatHook();
            chatHook.OnNewMessage += HookMessage;

            Connect();
        }
        public void Connect()
        {
            chatHook.Connect(pwServerConnection.gdeliveryd);
        }

        private async void HookMessage(PWToolKit.Models.WorldChatMessage message)
        {
            if (PvPprefs.isTriggerAllowed)
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
            string roleName = message.Text.Replace("!ponto", default).Trim();
            roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

            using (var db = new ApplicationDbContext())
            {
                Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                if (roleToDisplay != null)
                {
                    await SendMessage($"{roleToDisplay.CharacterName} possui {await GetPoints(roleName)} pontos.", PvPprefs.Channel, pwServerConnection);
                }
                else
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                }
            }
        }
        private async Task _GetKill(WorldChatMessage message)
        {
            string roleName = message.Text.Replace("!kill", default).Trim();
            roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

            using (var db = new ApplicationDbContext())
            {
                Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                if (roleToDisplay != null)
                {
                    await SendMessage($"{roleToDisplay.CharacterName} possui {await GetKills(roleName)} kills.", PvPprefs.Channel, pwServerConnection);
                }
                else
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                }
            }
        }
        private async Task _GetKDA(WorldChatMessage message)
        {
            string roleName = message.Text.Replace("!kda", default).Trim();
            roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

            using (var db = new ApplicationDbContext())
            {
                Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                if (roleToDisplay != null)
                {
                    await SendMessage($"{roleToDisplay.CharacterName} possui {(await GetKDA(roleName)).ToString("0.00")} KDA.", PvPprefs.Channel, pwServerConnection);
                }
                else
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                }
            }
        }
        private async Task GetActivity(WorldChatMessage message)
        {
            string roleName = message.Text.Replace("!atividade", default).Trim();
            roleName = string.IsNullOrEmpty(roleName) ? message.RoleName : roleName;

            using (var db = new ApplicationDbContext())
            {
                Role roleToDisplay = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).FirstOrDefault();

                if (roleToDisplay != null)
                {
                    double interacoes = await GetInteractions(roleName);
                    double kills = await GetKills(roleToDisplay.CharacterName);

                    await SendMessage($"{roleToDisplay.CharacterName} possui {interacoes} participações no PVP. {kills} Kills; {interacoes - kills} Mortes.", PvPprefs.Channel, pwServerConnection);
                }
                else
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"O personagem {roleName} não existe no ranking.");
                }
            }
        }
        private async Task GetTopLevel(WorldChatMessage message)
        {
            int requesterRoleId = message.RoleID;
            string classe = message.Text.Replace("!toprank level", default).Trim();
            double cooldown = DateTime.Now.Subtract(lastTopLevel).TotalSeconds;

            if (lastTopLevel.Year.Equals(1990) || cooldown > 30)
            {
                if (classe.Length > 0 && string.IsNullOrEmpty(ConvertClass(classe.ToUpper())))
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, requesterRoleId, $"A classe {classe} não existe.");
                }
                else
                {
                    await GetLevelPodium(classe);
                    lastTopLevel = DateTime.Now;
                }
            }
            else
            {
                PrivateChat.Send(pwServerConnection.gdeliveryd, requesterRoleId, $"O pedido está em tempo de espera. Tente novamente em {((cooldown - 30) * -1).ToString("0")} segundos.");
            }
        }
        private async Task GetTopPvP(WorldChatMessage message)
        {
            int requesterRoleId = message.RoleID;
            double cooldown = DateTime.Now.Subtract(lastTopRank).TotalSeconds;

            if (lastTopRank.Year.Equals(1990) || cooldown > 30)
            {
                string classe = message.Text.Replace("!toprank", default).Trim();

                PodiumPVP podio = await GetPodium(classe);

                if (podio.thirdPlace != null)
                {
                    await SendMessage($"Primeiro lugar: {podio.firstPlace.CharacterName}. Kills: {podio.firstPlaceKills}", PvPprefs.Channel, pwServerConnection);
                    await SendMessage($"Segundo lugar: {podio.secondPlace.CharacterName}. Kills: {podio.secondPlaceKills}", PvPprefs.Channel, pwServerConnection);
                    await SendMessage($"Terceiro lugar: {podio.thirdPlace.CharacterName}. Kills: {podio.thirdPlaceKills}", PvPprefs.Channel, pwServerConnection);
                }
                else
                {
                    PrivateChat.Send(pwServerConnection.gdeliveryd, requesterRoleId, "Ainda não há 3 players no ranking especificado.");
                }

                lastTopRank = DateTime.Now;
            }
            else
            {
                PrivateChat.Send(pwServerConnection.gdeliveryd, requesterRoleId, $"O pedido está em tempo de espera. Tente novamente em {((cooldown - 30) * -1).ToString("0")} segundos.");
            }
        }
        private async Task SendHelpMessages(WorldChatMessage message)
        {
            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !ponto serve para mostrar quantos pontos algum char tem. Exemplo: !ponto Ironside");
            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !kill serve para mostrar quantas kills algum char tem. Exemplo: !kill Ironside");
            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !atividade serve para mostrar quantas participações no pvp algum char tem. Exemplo: !atividade Ironside");
            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !kda mostra sua relação de kill sobre morte. Quanto mais alto, melhor. Exemplo: !kda Ironside");
            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !toprank serve para mostrar quantas kills os primeiros do ranking tem. É possível filtrar por classe e level. Exemplos: !toprank ou !toprank wr ou !toprank level");

            if (PvPprefs.ItemsReward.Count >= 1)
            {
                PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !reward serve para resgatar seus pontos por algum item.");
                PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "O comando !itens serve para mostrar todos os itens elegíveis para trocar por pontos.");
            }

            if (PvPprefs.isTrasferenceAllowed)
            {
                PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"O comando transferir serve para transferir seus pontos para algum personagem. Exemplo: ! transferir Ironside 10.");
            }
        }
        private async Task GetReward(WorldChatMessage message)
        {
            using (var db = new ApplicationDbContext())
            {
                string sentence = message.Text.Trim().Replace("!reward", default).Trim();
                int amount = sentence.Any(char.IsDigit) ? int.Parse(System.Text.RegularExpressions.Regex.Match(sentence, @"\d+").Value) : 1;
                sentence = sentence.Replace(amount.ToString(), default).Trim();

                if (PvPprefs.ItemsReward.Select(x => x.Name).Contains(sentence.Trim()))
                {
                    if (amount <= 99999)
                    {
                        ItemAward itemChoosed = PvPprefs.ItemsReward.Where(x => x.Name.Contains(sentence.Trim())).FirstOrDefault();

                        Role currentUser = db.Role.Where(x => x.RoleId.Equals(message.RoleID)).FirstOrDefault();

                        if (currentUser is null)
                        {
                            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, "Você não está cadastrado(a) no ranking. Relogue sua conta para participar.");
                        }

                        int Points = currentUser.Points;

                        if (Points >= itemChoosed.Cost * amount)
                        {
                            GRoleInventory item = new GRoleInventory()
                            {
                                Id = itemChoosed.Id,
                                MaxCount = 99999,
                                Pos = GetRolePocket.Get(pwServerConnection.gamedbd, currentUser.RoleId).Items.Length + 1,
                                Proctype = 0
                            };

                            if (itemChoosed.Stack > amount)
                            {
                                item.Count = amount;

                                SysSendMail.Send(pwServerConnection.gdeliveryd, currentUser.RoleId, "RECOMPENSA DE PVP", "Sua recompensa!", item);
                            }
                            else
                            {
                                item.Count = 1;

                                for (int i = 0; i < amount; i++)
                                {
                                    SysSendMail.Send(pwServerConnection.gdeliveryd, currentUser.RoleId, "RECOMPENSA DE PVP", "Sua recompensa!", item);
                                }
                            }

                            currentUser.Points -= itemChoosed.Cost * amount;
                            PrivateChat.Send(pwServerConnection.gdeliveryd, currentUser.RoleId, $"Sua recompensa foi entregue. Em sua Caixa de Correios deve haver {amount}x {itemChoosed.Name}({itemChoosed.Cost * amount} pontos). Te restam {currentUser.Points} pontos.");

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, @$"Você não tem pontos suficientes para resgatar ""{sentence}"". Necessita de {itemChoosed.Cost * amount} ponto(s).");
                        }
                    }
                    else
                    {
                        PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, $"Não extrapole os limites.");
                    }
                }
                else
                {
                    string displayMssage = sentence.Length <= 0 ? "É necessário especificar o nome do item a ser recebido" : @$"O item ""{sentence}"" não está elegível para recompensa.";
                    PrivateChat.Send(pwServerConnection.gdeliveryd, message.RoleID, displayMssage + ". Itens disponíveis:");
                    await SendItemsAvailable(message.RoleID);
                }
            }
        }
        private async Task SendItemsAvailable(int RoleId)
        {
            foreach (var item in PvPprefs.ItemsReward)
            {
                PrivateChat.Send(pwServerConnection.gdeliveryd, RoleId, $"Item: {item.Name}. Custo: {item.Cost} ponto(s).");
            }
        }
        private async Task<int> GetKills(string roleName)
        {
            using (var db = new ApplicationDbContext())
            {
                int RoleId = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).Select(x => x.RoleId).FirstOrDefault();
                return await db.Battle.Select(x => x.killerId).Where(x => x.Equals(RoleId)).CountAsync();
            }
        }
        private async Task<int> GetDeaths(string roleName)
        {
            using (var db = new ApplicationDbContext())
            {
                int RoleId = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).Select(x => x.RoleId).FirstOrDefault();
                return await db.Battle.Select(x => x.killedId).Where(x => x.Equals(RoleId)).CountAsync();
            }
        }
        public static async Task<double> GetKDA(string roleName)
        {
            using (var db = new ApplicationDbContext())
            {
                double kda = 0;
                int RoleId = 0;

                RoleId = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).Select(x => x.RoleId).FirstOrDefault();

                if (RoleId != 0)
                {
                    double kills = await db.Battle.Where(x => x.killerId.Equals(RoleId)).CountAsync();
                    double deaths = await db.Battle.Where(x => x.killedId.Equals(RoleId)).CountAsync();

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
        private async Task<double> GetInteractions(string roleName)
        {
            using (var db = new ApplicationDbContext())
            {
                int RoleId = db.Role.Where(x => x.CharacterName.Equals(roleName, StringComparison.Ordinal)).Select(x => x.RoleId).FirstOrDefault();

                int kills = await db.Battle.Select(x => x.killerId).Where(x => x.Equals(RoleId)).CountAsync();
                int deaths = await db.Battle.Select(x => x.killedId).Where(x => x.Equals(RoleId)).CountAsync();

                return kills + deaths;
            }
        }
        private async Task<int> GetPoints(string roleName)
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
        private async Task<PodiumPVP> GetPodium(string classe)
        {
            using (var db = new ApplicationDbContext())
            {
                string convertedClass = string.Empty;
                PodiumPVP podio = new PodiumPVP();
                List<Battle> allBattles = await db.Battle.ToListAsync();
                List<Battle> specificClassPodio = new List<Battle>();
                List<int> classesId = null;

                if (translateClassName.Select(x => x.Value).Contains(classe.Trim().ToUpper()) || string.IsNullOrEmpty(classe.Trim()))
                {
                    if (!string.IsNullOrEmpty(classe))
                    {
                        convertedClass = ConvertClass(classe.ToUpper());

                        classesId = convertedClass.Length > 0 ? allBattles.Select(x => x.killerId).Where(battle => db.Role.Where(x => x.CharacterClass.Equals(convertedClass)).Select(y => y.RoleId).ToList().Contains(battle)).Distinct().ToList() : null;
                        specificClassPodio = allBattles.Where(x => classesId.Contains(x.killerId)).ToList();
                    }

                    Dictionary<int, int> pvpRecords = classesId is null ? GetFrequencies(allBattles.Select(x => x.killerId).ToList()) : GetFrequencies(specificClassPodio.Select(x => x.killerId).ToList());
                    var places = pvpRecords.GroupBy(grp => grp.Value).OrderByDescending(x => x.Key).Take(3).SelectMany(s => s).ToList();

                    if (places.Count >= 3)
                    {
                        podio.firstPlace = db.Role.Where(x => x.RoleId.Equals(places[0].Key)).FirstOrDefault();
                        podio.secondPlace = db.Role.Where(x => x.RoleId.Equals(places[1].Key)).FirstOrDefault();
                        podio.thirdPlace = db.Role.Where(x => x.RoleId.Equals(places[2].Key)).FirstOrDefault();

                        podio.firstPlaceKills = places[0].Value;
                        podio.secondPlaceKills = places[1].Value;
                        podio.thirdPlaceKills = places[2].Value;
                    }

                }

                return podio;
            }
        }
        private async Task GetLevelPodium(string classe)
        {
            List<Role> Roles;

            using (var db = new ApplicationDbContext())
            {
                if (!string.IsNullOrEmpty(classe))
                {
                    var listByClass = await db.Role.Where(x => x.CharacterClass.Equals(ConvertClass(classe.ToUpper()))).ToListAsync();

                    Roles = listByClass.OrderByDescending(x => x.Level).Take(5).ToList();

                }
                else
                {
                    Roles = await db.Role.OrderByDescending(x => x.Level).Take(5).ToListAsync();
                }

                Roles.ForEach(async x => await SendMessage($"{(Roles.IndexOf(x) + 1)}º lugar: {x.CharacterName}. Nível {x.Level}", PvPprefs.Channel, pwServerConnection));
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

        public static string ConvertClass(string classInitials)
        {
            return translateClassName.Where(x => x.Value.Equals(classInitials)).Select(y => y.Key).FirstOrDefault();
        }

        public static async Task SendMessage(string message, BroadcastChannel channel, PwServerConnection pwServerConnection) => ChatBroadcast.Send(pwServerConnection.gprovider, channel, message);
    }
}
