using System;
using System.Collections.Generic;
using System.Linq;

using Snowlight.Game.Sessions;
using Snowlight.Communication.Outgoing;
using Snowlight.Game.Infobus;
using Snowlight.Game.Rooms;
using Snowlight.Game.Bots;
using Snowlight.Storage;
using Snowlight.Game.Achievements;
using Snowlight.Util;
using System.Text;
using Snowlight.Game.Moderation;
using Snowlight.Config;
using Snowlight.Game.Items;
using Snowlight.Game.Characters;
using Snowlight.Communication;
using Snowlight.Game.Rights;
using Snowlight.Specialized;

namespace Snowlight.Game.Misc
{
    public static class ChatCommands
    {
        public static bool HandleCommand(Session Session, string Input)
        {
            Input = Input.Substring(1, Input.Length - 1);
            string[] Bits = Input.Split(' ');

            RoomInstance Instance = RoomManager.GetInstanceByRoomId(Session.CurrentRoomId);
            RoomActor Actor = (Instance == null ? null : Instance.GetActorByReferenceId(Session.CharacterId));
            Session TargetSession = null;
            RoomActor TargetActor = null;
            String TargetName = "";

            switch (Bits[0].ToLower())
            {
                #region users
                #region misc
                case "commands":
                    {
                        Session.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.commands.info") + ":\n\n:commands\n:online\n:about\n:pickall"));
                        return true;
                    }
                case "online":
                    {
                        List<string> OnlineUsers = SessionManager.ConnectedUserData.Values.ToList();
                        StringBuilder MessageText = new StringBuilder(Localization.GetValue("command.online", OnlineUsers.Count.ToString()) + "\n");

                        foreach (string OnlineUser in OnlineUsers)
                        {
                            MessageText.Append('\n');
                            MessageText.Append("- " + OnlineUser);
                        }

                        Session.SendData(NotificationMessageComposer.Compose(MessageText.ToString()));
                        return true;
                    }
                case "about":

                    Session.SendData(UserAlertModernComposer.Compose("Powered by Snowlight", "This hotel is proudly powered by Snowlight,\nedited by flx5. \nCredits to Meth0d."));
                    return true;
                #endregion
                #region furni
                case "empty":
                case "emptyinv":

                    if (Bits.Length > 2)
                    {
                        return false;
                    }
                             
                    if (!Session.HasRight("hotel_admin") && Bits.Length == 2)
                    {
                        return false;
                    }

                    Session Targetuser = Session;

                    if (Bits.Length == 2)
                    {
                        uint userid = CharacterResolverCache.GetUidFromName(Bits[1]);
                        Targetuser = SessionManager.GetSessionByCharacterId(userid);
                    }

                    Targetuser.PetInventoryCache.ClearAndDeleteAll();
                    Targetuser.InventoryCache.ClearAndDeleteAll();                   
                    Targetuser.SendData(InventoryRefreshComposer.Compose());
                    Targetuser.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.emptyinv.sucess")));
                    return true;

                case "pickall":

                    if (!Instance.CheckUserRights(Session, true))
                    {
                        Session.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.pickall.error")));
                        return true;
                    }
                    Instance.PickAllToUserInventory(Session);
                    return true;
                #endregion
                #region extra
                case "moonwalk":
                    if (!Session.CharacterInfo.IsPremium)
                    {
                        return false;
                    }

                    Actor.WalkingBackwards = !Actor.WalkingBackwards;
                    Actor.Dance(Actor.WalkingBackwards ? 4 : 0);
                    Session.SendData(RoomChatComposer.Compose(Actor.Id, "TEST " + Actor.WalkingBackwards, 0, ChatType.Whisper));
                    return true;

                #region push
                case "push":
                    if (!Session.CharacterInfo.IsPremium || Bits.Length != 2)
                    {
                        return false;
                    }
                    TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                    TargetActor = Instance.GetActorByReferenceId(CharacterResolverCache.GetUidFromName(TargetName));

                    if (TargetActor == null || TargetActor.IsMoving)
                    {
                        return false;
                    }

                    

                    if ((TargetActor.Position.X == Actor.Position.X - 1) || (TargetActor.Position.X == Actor.Position.X + 1) || (TargetActor.Position.Y == Actor.Position.Y - 1) || (TargetActor.Position.Y == Actor.Position.Y + 1))
                    {
                        Vector2 Newposition = null;
                        
                        if (TargetActor.Position.X == Actor.Position.X - 1 && TargetActor.Position.Y == Actor.Position.Y)
                        {
                            Newposition = new Vector2(TargetActor.Position.X - 1, TargetActor.Position.Y);
                        }

                        if (TargetActor.Position.X == Actor.Position.X + 1 && TargetActor.Position.Y == Actor.Position.Y)
                        {
                            Newposition = new Vector2(TargetActor.Position.X + 1, TargetActor.Position.Y);
                        }

                        if (TargetActor.Position.X == Actor.Position.X && TargetActor.Position.Y == Actor.Position.Y + 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X, TargetActor.Position.Y + 1);
                        }

                        if (TargetActor.Position.X == Actor.Position.X && TargetActor.Position.Y == Actor.Position.Y - 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X, TargetActor.Position.Y - 1);
                        }

                        if (TargetActor.Position.X == Actor.Position.X + 1 && TargetActor.Position.Y == Actor.Position.Y + 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X + 1, TargetActor.Position.Y + 1);
                        }

