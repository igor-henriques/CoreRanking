using CoreRanking.Model;
using System;
using System.IO;
using System.Threading.Tasks;
using PWToolKit;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using CoreRanking.Model.PwServer;
using Newtonsoft.Json.Linq;
using CoreRanking.Model.RankingFirewall;
using System.Diagnostics;
using CoreRanking.Model.ItensExchange;
using CoreRanking.Model.PointsTransference;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.RankingPvP.MultiplesKill;
using PWToolKit.Enums;
using CoreRanking.Models;
using System.Linq;
using PWToolKit.API.Gamedbd;
using CoreRanking.Model.RankingPvE;

namespace CoreRanking
{
    class Program
    {
        static ManualResetEvent quitEvent = new ManualResetEvent(false);
        static MultipleKillWatch MultipleKillWatch;
        static PwServerConnection pwServerConnection;
        static PvPPrefs userPrefs;
        static PveConfiguration pveConfiguration;
        static CoreRankingFirewall CoreRankingFirewall;
        static LicenseControl license;
        static List<ItemAward> ItemsReward;
        static List<ClassPointConfig> classPointConfig;
        static LogWriter newLog;
        static Elo elo;

        static async Task Main()
        {
            await InitializePrefs();

            Console.WriteLine("PROGRAMADO POR IRONSIDE\nBOM USO!\nDiscord para Report Bug: Ironside#3862\n=============================================================");

            try
            {
                await Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                newLog = new LogWriter(ex.ToString());
            }

            Stop();
        }

        static async Task InitializePrefs()
        {
            Console.WriteLine("CHECANDO PROCESSO EXISTENTE\n");
            await CheckProcess();

            Console.WriteLine("CHECANDO VERSÃO ATUAL\n");

            Console.WriteLine("INICIALIZANDO SISTEMA DE LICENÇA\n");
            CoreLicense licenseConfigs = JsonConvert.DeserializeObject<CoreLicense>(await File.ReadAllTextAsync("./Configurations/License.json"));
            license = new LicenseControl(licenseConfigs.user, licenseConfigs.licensekey);            

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE SERVIDOR\n");
            JObject jsonServerConfig = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PwServerConnection.json"));
            pwServerConnection = await LoadServerConfig(jsonServerConfig);

            Console.WriteLine("INICIALIZANDO SISTEMA DE PONTUAÇÕES\n");
            JObject jsonPointsConfig = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PointsConfiguration.json"));
            classPointConfig = await LoadClassesPoints(jsonPointsConfig);

            Console.WriteLine("INICIALIZANDO ITENS DE RECOMPENSA\n");
            JObject jsonItemsReward = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/ItensAward.json"));
            ItemsReward = await LoadItemsAward(jsonItemsReward);

            Console.WriteLine("INICIALIZANDO SISTEMA DE MULTIPLE-KILL\n");
            JObject jsonMultipleKill = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/MultipleKill.json"));
            MultipleKillWatch = new MultipleKillWatch(await LoadMultipleKillConfig(jsonMultipleKill), pwServerConnection);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE ELO\n");
            elo = JsonConvert.DeserializeObject<Elo>(await File.ReadAllTextAsync("./Configurations/EloConfiguration.json"));

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE USUÁRIO\n");
            JObject jsonUserPrefs = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PvPChat.json"));
            userPrefs = await LoadPvPPreferences(jsonUserPrefs);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE PVE\n");
            JObject jsonPvePoints = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync("./Configurations/PvePoints.json"));
            pveConfiguration = await LoadPvEPreferences(jsonPvePoints);

            Console.WriteLine("INICIALIZANDO CONFIGURAÇÕES DE FIREWALL\n");
            FirewallDefinitions defs = JsonConvert.DeserializeObject<FirewallDefinitions>(await File.ReadAllTextAsync("./Configurations/Firewall.json"));
            CoreRankingFirewall = new CoreRankingFirewall(defs, pwServerConnection);

