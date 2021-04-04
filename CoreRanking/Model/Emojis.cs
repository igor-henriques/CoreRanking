using System;

namespace CoreRanking.Model
{
    public class Emojis
    {
        public static string CreateRandom(EmojiType type)
        {
            string x = $"<0><{((int)type).ToString()}:{new Random().Next(50).ToString()}>";
            return x;
        }
    }
}
