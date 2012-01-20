using System;
using System.Data;

using Snowlight.Specialized;
using Snowlight.Storage;

namespace Snowlight.Game.Pets
{
    public static class PetFactory
    {
        public static Pet CreatePet(SqlDatabaseClient MySqlClient, uint UserId, int Type, string Name, int Race, String color)
        {
            MySqlClient.SetParameter("userid", UserId);
            MySqlClient.SetParameter("type", Type);
            MySqlClient.SetParameter("name", Name);
            MySqlClient.SetParameter("race", Race);
            MySqlClient.SetParameter("timestamp", UnixTimestamp.GetCurrent());
            MySqlClient.SetParameter("color", color);

            string RawId = MySqlClient.ExecuteScalar("INSERT INTO pets (user_id,type,name,race,timestamp,color) VALUES (@userid,@type,@name,@race,@timestamp,@color); SELECT LAST_INSERT_ID();").ToString();

            uint Id = 0;
            uint.TryParse(RawId, out Id);

            if (Id == 0)
            {
                return null;
            }

            return new Pet(Id, Name, Type, Race, UserId, 0, new Vector3(0, 0, 0), UnixTimestamp.GetCurrent(), 0, 120, 100, 0, color);
        }

        public static Pet GetPetFromDatabaseRow(DataRow Row)
        {
            return new Pet((uint)Row["id"], (string)Row["name"], (int)Row["type"], (int)Row["race"],
                (uint)Row["user_id"], (uint)Row["room_id"], Vector3.FromString((string)Row["room_pos"]),
                (double)Row["timestamp"], (int)Row["experience"], (int)Row["energy"], (int)Row["happiness"],
                (int)Row["score"], (String)Row["color"]);
        }
    }
}
