﻿using CoreRanking.Data;
using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvE;
using CoreRanking.Model.RankingPvP;
using CoreRanking.Model.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CoreRanking.Watchers
{
    public class PvEWatcher
    {
        static private long lastSize;
        private static string path;
        static Timer PvEWatch;
        static ServerConnection Server;
        static Random rdm = new Random();
        static PveConfiguration pveConfiguration;

        public static async Task Start(ServerConnection server, PveConfiguration _pveConfiguration)
        {
            pveConfiguration = _pveConfiguration;
            Server = server;
            path = server.logsPath + "world2.log";
            lastSize = GetFileSize(path).Result;

            PvEWatch = new Timer(1000);
            PvEWatch.Elapsed += PvEWatch_Elapsed;

            if (pveConfiguration.isActive)
                PvEWatch.Start();
        }

        private static async void PvEWatch_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                long fileSize = await GetFileSize(path);

                if (fileSize > lastSize)
                {
                    List<PVEInfo> generalInfo = new List<PVEInfo>();

                    generalInfo.AddRange(await ReadTail(path, UpdateLastFileSize(fileSize), Server));

                    generalInfo = generalInfo.Where(x => x != null).ToList();

                    generalInfo.ForEach(async x => await UploadPvEvent(x));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task UploadPvEvent(PVEInfo info)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    if (info.Hunt != null)
                    {
                        db.Hunt.AddRange(info.Hunt);
                        await db.SaveChangesAsync();
                    }
                    else if (info.Collect != null)
                    {
                        Role currentRole = db.Role.Where(x => x.RoleId.Equals(info.Collect.First().RoleId)).FirstOrDefault();

                        //RoleWatch.CreateAccount(info.Collect.First().RoleId());

                        if (currentRole != null)
                        {
                            foreach (var item in info.Collect)
                            {
                                //........................................sequência de id de ervas
                                currentRole.CollectPoint += item.ItemId >= 1820 && item.Id <= 1875 ? pveConfiguration.herbPoint : pveConfiguration.mineralPoint;                                

                                db.Collect.Add(item);
                            }

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            LogWriter.Write($"O personagem {info.Collect.First().RoleId} coletou {info.Collect.Count}x {info.Collect.First().ItemId}, mas não está cadastrado no ranking.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static async Task<List<PVEInfo>> ReadTail(string filename, long offset, ServerConnection pwServerConnection)
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

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                List<string> logs = GB2312ToUtf8(bytes).Split(new string[] { "\n" }[0]).Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

                GC.Collect();

                List<PVEInfo> genInfo = new List<PVEInfo>();

                foreach (var log in logs)
                {
                    genInfo.Add(await DecodeMessage(log));
                }

                return genInfo;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
        private static string GB2312ToUtf8(byte[] gb2312bytes)
        {
            Encoding fromEncoding = Encoding.GetEncoding("GB2312");
            Encoding toEncoding = Encoding.UTF8;
            return EncodingConvert(gb2312bytes, fromEncoding, toEncoding);
        }
        private static string EncodingConvert(byte[] fromBytes, Encoding fromEncoding, Encoding toEncoding)
        {
            byte[] toBytes = Encoding.Convert(fromEncoding, toEncoding, fromBytes);

            string toString = toEncoding.GetString(toBytes);
            return toString;
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
        private static async Task<PVEInfo> DecodeMessage(string encodedMessage)
        {
            try
            {
                PVEInfo newInfo = new PVEInfo();

                string message = System.Text.RegularExpressions.Regex.Match(encodedMessage, @"info : ([\s\S]*)").Value.Replace("info : ", "");

                if (message.Contains("卖店"))
                {
                    List<Hunt> hunts = new List<Hunt>();

                    int roleId = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"用户([0-9]*)").Value.Replace("用户", ""));
                    int amount = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"卖店([0-9]*)").Value.Replace("卖店", ""));
                    int itemId = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"个([0-9]*)").Value.Replace("个", ""));

                    for (int i = 0; i < amount; i++)
                    {
                        hunts.Add(new Hunt
                        {
                            ItemId = itemId,
                            RoleId = roleId,
                            Date = DateTime.Now
                        });
                    }

                    newInfo.Hunt = hunts;
                }
                else if (message.Contains("采集得到"))
                {
                    List<Collect> coletas = new List<Collect>();

                    int roleId = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"用户([0-9]*)").Value.Replace("用户", default));
                    int amount = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"采集得到([0-9]*)").Value.Replace("采集得到", default));
                    int itemId = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"个([0-9]*)").Value.Replace("个", default));

                    for (int i = 0; i < amount; i++)
                    {
                        coletas.Add(new Collect
                        {
                            RoleId = roleId,
                            Amount = 1,
                            ItemId = itemId,
                            Date = DateTime.Now
                        });
                    }

                    newInfo.Collect = coletas;
                }

                return newInfo;
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }
    }
}