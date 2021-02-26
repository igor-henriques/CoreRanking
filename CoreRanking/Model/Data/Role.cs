using System.ComponentModel.DataAnnotations;

namespace CoreRanking.Model
{
    public class Role
    {  
        public int AccountId { get; set; }
        [Key]
        public int RoleId { get; set; }
        public string CharacterName { get; set; }
        public string CharacterClass { get; set; }  
        public string CharacterGender { get; set; }
        public string Elo { get; set; }
        public int Level { get; set; }
        public int Points { get; set; }
        public string Evento { get; set; }
        public int Doublekill { get;set; }
        public int Triplekill { get;set; }
        public int Quadrakill { get;set; }
        public int Pentakill { get;set; }
        public double CollectPoint { get; set; }
    }
}