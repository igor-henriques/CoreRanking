using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreRanking.Model.RankingPvP
{
    public class Battle
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        [Required][ForeignKey("KillerRole")]
        public int KillerId { get; set; }        
        public virtual Role KillerRole { get; set; }
        [Required][ForeignKey("KilledRole")]
        public int KilledId { get; set; }        
        public virtual Role KilledRole { get; set; }
    }
}
