using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snowlight.Communication.Outgoing;
using Snowlight.Game.Sessions;
using Snowlight.Communication.Incoming;
using Snowlight.Communication;
using Snowlight.Game.Rooms;
using Snowlight.Storage;
using Snowlight.Game.Items.Wired;

namespace Snowlight.Game.Items.DefaultBehaviorHandlers
{ 
    public static class WiredHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredTrigger, new ItemEventHandler(HandleWired));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredEffect, new ItemEventHandler(HandleWired));
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.WiredCondition, new ItemEventHandler(HandleWired));
        }

        private static bool HandleWired(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.Interact:
                    switch (Item.Definition.Behavior)
                    {
                        case ItemBehavior.WiredTrigger:
                            Session.SendData(WiredFurniTriggerComposer.Compose(Item, Instance));
                            break;

                        case ItemBehavior.WiredEffect:
                            Session.SendData(WiredFurniActionComposer.Compose(Item));
                            break;
                    }
                    break;
                case ItemEventType.Placed:
                    Item.WiredData = Instance.WiredManager.LoadWired(Item.Id, Item.Definition.BehaviorData);
                    Item.WiredManager = Instance.WiredManager;
                    break;
                case ItemEventType.Removing:
                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        Instance.WiredManager.RemoveWired(Item.Id, MySqlClient);
                    }
                    Item.WiredManager = null;
                    break;
                case ItemEventType.UpdateTick:
                    if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData) == WiredTriggerTypes.periodically)
                    {
                        Instance.WiredManager.ExecuteActions(Item, null);
                        Item.RequestUpdate(Item.WiredData.Data2);
                        return true;
                    }
                    Item.BroadcastStateUpdate(Instance);
                    break;
            }
            return true;
        }
  
       
    } 
} 