            if (Convert.ToBoolean(await File.ReadAllTextAsync("./Configurations/UpdateLevel.conf")))
            {
                Console.WriteLine("ATUALIZANDO NÍVEIS DE PERSONAGENS\n");
                await UpdateRoleLevel();
            }

            Console.WriteLine("INICIALIZANDO HOOK DE CHAT\n");
            WorldChatWatch worldChat = new WorldChatWatch(pwServerConnection, classPointConfig, userPrefs);

            Console.WriteLine("INICIALIZANDO SISTEMA DE TRANSFERÊNCIA DE PONTOS\n");
            TransferWatch transferenceSystem = new TransferWatch(pwServerConnection, userPrefs);

            Console.WriteLine("INICIALIZANDO SISTEMA DE RANKING PVE\n");
            await PvEWatcher.Start(pwServerConnection, pveConfiguration);

            Console.WriteLine("MÓDULOS INICIALIZADOS COM SUCESSO\n\n\n");
            newLog = new LogWriter("TODOS OS MÓDULOS FORAM INICIALIZADOS COM SUCESSO");
        }
        static async Task UpdateRoleLevel()
        {
            using (var db = new ApplicationDbContext())
            {
                bool updatedNeeded = false;
                PWGlobal.UsedPwVersion = pwServerConnection.PwVersion;
                List<Role> ToRemove = new List<Role>();
                Console.WriteLine("\n\n");

                foreach (var role in db.Role.ToList())
                {                       
                    var currentRole = GetRoleData.Get(pwServerConnection.gamedbd, role.RoleId);

                    if (currentRole != null)
                    {
                        if (currentRole.GRoleStatus.Level > role.Level)
                        {
                            role.Level = currentRole.GRoleStatus.Level;
                            updatedNeeded = true;
                            Console.WriteLine($"Nível mais atualizado disponível para o personagem {role.CharacterName}. \nLevel atualizado: {currentRole.GRoleStatus.Level}\nLevel atual: {role.Level}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Personagem {role.CharacterName} existe no registro do ranking, mas não no registro do jogo. Marcando para exclusão.");
                        ToRemove.Add(role);                        
                    }
                }

                Console.WriteLine("Não foi encontrado nenhum personagem que seja necessário atualizar o nível.");
                Console.WriteLine("===========================================================================");

                foreach (var role in ToRemove)
                {                                        
                    string answer = default;

                    while(answer != "Y" && answer != "N")
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
                    newLog = new LogWriter("ELIMINANDO PROCESSO PRÉ-EXISTENTE");
                }
            }
        }
        static async Task Run()
        {
            PvPWatch.Start(pwServerConnection, userPrefs, classPointConfig);
            RoleWatch.Start(pwServerConnection);
        }
        static async Task<PwServerConnection> LoadServerConfig(JObject jsonNodes)
        {
            PwServerConnection pwServerConnection = new PwServerConnection
            (
                jsonNodes["GAMEDBD"]["HOST"].ToObject<string>(),
                jsonNodes["GAMEDBD"]["PORT"].ToObject<int>(),
                jsonNodes["GPROVIDER"]["HOST"].ToObject<string>(),
                jsonNodes["GPROVIDER"]["PORT"].ToObject<int>(),
                jsonNodes["GDELIVERYD"]["HOST"].ToObject<string>(),
                jsonNodes["GDELIVERYD"]["PORT"].ToObject<int>(),
                (PwVersion)jsonNodes["PW_VERSION"].ToObject<int>(),
                jsonNodes["ROOT_PATH"].ToObject<string>(),
                jsonNodes["LOGS_PATH"].ToObject<string>()
            );

            return pwServerConnection;
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
        static async Task<PvPPrefs> LoadPvPPreferences(JObject jsonNodes)
        {
            List<string> messages = new List<string>();
            foreach (var package in jsonNodes["MENSAGENS"].Children())
            {
                messages.Add(package.First.ToString());
            }

            PvPPrefs prefs = new PvPPrefs
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
                jsonNodes["ATIVAR TRIGGERS"].ToObject<bool>()
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
