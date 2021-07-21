using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreRanking.Model.RankingPvP
{
    public class Role
    {
        [Required][ForeignKey("Account")]
        public int AccountId { get; set; }        
        public Account Account { get; set; }
        [Key]
        public int RoleId { get; set; }
        public string CharacterName { get; set; }
        public string CharacterClass { get; set; }  
        public string CharacterGender { get; set; }
        public int Kill { get; set; }
        public int Death { get; set; }
        public string Elo { get; set; }
        public int Level { get; set; }
        public DateTime LevelDate{ get; set; }
        public int Points { get; set; }
        public int Doublekill { get;set; }
        public int Triplekill { get;set; }
        public int Quadrakill { get;set; }
        public int Pentakill { get;set; }
        public double CollectPoint { get; set; }
    }
}