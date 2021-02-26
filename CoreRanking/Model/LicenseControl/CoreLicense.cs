using System;

namespace CoreRanking.Model
{
    public class CoreLicense
    {

        public int id { get; set; }
        public string user { get; set; }
        public string licensekey { get; set; }
        public DateTime validade { get; set; }
        public bool active { get; set; }
        public string hwid { get; set; }
    }
}
