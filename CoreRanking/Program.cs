using CoreRanking.Model;
using System;
using System.IO;
using System.Threading.Tasks;
using PWToolKit;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using CoreRanking.Model.ItensExchange;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using PWToolKit.Enums;
using PWToolKit.API.Gamedbd;
using CoreRanking.Model.RankingPvE;
using Microsoft.EntityFrameworkCore;
using CoreRanking.Watchers;
using CoreRanking.Model.Server;
using CoreRanking.License;
using CoreRanking.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CoreRanking.Repositories;
using Microsoft.Extensions.Logging;
using PWToolKit.API.GDeliveryd;
using System.Linq;

namespace CoreRanking
{
    public class Program
    {
        private readonly ILogger<Program> logger;

        static ManualResetEvent quitEvent = new ManualResetEvent(false);
        static MultipleKillWatch MultipleKillWatch;
        static ServerConnection ServerConnection;
        static RankingDefinitions rankingDefs;
        static PveConfiguration pveConfiguration;
        static FirewallWatch CoreRankingFirewall;
        static LicenseControl license = new LicenseControl();
        static List<ItemAward> ItemsReward;
        static List<ClassPointConfig> classPointConfig;
        static Elo elo;

        public Program(ILogger<Program> logger)
        {
            this.logger = logger;
        }

