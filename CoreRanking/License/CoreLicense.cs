using System;

namespace CoreRanking.License
{
    public class CoreLicense
    {

        public int Id { get; set; }
        public string User { get; set; }
        public string Licensekey { get; set; }
        public DateTime Validade { get; set; }
        public bool Active { get; set; }
        public string Hwid { get; set; }
        public Product Product { get; set; }
    }
}
