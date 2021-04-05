using System;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using CoreRanking.Model.Data;
using System.Net.Http;

namespace CoreRanking.License
{
    public class LicenseControl
    {
        private static string user;
        private static string licenseKey;
        private static string hwid = default;
        private static Product product;

        public State _response = State.Erro;

        Timer LicenseWatch;
        public async Task Start(string _user, string _licenseKey, Product _product)
        {
            user = _user;
            licenseKey = _licenseKey;
            hwid = await UserHWID();
            product = _product;

            if (!string.IsNullOrEmpty(hwid))
            {
                await DoAllTests();

                LicenseWatch = new Timer(1800000);
                LicenseWatch.Elapsed += LicenseTick;
                LicenseWatch.Start();
            }
            else
            {                
                LogWriter.Write("Não foi possível obter o registro de HWID.");
                Environment.Exit(0);
            }
        }

        private async void LicenseTick(object sender, ElapsedEventArgs e)
        {
            await DoAllTests();
        }

        private async Task DoAllTests()
        {
            try
            {
                //Checa se existe registro do equipamento
                var httpClient = HttpClientFactory.Create();

                var response = (State)int.Parse(await httpClient.GetStringAsync($"http://license.ironside.dev/api/license/{user}/{licenseKey}/{hwid}/{Enum.GetName(typeof(Product), (int)product)}"));
                _response = response;

                if (!(response is State.Erro))
                {
                    if (response is State.Inexiste)
                    {                        
                        LogWriter.Write("Não existe registro da sua instância. Entre em contato com a administração. Discord: Ironside#3862");
                        Environment.Exit(0);
                    }
                    else if (response is State.Welcome)
                    {
                        LogWriter.Write($"Bem vindo, {user}!");
                    }
                    else
                    {
                        //Checa se a licença está na validade
                        if (response is State.Expirado)
                        {                            
                            LogWriter.Write("Sua licença está fora da validade. Entre em contato com a administração. Discord: Ironside#3862");
                            Environment.Exit(0);
                        }
                        else
                        {
                            //Checa se a licença está ativa
                            if (response is State.Inativo)
                            {                         
                                LogWriter.Write("Sua licença não está ativa. Entre em contato com a administração. Discord: Ironside#3862");
                                Environment.Exit(0);
                            }
                            else
                            {
                                //Checa se a licença atual já está sendo utilizada por outro HWID
                                if (response is State.Esgotado)
                                {                             
                                    LogWriter.Write("Essa licença já está registrada em outra instância. Entre em contato com a administração. Discord: Ironside#3862");
                                    Environment.Exit(0);
                                }
                                else
                                {
                                    LogWriter.Write("Produto inválido. Entre em contato com a administração. Discord: Ironside#3862");
                                    Environment.Exit(0);
                                }
                            }
                        }
                    }
                }
                else
                {                    
                    LogWriter.Write("Houve um erro na requisição da licença. Discord: Ironside#3862");
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                LogWriter.Write(e.ToString());
                Environment.Exit(0);
            }
        }
        private static async Task<string> UserHWID()
        {
            var ip = new WebClient().DownloadString(new Uri("https://ipv4.icanhazip.com/"));

            return ip.Replace("\n", default).Replace(".", default);
        }
    }
}
