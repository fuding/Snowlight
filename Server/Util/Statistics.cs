using System;
using System.Threading;

using Snowlight.Storage;
using Snowlight.Game.Sessions;
using Snowlight.Config;
using System.Data;


namespace Snowlight.Util
{
    public static class StatisticsSyncUtil
    {
        public static void Initialize()
        {
            Thread Thread = new Thread(new ThreadStart(ProcessThread));
            Thread.Priority = ThreadPriority.Lowest;
            Thread.Name = "StatisticsDbSyncThread";
            Thread.Start();
        }

        private static void ProcessThread()
        {
            Int32 user_peak = 0;
            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                DataRow Row = MySqlClient.ExecuteQueryRow("SELECT sval FROM server_statistics WHERE skey = 'online_peak' LIMIT 1");
                if (Row != null)
                {
                    user_peak = Convert.ToInt32(Row[0]);
                }
            }

            while (Program.Alive)
            {
                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    
                    MySqlClient.SetParameter("skey", "active_connections");
                    MySqlClient.SetParameter("sval", SessionManager.ActiveConnections);
                    MySqlClient.ExecuteNonQuery("UPDATE server_statistics SET sval = @sval WHERE skey = @skey LIMIT 1");

                    MySqlClient.SetParameter("skey", "stamp");
                    MySqlClient.SetParameter("sval", UnixTimestamp.GetCurrent());
                    MySqlClient.ExecuteNonQuery("UPDATE server_statistics SET sval = @sval WHERE skey = @skey LIMIT 1");

                    if (SessionManager.ActiveConnections > user_peak)
                    {
                        user_peak = SessionManager.ActiveConnections;

                        MySqlClient.SetParameter("skey", "online_peak");
                        MySqlClient.SetParameter("sval", user_peak);
                        MySqlClient.ExecuteNonQuery("UPDATE server_statistics SET sval = @sval WHERE skey = @skey LIMIT 1");

                        MySqlClient.SetParameter("skey", "online_peak_stamp");
                        MySqlClient.SetParameter("sval", UnixTimestamp.GetCurrent());
                        MySqlClient.ExecuteNonQuery("UPDATE server_statistics SET sval = @sval WHERE skey = @skey LIMIT 1");
                    }
                }

                Thread.Sleep(60 * 1000);
            }
        }
    }
}
