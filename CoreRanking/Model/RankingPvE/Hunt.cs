using CoreRanking.Model.RankingPvP;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreRanking.Model.RankingPvE
{
    public class Hunt
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        [Required][ForeignKey("Role")]
        public int RoleId { get; set; }        
        public Role Role { get; set; }
        public DateTime Date { get; set; }
    }
}
