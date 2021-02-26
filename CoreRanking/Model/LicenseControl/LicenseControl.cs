using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using CoreRanking.Model.Data;

namespace CoreRanking.Model
{
    public class LicenseControl
    {
        private static string user;
        private static string licenseKey;
        private static string hwid = default;
        private static LogWriter newLog;
        Timer LicenseWatch;
        public LicenseControl(string _user, string _licenseKey)
        {
            user = _user;
            licenseKey = _licenseKey;
            hwid = UserHWID().Result;

            if (!string.IsNullOrEmpty(hwid))
            {
                DoAllTests();

                LicenseWatch = new Timer(3600000);
                LicenseWatch.Elapsed += LicenseTick;
                LicenseWatch.Start();
            }
            else
            {
                Console.WriteLine("Não foi possível obter o registro de HWID.");
                newLog = new LogWriter("Não foi possível obter o registro de HWID.");
            }
        }

        private async void LicenseTick(object sender, ElapsedEventArgs e)
        {
            await DoAllTests();
        }

        private static async Task DoAllTests()
        {
            using (var db = new LicenseDbContext())
            {
                //Checa se existe registro do equipamento
                CoreLicense license = db.CoreLicense.Where(x => x.licensekey.Equals(licenseKey)).Where(y => y.user.Equals(user)).FirstOrDefault();

                if (license is null)
                {
                    Console.WriteLine("Não existe registro da sua instância. Entre em contato com a administração. Discord: Ironside#3862");
                    newLog = new LogWriter("Não existe registro da sua instância. Entre em contato com a administração. Discord: Ironside#3862");                    
                    Environment.Exit(0);
                }
                else if (license.hwid.Equals("0"))
                {
                    //Checa se a licença foi criada recentemente e ainda não tem registro de HWID atrelado à licença.
                    license.user = user;
                    license.hwid = hwid;
                    license.active = true;

                    await db.SaveChangesAsync();
                    Console.WriteLine($"Bem vindo, {license.user}!");
                    newLog = new LogWriter($"Bem vindo, {license.user}!");
                }
                else
                {
                    //Checa se a licença está na validade
                    if (license.validade.Subtract(DateTime.Now) <= TimeSpan.FromSeconds(0))
                    {
                        Console.WriteLine("Sua licença está fora da validade. Entre em contato com a administração. Discord: Ironside#3862");
                        newLog = new LogWriter("Sua licença está fora da validade. Entre em contato com a administração. Discord: Ironside#3862");                        
                        Environment.Exit(0);
                    }
                    else
                    {
                        //Checa se a licença está ativa
                        if (!license.active)
                        {
                            Console.WriteLine("Sua licença não está ativa. Entre em contato com a administração. Discord: Ironside#3862");
                            newLog = new LogWriter("Sua licença não está ativa. Entre em contato com a administração. Discord: Ironside#3862");                            
                            Environment.Exit(0);
                        }
                        else
                        {
                            //Checa se a licença atual já está sendo utilizada por outro HWID
                            if (license.hwid != hwid)
                            {
                                Console.WriteLine("Essa licença já está registrada em outra instância. Entre em contato com a administração. Discord: Ironside#3862");
                                newLog = new LogWriter("Essa licença já está registrada em outra instância. Entre em contato com a administração. Discord: Ironside#3862");                                
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }
        private static async Task<string> UserHWID()
        {            
            return new WebClient().DownloadString(new Uri("https://ipv4.icanhazip.com/"));
        }
    }
}