        static async Task Main()
        {
            try
            {
                var host = CreateHostBuilder().Build();
                await host.Services.GetRequiredService<Program>().Run();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            Stop();
        }
        async Task Run()
        {
            await InitializePrefs();

            PvPWatch.Start(ServerConnection, rankingDefs, classPointConfig);
            RoleWatch.Start(ServerConnection, rankingDefs);
        }
        static async Task InitializePrefs()
        {
            Console.WriteLine("CHECANDO PROCESSOS EXISTENTES\n");
            await CheckProcess();

            Console.WriteLine("CHECANDO VERSÃO ATUAL\n");
            //A IMPLEMENTAR

            Console.WriteLine("INICIALIZANDO SISTEMA DE LICENÇA\n");
            CoreLicense licenseConfigs = JsonConvert.DeserializeObject<CoreLicense>(await File.ReadAllTextAsync("./Configurations/License.json"));
            await license.Start(licenseConfigs.User, licenseConfigs.Licensekey, licenseConfigs.Product);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE SERVIDOR\n");
            JObject jsonServerConfig = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/ServerConnection.json"));
            ServerConnection = await LoadServerConfig(jsonServerConfig);

            Console.WriteLine("INICIALIZANDO SISTEMA DE PONTUAÇÕES\n");
            JObject jsonPointsConfig = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PointsConfiguration.json"));
            classPointConfig = await LoadClassesPoints(jsonPointsConfig);

            Console.WriteLine("INICIALIZANDO ITENS DE RECOMPENSA\n");
            JObject jsonItemsReward = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/ItensAward.json"));
            ItemsReward = await LoadItemsAward(jsonItemsReward);

            Console.WriteLine("INICIALIZANDO SISTEMA DE MULTIPLE-KILL\n");
            JObject jsonMultipleKill = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/MultipleKill.json"));
            MultipleKillWatch = new MultipleKillWatch(await LoadMultipleKillConfig(jsonMultipleKill), ServerConnection);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE ELO\n");
            elo = JsonConvert.DeserializeObject<Elo>(await File.ReadAllTextAsync("./Configurations/EloConfiguration.json"));

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE USUÁRIO\n");
            JObject jsonrankingDefs = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/RankingDefinitions.json"));
            rankingDefs = await LoadPvPPreferences(jsonrankingDefs);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE PVE\n");
            JObject jsonPvePoints = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PvePoints.json"));
            pveConfiguration = await LoadPvEPreferences(jsonPvePoints);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE FIREWALL\n");
            FirewallDefinitions defs = JsonConvert.DeserializeObject<FirewallDefinitions>(await File.ReadAllTextAsync("./Configurations/Firewall.json"));
            CoreRankingFirewall = new FirewallWatch(defs, ServerConnection);

            if (Convert.ToBoolean(await File.ReadAllTextAsync("./Configurations/UpdateLevel.conf")))
            {
                Console.WriteLine("ATUALIZANDO NÍVEIS DE PERSONAGENS\n");
                await UpdateRoleLevel();
            }

            Console.WriteLine("INICIALIZANDO HOOK DE CHAT\n");
            WorldChatWatch worldChat = new WorldChatWatch(ServerConnection, rankingDefs);

            Console.WriteLine("INICIALIZANDO SISTEMA DE TRANSFERÊNCIA DE PONTOS\n");
            TransferWatch transferenceSystem = new TransferWatch(ServerConnection, rankingDefs);

            Console.WriteLine("INICIALIZANDO SISTEMA DE RANKING PVE\n");
            await PvEWatcher.Start(ServerConnection, pveConfiguration);

            await EnsureOnlineRoleCreation();

            Console.WriteLine("MÓDULOS INICIALIZADOS COM SUCESSO\n\n\n");

            Console.WriteLine("PROGRAMADO POR IRONSIDE\nBOM USO!\nDiscord para Report Bug: Ironside#3862\n=============================================================");
        }
        private static async Task EnsureOnlineRoleCreation()
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (await db.Role.CountAsync() <= 0)
                    {
                        LogWriter.Write("CADASTRANDO JOGADORES ONLINE");

                        List<Account> usersAccounts = new List<Account>();
                        List<Role> users = new List<Role>();

                        var onlineUsers = GMListOnlineUser.Get(ServerConnection.gdeliveryd);

                        foreach (var user in onlineUsers)
                        {
                            var extraInfo = GetRoleData.Get(ServerConnection.gamedbd, user.RoleId);

                            usersAccounts.Add(new Account
                            {
                                Id = user.UserId,
                            });

                            users.Add(new Role
                            {
                                AccountId = user.UserId,
                                RoleId = user.RoleId,
                                CharacterClass = extraInfo.GRoleBase.Class.ToString(),
                                CharacterName = extraInfo.GRoleBase.Name,
                                CharacterGender = extraInfo.GRoleBase.Gender == 0 ? "Male" : "Female",
                                Level = extraInfo.GRoleStatus.Level
                            });

                            LogWriter.Write($"Jogador {extraInfo.GRoleBase.Name}(RoleID: {extraInfo.GRoleBase.Id} | AccountID: {extraInfo.GRoleBase.UserId}) inserido no Ranking. Nível: {extraInfo.GRoleStatus.Level}");
                            PrivateChat.Send(ServerConnection.gdeliveryd, extraInfo.GRoleBase.Id, "Você foi inserido no Core Ranking!");
                        }

                        await db.Role.AddRangeAsync(users);
                        await db.Account.AddRangeAsync(usersAccounts);

                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task UpdateRoleLevel()
        {
            using (var db = new ApplicationDbContext())
            {
                PWGlobal.UsedPwVersion = ServerConnection.PwVersion;
                List<Role> ToRemove = new List<Role>();
                Console.WriteLine("\n\n");

                var roles = await db.Role.ToListAsync();

                foreach (var role in roles)
                {
                    var currentRole = GetRoleData.Get(ServerConnection.gamedbd, role.RoleId);

                    if (currentRole != null)
                    {
                        if (currentRole.GRoleStatus.Level > role.Level)
                        {
                            Console.WriteLine($"Nível mais atualizado disponível para o personagem {role.CharacterName}. \nLevel atualizado: {currentRole.GRoleStatus.Level}\nLevel atual: {role.Level}");

                            role.Level = currentRole.GRoleStatus.Level;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Personagem {role.CharacterName} existe no registro do ranking, mas não no registro do jogo. Marcando para exclusão.");
                        ToRemove.Add(role);
                    }
                }

                if (roles.Count <= 0)
                    Console.WriteLine("Não foi encontrado nenhum personagem que seja necessário atualizar o nível.");

                Console.WriteLine("===========================================================================");

                foreach (var role in ToRemove)
                {
                    string answer = default;

                    while (answer != "Y" && answer != "N")
                    {
                        Console.WriteLine($"\nDeseja remover o personagem {role.CharacterName}(ID: {role.RoleId}) da base do ranking? Y/N");
                        answer = Console.ReadLine().Trim().ToUpper();

                        if (answer.Equals("Y"))
                        {
                            db.Role.Remove(role);

                            Console.WriteLine("Personagem removido.");
                        }
                    }
                }

                Console.WriteLine("\n");

                await db.SaveChangesAsync();
            }
        }
        static async Task CheckProcess()
        {
            Process p = Process.GetCurrentProcess();
            var ProcessesList = Process.GetProcessesByName(p.ProcessName);

            for (int i = 0; i < ProcessesList.Length - 1; i++)
            {
                if (!ProcessesList[i].Equals(p))
                {
                    ProcessesList[i].Kill();
                    LogWriter.Write("ELIMINANDO PROCESSO PRÉ-EXISTENTE");
                }
            }
        }
        static async Task<ServerConnection> LoadServerConfig(JObject jsonNodes)
        {
            ServerConnection ServerConnection = new ServerConnection
            (
                jsonNodes["GAMEDBD"]["HOST"].ToObject<string>(),
                jsonNodes["GAMEDBD"]["PORT"].ToObject<int>(),
                jsonNodes["GPROVIDER"]["HOST"].ToObject<string>(),
                jsonNodes["GPROVIDER"]["PORT"].ToObject<int>(),
                jsonNodes["GDELIVERYD"]["HOST"].ToObject<string>(),
                jsonNodes["GDELIVERYD"]["PORT"].ToObject<int>(),
                (PwVersion)jsonNodes["PW_VERSION"].ToObject<int>(),
                jsonNodes["LOGS_PATH"].ToObject<string>()
            );

            return ServerConnection;
        }

        static async Task<MultipleKill> LoadMultipleKillConfig(JObject jsonNodes)
        {
            List<List<string>> messages = new List<List<string>>();
            string[] multiplicadores = new string[] { "DOUBLEKILL", "TRIPLEKILL", "QUADRAKILL", "PENTAKILL" };

            foreach (var multiplicador in multiplicadores)
            {
                List<string> messagePerMultiplier = new List<string>();

                foreach (var package in jsonNodes[multiplicador]["MENSAGENS"].Children())
                {
                    messagePerMultiplier.Add(package.First.ToString());
                }

                messages.Add(messagePerMultiplier);
            }

            MultipleKill MultipleKill = new MultipleKill()
            {
                IsActive = jsonNodes["ATIVO"].ToObject<bool>(),
                IsMessageAllowed = jsonNodes["MENSAGEM INGAME"].ToObject<bool>(),
                Channel = (BroadcastChannel)jsonNodes["CANAL"].ToObject<int>(),

                DoubleKill = new DoubleKill
                {
                    Time = jsonNodes["DOUBLEKILL"]["TEMPO"].ToObject<double>(),
                    Points = jsonNodes["DOUBLEKILL"]["PONTOS"].ToObject<int>(),
                    Messages = messages[0]
                },

                TripleKill = new TripleKill
                {
                    Time = jsonNodes["TRIPLEKILL"]["TEMPO"].ToObject<double>(),
                    Points = jsonNodes["TRIPLEKILL"]["PONTOS"].ToObject<int>(),
                    Messages = messages[1]
                },

                QuadraKill = new QuadraKill
                {
                    Time = jsonNodes["QUADRAKILL"]["TEMPO"].ToObject<double>(),
                    Points = jsonNodes["QUADRAKILL"]["PONTOS"].ToObject<int>(),
                    Messages = messages[2]
                },

                PentaKill = new PentaKill
                {
                    Time = jsonNodes["PENTAKILL"]["TEMPO"].ToObject<double>(),
                    Points = jsonNodes["PENTAKILL"]["PONTOS"].ToObject<int>(),
                    Messages = messages[3]
                }
            };

            return MultipleKill;
        }
        static async Task<List<ClassPointConfig>> LoadClassesPoints(dynamic jsonNodes)
        {
            List<ClassPointConfig> classesPointsConfig = new List<ClassPointConfig>();
            string[] classes = new string[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "PSY", "SK", "MS", "RT", "TM" };

            foreach (var classe in classes)
            {
                ClassPointConfig newClassPointConfig = new ClassPointConfig();

                newClassPointConfig.Ocuppation = classe;
                newClassPointConfig.onKill = jsonNodes[classe]["onKill"];
                newClassPointConfig.onDeath = jsonNodes[classe]["onDeath"];

                classesPointsConfig.Add(newClassPointConfig);
            }

            return classesPointsConfig;
        }
        static async Task<RankingDefinitions> LoadPvPPreferences(JObject jsonNodes)
        {
            List<string> messages = new List<string>();
            foreach (var package in jsonNodes["MENSAGENS"].Children())
            {
                messages.Add(package.First.ToString());
            }

            RankingDefinitions prefs = new RankingDefinitions
            (
                jsonNodes["AVISO DE MENSAGEM"].ToObject<bool>(),
                jsonNodes["CANAL"].ToObject<int>(),
                ItemsReward,
                jsonNodes["GOLD POR KILL"].ToObject<int>(),
                messages.ToArray(),
                jsonNodes["TRANSFERÊNCIA DE PONTOS"].ToObject<bool>(),
                elo,
                jsonNodes["TOLERANCIA DE LEVEL"].ToObject<int>(),
                jsonNodes["TOLERANCIA DE PONTO"].ToObject<int>(),
                jsonNodes["MOSTRAR KDA"].ToObject<bool>(),
                jsonNodes["ATIVAR TRIGGERS"].ToObject<bool>(),
                jsonNodes["LIMITE MINIMO DE PONTOS"].ToObject<int>(),
                jsonNodes["QUANTIDADE DE JOGADORES NO TOPRANK"].ToObject<int>(),
                jsonNodes["COR DA MENSAGEM"].ToObject<string>(),
                new List<int>(),
                jsonNodes["ID DA MISSAO QUE RESETA KDA"].ToObject<int>()
            );

            return prefs;
        }
        static async Task<PveConfiguration> LoadPvEPreferences(JObject jsonNodes)
        {
            PveConfiguration prefs = new PveConfiguration
            (
                jsonNodes["ATIVO"].ToObject<bool>(),
                jsonNodes["PONTOS POR ERVAS COLETADAS"].ToObject<double>(),
                jsonNodes["PONTOS POR MATERIAIS COLETADOS"].ToObject<double>()
            );

            return prefs;
        }
        static async Task<List<ItemAward>> LoadItemsAward(JObject jsonNodes)
        {
            List<ItemAward> itemsAward = new List<ItemAward>();

            foreach (var item in jsonNodes)
            {
                itemsAward.Add(new ItemAward
                {
                    Id = int.Parse(item.Key),
                    Name = item.Value["NOME"].ToObject<string>(),
                    Cost = item.Value["CUSTO"].ToObject<int>(),
                    Stack = item.Value["STACK"].ToObject<int>()
                });
            }

            return itemsAward;
        }
        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddTransient<Program>();
                    services.AddTransient<ApplicationDbContext>();
                    services.AddTransient<IBattleRepository, BattleRepository>();
                    services.AddTransient<IRoleRepository, RoleRepository>();
                    services.AddTransient<IAccountRepository, AccountRepository>();
                    services.AddTransient<IHuntRepository, HuntRepository>();
                    services.AddTransient<ICollectRepository, CollectRepository>();
                    services.AddTransient<IBannedRepository, BannedRepository>();
                });
        }
        static void Stop()
        {
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                quitEvent.Set();
                eArgs.Cancel = true;
            };

            quitEvent.WaitOne();
        }
    }
}
