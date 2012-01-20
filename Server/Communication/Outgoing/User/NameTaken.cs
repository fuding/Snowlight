using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snowlight.Storage;
using System.Data;

namespace Snowlight.Communication.Outgoing.User
{
    public static class NameTaken
    {
        public static ServerMessage Compose(String Username)
        {
            DataTable Data = null;

            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                Data = MySqlClient.ExecuteQueryTable("SELECT SQL_NO_CACHE * FROM tags ORDER BY rand() LIMIT 6");

            }

            ServerMessage Message = new ServerMessage(571);
            Message.AppendInt32(5);

            Message.AppendStringWithBreak(Username);
            Message.AppendInt32(Data.Rows.Count);

            foreach (DataRow Row in Data.Rows)
            {               
                Message.AppendStringWithBreak(Username + (string)Row["tag"]);
            }

            return Message;
        }
    }
}
