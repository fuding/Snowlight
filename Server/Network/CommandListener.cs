using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Snowlight.Util;
using Snowlight.Game.Sessions;
using Snowlight.Game.Misc;
using Snowlight.Communication.Outgoing;
using Snowlight.Communication;
using Snowlight.Storage;
using Snowlight.Game.Rooms;
using Snowlight.Game.Achievements;
using Snowlight.Game.Catalog;
using Snowlight.Game.Items;

namespace Snowlight.Network
{
    public class  CommandListener
    {
        private Socket mSocket;
        private byte[] mBuffer; 
        private static uint mCounter = 0;
        private static Dictionary<uint, CommandListener> mSessions = new Dictionary<uint, CommandListener>();
        private uint mId;
        private bool is_human = false;

        public CommandListener(uint Id)
        {
            mId = Id;
        }
        
        public static void parse(Socket IncomingSocket)
        {
            uint Id = mCounter++;

            IncomingSocket.Blocking = false;
            Output.WriteLine("Started Command client " + Id + ".", OutputLevel.DebugInformation);

            CommandListener mus = new CommandListener(Id);
            mus.mBuffer = new byte[512];
            mus.mSocket = IncomingSocket;
            //mus.SendData("Welcome. For not being disconnected after a command type human.\r\n");
            mus.BeginReceive();           

            mSessions.Add(Id,mus);
        }

        private void BeginReceive()
        {
            try
            {
                if (mSocket != null)
                {
                    mSocket.BeginReceive(mBuffer, 0, mBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveData), null);
                }
            }
            catch (Exception)
            {
                stop(mId);
            }
        }

        private void OnReceiveData(IAsyncResult Result)
        {
            int ByteCount = 0;

            try
            {
                if (mSocket != null)
                {
                    ByteCount = mSocket.EndReceive(Result);
                }
            }
            catch (Exception) { }

            if (ByteCount < 1 || ByteCount >= mBuffer.Length)
            {
                stop(mId);
                return;
            }

            ProcessData(ByteUtil.Subbyte(mBuffer, 0, ByteCount));
            BeginReceive();
        }

        private void ProcessData(byte[] Data)
        {
            if (Data.Length == 0)
            {
                return;
            }

            ASCIIEncoding enc = new ASCIIEncoding();
            String command = enc.GetString(Data);

            command = command.Replace("\r\n", "").Trim();
            String[] bits = command.Split(Convert.ToChar(1));

            command = bits[0];
            Session Target = null;
            
            switch (command)
            {
                case "status":
                    SendData("1");
                    break;

                case "human":
                    is_human = true;
                    SendData("Welcome. To get a list of commands type commands.");
                    break;  

                case "close":
                case "exit":
                    SendData("Bye");
                    stop(mId);
                    break;

                case "ha":
                    if (bits.Length < 2)
                    {
                        SendData("Command must be ha <message>");
                        break;
                    }
                    string Alert = UserInputFilter.FilterString(bits[1]);
                     SessionManager.BroadcastPacket(UserAlertModernComposer.Compose("Important notice from Hotel Management", Alert));
                    break;

                case "update_catalog":
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        CatalogManager.RefreshCatalogData(MySqlClient);
                    }
                    break;

                case "update_items":
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        ItemDefinitionManager.Initialize(MySqlClient);
                    }
                    break;

                case "premium":
                    if (bits.Length < 3 || (bits[1] != "enable" && bits[1] != "disable"))
                    {     
                        SendData("Command must be premium (enable|disable) <userid>");
                        break;
                    }
                    
                        Target = SessionManager.GetSessionByCharacterId(Convert.ToUInt32(bits[2]));

                        if (Target == null)
                        {
                            SendData("User not online");
                            break;
                        }

                        if (bits[1] == "enable" && !Target.CharacterInfo.IsPremium)
                        {
                            Target.CharacterInfo.IsPremium = true;
                            Target.SendData(UserAlertModernComposer.Compose("Premium", "Your premium was activated"));

                            ServerMessage Welcome = new ServerMessage(575);
                            Welcome.AppendInt32(1);
                            Welcome.AppendInt32(0);
                            Welcome.AppendInt32(1);
                            Target.SendData(Welcome);
                        }
                        else if(bits[1] == "disable" && Target.CharacterInfo.IsPremium)
                        {
                            ServerMessage Welcome = new ServerMessage(575);
                            Welcome.AppendInt32(0);
                            Welcome.AppendInt32(1);
                            Welcome.AppendInt32(0);
                            Target.SendData(Welcome);

                            Target.CharacterInfo.IsPremium = false;
                            Target.SendData(UserAlertModernComposer.Compose("Premium", "Your premium was deactivated"));
                        }

                        Target.SendData(CatalogUpdatedNotificationComposer.Compose());

                        SendData("OK");
                        
                    break;
                case "update_badges":
                    if (bits.Length < 2)
                    {
                        SendData("Command must be update_badges <userid>");
                        break;
                    }
                        Target = SessionManager.GetSessionByCharacterId(Convert.ToUInt32(bits[1]));

                        if (Target == null)
                        {
                            SendData("User not online");
                            break;
                        }

                      using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient()) {
                          Target.AchievementCache.ReloadCache(MySqlClient);
                          Target.BadgeCache.ReloadCache(MySqlClient, Target.AchievementCache);

                          RoomInstance Instance = RoomManager.GetInstanceByRoomId(Target.CurrentRoomId);
                         
                          if (Instance != null)
                          {
                              Instance.BroadcastMessage(RoomUserBadgesComposer.Compose(Target.CharacterId, Target.BadgeCache.EquippedBadges));
                          }                   
                         
                      }
                      SendData("OK");
                        break;
                    
                case "":
                    break;
                default:
                    SendData("Unknown Command.");
                    break;
            }

            if (!is_human)
            {
                stop(mId);
            }
            
        }

        private void SendData(String command)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            if (is_human) { command = command + "\r\n"; }
            Byte[] Data = enc.GetBytes(command);
            try
            {
                if (mSocket != null)
                {
                    mSocket.BeginSend(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(OnDataSent), null);
                }
            }
            catch (Exception e)
            {
                Output.WriteLine("[SND] Socket is null!\n\n" + e.StackTrace, OutputLevel.CriticalError);
            }
        }

        private void OnDataSent(IAsyncResult Result)
        {
            try
            {
                if (mSocket != null)
                {
                    mSocket.EndSend(Result);
                }
            }
            catch (Exception)
            {
                stop(mId);
            }
        }

        private void stop(uint Id)
        {
            mSocket.Close();
            stop2(Id);
        }
        
        private static void stop2(uint Id)
        {
            if(mSessions.ContainsKey(Id)) {
            mSessions.Remove(Id);
            Output.WriteLine("Stopped Command client " + Id + ".", OutputLevel.DebugInformation);
            }
        }
    }
}
