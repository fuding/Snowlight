using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snowlight.Storage;
using Snowlight.Game.Sessions;
using Snowlight.Communication;
using Snowlight.Communication.Incoming;
using Snowlight.Game.Rooms;
using Snowlight.Specialized;
using System.Collections.ObjectModel;
using Snowlight.Util;

namespace Snowlight.Game.Items.Wired
{
    public enum WiredTriggerTypes
    {
        says_something = 0,
        periodically = 6,
        enter_room = 7
    }

    public enum WiredEffectTypes
    {
        move_rotate = 4,
        show_message = 7
    }

    public static class WiredTypesUtil
    {
        public static WiredTriggerTypes TriggerFromInt(int Type)
        {
            switch (Type)
            {
                default:
                case 0:
                    return WiredTriggerTypes.says_something;
                case 6:
                    return WiredTriggerTypes.periodically;
                case 7:
                    return WiredTriggerTypes.enter_room;
            }
        }

        public static WiredEffectTypes EffectFromInt(int Type)
        {
            switch (Type)
            {
                default:
                case 4:
                    return WiredEffectTypes.move_rotate;
                case 7:
                    return WiredEffectTypes.show_message;
            }
        }
    }

    public class WiredManager
    {
        private Dictionary<uint, WiredData> mWired;
        private RoomInstance mInstance;

        public WiredManager(RoomInstance Instance)
        {
            mInstance = Instance;
            mWired = new Dictionary<uint, WiredData>();
        }

        public WiredData LoadWired(uint ItemId, int Type)
        {
            
            if (!mWired.ContainsKey(ItemId))
            {               
                mWired.Add(ItemId, new WiredData(ItemId, Type));
            }

            return mWired[ItemId];
        }

        public void RemoveWired(uint ItemId, SqlDatabaseClient MySqlClient)
        {
            if (mWired.ContainsKey(ItemId))
            {
                mWired.Remove(ItemId);
                MySqlClient.SetParameter("id", ItemId);
                MySqlClient.ExecuteNonQuery("DELETE FROM wired_items WHERE item_id = @id");
            }
        }

        public void SynchronizeDatabase(SqlDatabaseClient MySqlClient)
        {
            foreach(WiredData data in mWired.Values) {
                data.SynchronizeDatabase(MySqlClient);
            }
        }

        public void HandleSave(Session Session, ClientMessage Message)
        {
            uint ItemId = Message.PopWiredUInt32();
            
            Message.PopWiredInt32(); // ???

            int Item1 = Message.PopWiredInt32();
            string Keyword = Message.PopString();           
            uint Item3 = Message.PopWiredUInt32();
            uint Item4 = Message.PopWiredUInt32();
            uint Item5 = Message.PopWiredUInt32();
            uint Item6 = Message.PopWiredUInt32();
            uint Item7 = Message.PopWiredUInt32();
            uint Item8 = Message.PopWiredUInt32();

            RoomInstance Instance = RoomManager.GetInstanceByRoomId(Session.CurrentRoomId);

            if (Instance == null || !Instance.CheckUserRights(Session) || !mWired.ContainsKey(ItemId))
            {
                return;
            }

            WiredData data = mWired[ItemId];

            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                    if (data.Data1 == Keyword && data.Data2 == Item1)
                    {
                        return;
                    }
                    data.Data1 = Keyword;
                    data.Data2 = Item1; 
                    data.SynchronizeDatabase(MySqlClient);           
            }
        }

        public void HandleEnterRoom(RoomActor Actor)
        {
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.enter_room)
                {
                    if (data.Data1 != "" && data.Data1 != Actor.Name)
                    {
                        continue;
                    }

                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(mInstance);
                    Item.DisplayFlags = "";
                    Item.RequestUpdate(4);

                    ExecuteActions(Item, Actor);
                }
            }
        }

        public List<Item> ContainsRequiresActor(Vector2 Position)
        {
            List<Item> Items = new List<Item>();
            foreach (Item Item in mInstance.GetItemsOnPosition(Position))
            {
                if (Item.Definition.Behavior != ItemBehavior.WiredEffect)
                {
                    continue;
                }
                if (WiredTypesUtil.EffectFromInt(Item.Definition.BehaviorData) == WiredEffectTypes.show_message)
                {
                    Items.Add(Item);
                }
            }

            return Items;
        }

        public bool HandleChat(String Message, RoomActor Actor)
        {
            Boolean doneAction = false;
            foreach (WiredData data in mWired.Values)
            {
                Item Item = mInstance.GetItem(data.ItemId);
                if (Item.Definition.Behavior == ItemBehavior.WiredTrigger &&
                    WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.says_something &&
                    data.Data1 == Message && (data.Data2 == 0 || data.Data2 == Actor.Id)
                    )
                {

                    Item.DisplayFlags = "1";
                    Item.BroadcastStateUpdate(mInstance);
                    Item.DisplayFlags = "2";
                    Item.RequestUpdate(4);

                    ExecuteActions(Item, Actor);
                    doneAction = true;
                }
            }
            return doneAction;
        }

        public void ExecuteActions(Item Item, RoomActor Actor)
        {
            foreach (Item ActionItem in mInstance.GetItemsOnPosition(Item.RoomPosition.GetVector2()))
            {
                if (ActionItem.Definition.Behavior == ItemBehavior.WiredEffect)
                {
                    ActionItem.DisplayFlags = "1";
                    ActionItem.BroadcastStateUpdate(mInstance);
                    ActionItem.DisplayFlags = "2";
                    ActionItem.RequestUpdate(4);

                    switch (WiredTypesUtil.EffectFromInt(ActionItem.Definition.BehaviorData))
                    {
                        case WiredEffectTypes.show_message:
                            if (Actor == null)
                            {
                                continue;
                            }
                            Actor.Whisper(mWired[ActionItem.Id].Data1, 0, true);
                            break;
                    }
                }
            }
        }
    }
}
