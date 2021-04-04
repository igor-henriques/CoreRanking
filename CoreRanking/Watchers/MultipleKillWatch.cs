using CoreRanking.Data;
using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using PWToolKit.API.GDeliveryd;
using PWToolKit.API.GProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Watchers
{
    public class MultipleKillWatch
    {
        static List<PlayerControl> ToRemove = new List<PlayerControl>();
        static List<PlayerControl> PlayerController = new List<PlayerControl>();

        static Timer playerVerifier = new Timer(250);

        public static MultipleKill MultipleKill;
        public static ServerConnection Server;

        public MultipleKillWatch(MultipleKill multipleKill, ServerConnection server)
        {
            Server = server;
            MultipleKill = multipleKill;

            playerVerifier.Elapsed += PlayerVerifier_Elapsed;

            if (MultipleKill.IsActive)
            {
                playerVerifier.Start();
            }            
        }

        private void PlayerVerifier_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (PlayerController.Count > 0)
                {
                    foreach (var player in PlayerController)
                    {
                        if (player.Kills <= 2 && player.Clock.ElapsedMilliseconds - 100 > MultipleKill.DoubleKill.Time)
                        {
                            if (player.Kills == 2)
                                PrivateChat.Send(Server.gdeliveryd, player.Role.RoleId, "SEQUÊNCIA DE MORTE FINALIZADA");

                            ToRemove.Add(player);
                        }
                        else if (player.Kills == 3 && player.Clock.ElapsedMilliseconds - 100 > MultipleKill.TripleKill.Time)
                        {
                            PrivateChat.Send(Server.gdeliveryd, player.Role.RoleId, "SEQUÊNCIA DE MORTE FINALIZADA");
                            ToRemove.Add(player);
                        }
                        else if (player.Kills == 4 && player.Clock.ElapsedMilliseconds - 100 > MultipleKill.QuadraKill.Time)
                        {
                            PrivateChat.Send(Server.gdeliveryd, player.Role.RoleId, "SEQUÊNCIA DE MORTE FINALIZADA");
                            ToRemove.Add(player);
                        }
                        else if (player.Kills >= 5 && player.Clock.ElapsedMilliseconds - 100 > MultipleKill.PentaKill.Time)
                        {
                            PrivateChat.Send(Server.gdeliveryd, player.Role.RoleId, "SEQUÊNCIA DE MORTE FINALIZADA");
                            ToRemove.Add(player);
                        }
                    }

                    PlayerController = PlayerController.Except(ToRemove).ToList();
                    ToRemove.Clear();
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }                        
        }

        public async static Task Trigger(Role Role)
        {
            try
            {
                if (MultipleKill.IsActive)
                {
                    if (Role is null)
                        return;

                    var player = PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault();

                    if (player is null)
                    {
                        PlayerController.Add(new PlayerControl
                        {
                            Clock = new Stopwatch(),
                            Role = Role,
                            Kills = 1
                        });

                        PlayerController.Last().Clock.Start();
                    }
                    else
                    {
                        if (player.Clock.ElapsedMilliseconds <= MultipleKill.DoubleKill.Time && player.Kills.Equals(1))
                        {
                            PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault().Kills += 1;

                            await Reward(MultipleKill.DoubleKill, Role, player, 2);
                        }
                        else if (player.Clock.ElapsedMilliseconds <= MultipleKill.TripleKill.Time && player.Kills.Equals(2))
                        {
                            PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault().Kills += 1;

                            await Reward(MultipleKill.TripleKill, Role, player, 3);
                        }
                        else if (player.Clock.ElapsedMilliseconds <= MultipleKill.QuadraKill.Time && player.Kills.Equals(3))
                        {
                            PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault().Kills += 1;

                            await Reward(MultipleKill.QuadraKill, Role, player, 4);
                        }
                        else if (player.Clock.ElapsedMilliseconds <= MultipleKill.PentaKill.Time && player.Kills >= 4)
                        {
                            PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault().Kills += 1;

                            await Reward(MultipleKill.PentaKill, Role, player, 5);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }                       
        }
        private async static Task Reward(dynamic Multiplier, Role Role, PlayerControl Controller, int kills)
        {
            try
            {
                if (MultipleKill.IsMessageAllowed)
                    ChatBroadcast.Send(Server.gprovider, MultipleKill.Channel, await BuildMessage(Multiplier.Messages, Role.CharacterName));

                using (var db = new ApplicationDbContext())
                {
                    var currentRole = db.Role.Where(x => x.Equals(Role)).FirstOrDefault();

                    if (currentRole != null)
                    {
                        currentRole.Points += Multiplier.Points;

                        if (Multiplier is DoubleKill)
                        {
                            currentRole.Doublekill += 1;
                            currentRole.Points += MultipleKill.DoubleKill.Points;

                            LogWriter.Write($"MultipleKill: {currentRole.CharacterName} fez Doublekill e ganhou {MultipleKill.DoubleKill.Points} pontos");
                        }
                        else if (Multiplier is TripleKill)
                        {
                            currentRole.Triplekill += 1;
                            currentRole.Points += MultipleKill.TripleKill.Points;

                            LogWriter.Write($"MultipleKill: {currentRole.CharacterName} fez Triplekill e ganhou {MultipleKill.TripleKill.Points} pontos");
                        }
                        else if (Multiplier is QuadraKill)
                        {
                            currentRole.Quadrakill += 1;
                            currentRole.Points += MultipleKill.QuadraKill.Points;

                            LogWriter.Write($"MultipleKill: {currentRole.CharacterName} fez Quadrakill e ganhou {MultipleKill.QuadraKill.Points} pontos");
                        }
                        else if (Multiplier is PentaKill)
                        {
                            currentRole.Pentakill += 1;
                            currentRole.Points += MultipleKill.PentaKill.Points;

                            LogWriter.Write($"MultipleKill: {currentRole.CharacterName} fez Pentakill e ganhou {MultipleKill.PentaKill.Points} pontos");
                        }
                    }

                    if (kills is 2)
                        PrivateChat.Send(Server.gdeliveryd, Role.RoleId, "SEQUÊNCIA DE MORTE INICIADA");

                    await db.SaveChangesAsync();
                }

                Controller.Clock.Restart();
                PlayerController.Where(x => x.Role.CharacterName.Equals(Role.CharacterName, StringComparison.Ordinal)).FirstOrDefault().Clock = Controller.Clock;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }           
        }

        private static async Task<string> BuildMessage(List<string> messages, string killer) => messages[new Random().Next(0, messages.Count - 1)].Replace("$killer", killer);
    }
}
