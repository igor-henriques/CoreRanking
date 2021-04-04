using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreRanking.Model.RankingPvP
{
    public class Banned
    {
        [Key]
        public int Id { get; set; }
        [Required][ForeignKey("Role")]
        public int RoleId { get; set; }        
        public Role Role { get; set; }
        public DateTime BanTime { get; set; }
    }
}