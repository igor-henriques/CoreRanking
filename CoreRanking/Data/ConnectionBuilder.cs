using CoreRanking.Model.Data;
using Newtonsoft.Json;
using System.IO;

namespace CoreRanking.Model
{    
    public static class ConnectionBuilder
    {
        public static string GetConnectionString()
        {
            DatabaseConnection data = JsonConvert.DeserializeObject<DatabaseConnection>(File.ReadAllText("./Configurations/Database.json"));

            string constring = $"Server={data.HOST};Port={data.PORT};Database={data.DB};Uid={data.USER};Pwd={data.PASSWORD};ConvertZeroDateTime=True";

            return constring;
        }
    }
}