                        if (TargetActor.Position.X == Actor.Position.X - 1 && TargetActor.Position.Y == Actor.Position.Y - 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X - 1, TargetActor.Position.Y - 1);
                        }

                        if (TargetActor.Position.X == Actor.Position.X - 1 && TargetActor.Position.Y == Actor.Position.Y + 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X - 1, TargetActor.Position.Y + 1);
                        }

                        if (TargetActor.Position.X == Actor.Position.X + 1 && TargetActor.Position.Y == Actor.Position.Y - 1)
                        {
                            Newposition = new Vector2(TargetActor.Position.X + 1, TargetActor.Position.Y - 1);
                        }

                        if (Newposition == null || !Instance.IsValidPosition(Newposition) || (Instance.Model.DoorPosition.GetVector2().X == Newposition.X && Instance.Model.DoorPosition.GetVector2().Y == Newposition.Y))
                        {
                            return false;
                        }

                        TargetActor.MoveTo(Newposition);                     
                        Actor.Chat("*" + Session.CharacterInfo.Username + " pushes " + Bits[1] + "*");
                        return true;
                    }
                    else
                    {
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, Bits[1] + " is not in your area.",0,ChatType.Whisper));
                        return false;
                    }
                    
                #endregion
                
                case "pull":
                    if (!Session.CharacterInfo.IsPremium || Bits.Length != 2)
                    {
                        return false;
                    }

                    TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                    TargetActor = Instance.GetActorByReferenceId(CharacterResolverCache.GetUidFromName(TargetName));

                    if (TargetActor == null || TargetActor.IsMoving)
                    {
                        return false;
                    }

                    if ((TargetActor.Position.X > Actor.Position.X - 10) && (TargetActor.Position.X < Actor.Position.X + 10) && (TargetActor.Position.Y > Actor.Position.Y - 10) && (TargetActor.Position.Y < Actor.Position.Y + 10) && (Instance.Model.DoorPosition.GetVector2().X == Actor.SquareInFront.X && Instance.Model.DoorPosition.GetVector2().Y == Actor.SquareInFront.Y))
                    {
                        TargetActor.MoveTo(Actor.SquareInFront);
                        Actor.Chat("*" + Session.CharacterInfo.Username + " pulls " + Bits[1] + "*");
                        return true;
                    }

                    Session.SendData(RoomChatComposer.Compose(Actor.Id, Bits[1] + " is not in your area.", 0, ChatType.Whisper));
                    return false;
                #endregion
                #endregion

                #region debugging
                #region items
                case "update_catalog":
                    {
                        if (!Session.HasRight("hotel_admin"))
                        {
                            return false;
                        }
                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            Snowlight.Game.Catalog.CatalogManager.RefreshCatalogData(MySqlClient);
                        }
                        Session.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.updatecatalog.success")));
                        return true;
                    }
                case "update_items":
                    {
                        if (!Session.HasRight("hotel_admin"))
                        {
                            return false;
                        }
                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            Snowlight.Game.Items.ItemDefinitionManager.Initialize(MySqlClient);
                        }
                        Session.SendData(NotificationMessageComposer.Compose("Items reloaded"));
                        return true;
                    }
                #endregion
                #region rooms
                case "unload":
                    if (!Session.HasRight("hotel_admin"))
                    {
                        return false;
                    }
                    Instance.BroadcastMessage(NotificationMessageComposer.Compose("This room was unloaded!"));
                    Instance.Unload();
                    return true;
                case "t":

                    if (!Session.HasRight("hotel_admin"))
                    {
                        return false;
                    }

                    Session.SendData(NotificationMessageComposer.Compose("Position: " + Actor.Position.ToString() + ", Rotation: " + Actor.BodyRotation));
                    return true;

                #endregion
           
                case "update_rights":
                    if (!Session.HasRight("hotel_admin"))
                    {
                        return false;
                    }

                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        RightsManager.RebuildCache(MySqlClient);
                    }

                    return true;
                case "effect":

                    if (!Session.HasRight("hotel_admin"))
                    {
                        return false;
                    }

                    if (Bits.Length < 1)
                    {
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :effect <id>", 0, ChatType.Whisper));
                        return true;
                    }

                    int effectID;

                    if (int.TryParse(Bits[1], out effectID))
                    {
                        Actor.ApplyEffect(effectID);
                        Session.CurrentEffect = 0;
                    }
                    else
                    {
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :effect <id>", 0, ChatType.Whisper));
                    }

                    return true;

                case "clipping":

                    if (!Session.HasRight("hotel_admin"))
                    {
                        return false;
                    }

                    Actor.OverrideClipping = !Actor.OverrideClipping;
                    Actor.ApplyEffect(Actor.ClippingEnabled ? 0 : 23);
                    Session.CurrentEffect = 0;
                    return true;  

                #endregion

                #region moderation
                #region kick
                case "superkick":  // Kick User out of the Hotel
                    {
                        if (!Session.HasRight("hotel_admin"))
                        {
                            return false;
                        }

                        if (Bits.Length < 2)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.invalidsyntax") + " - :kick <username>", 0, ChatType.Whisper));
                            return true;
                        }

                        TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                        TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(TargetName));

                        if (TargetSession == null || TargetSession.HasRight("moderation_tool") || !TargetSession.InRoom)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' is offline or cannot be kicked.", 0, ChatType.Whisper));
                            return true;
                        }

                        SessionManager.StopSession(TargetSession.Id);

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Superkicked user from server (chat command)",
                               "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                        }

                        return true;
                    }
                case "kick": //kick User out of Room
                    {
                        if (!Session.HasRight("moderation_tool"))
                        {
                            return false;
                        }

                        if (Bits.Length < 2)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.invalidsyntax") + " - :kick <username>", 0, ChatType.Whisper));
                            return true;
                        }

                        TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                        TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(TargetName));

                        if (TargetSession == null || TargetSession.HasRight("moderation_tool") || !TargetSession.InRoom)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' is offline, not in a room, or cannot be kicked.", 0, ChatType.Whisper));
                            return true;
                        }

                        RoomManager.RemoveUserFromRoom(TargetSession, true);
                        TargetSession.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.kick.success")));

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Kicked user from room (chat command)",
                                "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                        }

                        return true;
                    }
                #endregion
                #region mute
                case "roomunmute":
                    {
                        if (!Session.HasRight("mute"))
                        {
                            return false;
                        }

                        if (Instance.RoomMuted)
                        {
                            Instance.RoomMuted = false;
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.roomunmute.success"), 0, ChatType.Whisper));
                        }
                        else
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.roomunmute.error"), 0, ChatType.Whisper));
                        }

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Unmuted room", "Room '"
                                + Instance.Info.Name + "' (ID " + Instance.RoomId + ")");
                        }

                        return true;
                    }
                case "roommute":
                    {
                        if (!Session.HasRight("mute"))
                        {
                            return false;
                        }

                        if (!Instance.RoomMuted)
                        {
                            Instance.RoomMuted = true;
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.roommute.success"), 0, ChatType.Whisper));
                        }
                        else
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.roommute.error"), 0, ChatType.Whisper));
                        }

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Muted room", "Room '"
                                + Instance.Info.Name + "' (ID " + Instance.RoomId + ")");
                        }

                        return true;
                    }

                case "unmute":
                    {
                        if (!Session.HasRight("mute"))
                        {
                            return false;
                        }

                        if (Bits.Length < 2)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.invalidsyntax") + " - :unmute <username>", 0, ChatType.Whisper));
                            return true;
                        }

                        TargetName = UserInputFilter.FilterString(Bits[1].Trim());

                        TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(TargetName));

                        if (TargetSession == null)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' " + Localization.GetValue("command.cannotproceedcmd3"), 0, ChatType.Whisper));
                            return true;
                        }

                        if (!TargetSession.CharacterInfo.IsMuted)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' " + Localization.GetValue("command.unmute.error"), 0, ChatType.Whisper));
                            return true;
                        }

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            TargetSession.CharacterInfo.Unmute(MySqlClient);
                        }

                        TargetSession.SendData(NotificationMessageComposer.Compose(Localization.GetValue("command.unmute.sucess")));
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' " + Localization.GetValue("command.unmute.sucess2"), 0, ChatType.Whisper));

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Unmuted user",
                                "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ").");
                        }

                        return true;
                    }

                case "mute":
                    {
                        if (!Session.HasRight("mute"))
                        {
                            return false;
                        }

                        if (Bits.Length < 2)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.invalidsyntax") + " - :mute <username> [length in seconds]", 0, ChatType.Whisper));
                            return true;
                        }

                        TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                        int TimeToMute = 0;

                        if (Bits.Length >= 3)
                        {
                            int.TryParse(Bits[2], out TimeToMute);
                        }

                        if (TimeToMute <= 0)
                        {
                            TimeToMute = 300;
                        }

                        if (TimeToMute > 3600)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.mute.error"), 0, ChatType.Whisper));
                            return true;
                        }

                        TargetSession = SessionManager.GetSessionByCharacterId(CharacterResolverCache.GetUidFromName(TargetName));

                        if (TargetSession == null || TargetSession.HasRight("mute"))
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.targetuser") + " '" + TargetName + "' " + Localization.GetValue("command.cannotproceedcmd4"), 0, ChatType.Whisper));
                            return true;
                        }

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            TargetSession.CharacterInfo.Mute(MySqlClient, TimeToMute);
                            ModerationLogs.LogModerationAction(MySqlClient, Session, "Muted user",
                                "User '" + TargetSession.CharacterInfo.Username + "' (ID " + TargetSession.CharacterId + ") for " + TimeToMute + " seconds.");
                        }

                        TargetSession.SendData(RoomMutedComposer.Compose(TimeToMute));
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, Localization.GetValue("command.mute.sucess.part1") + " '" + TargetName + "' " + Localization.GetValue("command.mute.sucess.part2") + " " + TimeToMute + " seconds.", 0, ChatType.Whisper));
                        return true;
                    }
                #endregion
                #region credits
                case "coins":
                case "credits":
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        if (!Session.HasRight("hotel_admin"))
                        {
                            return false;
                        }
                        if (Bits.Length < 2)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Invalid syntax - :" + Bits[0].ToLower() + " <user> <amount>", 0, ChatType.Whisper));
                            return false;
                        }
                        int Valor;
                        if (!Int32.TryParse(Bits[2], out Valor))
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "Amount must be numeric!", 0, ChatType.Whisper));
                            return false;
                        }

                        TargetName = UserInputFilter.FilterString(Bits[1].Trim());
                        uint UserID = CharacterResolverCache.GetUidFromName(TargetName);

                        if (UserID == 0)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "User not found!", 0, ChatType.Whisper));
                            return false;
                        }
                        Session TargetUser = SessionManager.GetSessionByCharacterId(UserID);
                        if (TargetUser == null)
                        {
                            Session.SendData(RoomChatComposer.Compose(Actor.Id, "User not online!", 0, ChatType.Whisper));
                            return false;
                        }
                        TargetUser.CharacterInfo.UpdateCreditsBalance(MySqlClient, (int)Valor);
                        TargetUser.SendData(RoomChatComposer.Compose(TargetUser.Id, "You received " + Valor + " coins!", 0, ChatType.Whisper));
                        Session.SendData(RoomChatComposer.Compose(Actor.Id, TargetName + " received " + Valor + " coins!", 0, ChatType.Whisper));
                        TargetUser.SendData(CreditsBalanceComposer.Compose(TargetUser.CharacterInfo.CreditsBalance));
                      
                        return true;
                    }
                #endregion
                #region messages
                case "ha":
                    {
                        if (!Session.HasRight("hotel_admin"))
                        {
                            return false;
                        }
                        string Alert = UserInputFilter.FilterString(MergeInputs(Bits, 1));
                        SessionManager.BroadcastPacket(UserAlertModernComposer.Compose("Important notice from Hotel Management", Alert));
                        return true;
                    }
                #endregion
                #endregion
                      
            }


            return false;
        }

        public static string MergeInputs(string[] Inputs, int Start) //From UberEmu
        {
            StringBuilder MergedInputs = new StringBuilder();

            for (int i = 0; i < Inputs.Length; i++)
            {
                if (i < Start)
                {
                    continue;
                }

                if (i > Start)
                {
                    MergedInputs.Append(" ");
                }

                MergedInputs.Append(Inputs[i]);
            }

            return MergedInputs.ToString();
        } 
    }
}
