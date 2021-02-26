using System;

namespace CoreRanking.Model.Data
{
    public class Banned
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public DateTime BanTime { get; set; }
    }
}