using System;
using Snowlight.Game.Items;
using Snowlight.Game.Items.Wired;
using Snowlight.Game.Rooms;
using System.Collections.Generic;

namespace Snowlight.Communication.Outgoing
{
    public static class WiredFurniTriggerComposer
    {
        public static ServerMessage Compose(Item Item, RoomInstance Instance)
        {   // com.sulake.habbo.communication.messages.incoming.userdefinedroomevents.WiredFurniTriggerEvent;
            ServerMessage Message = new ServerMessage(650);
            Message.AppendInt32(2);
            Message.AppendInt32(2);
            Message.AppendInt32(0);
            Message.AppendUInt32(Item.Definition.SpriteId);
            Message.AppendInt32((int)Item.Id);
            Message.AppendStringWithBreak(Item.WiredData.Data1);

            switch (WiredTypesUtil.TriggerFromInt(Item.Definition.BehaviorData))
            {
                case WiredTriggerTypes.says_something:                                     
                    Message.AppendInt32(Item.WiredData.Data2);                   
                    break;
                case WiredTriggerTypes.periodically:
                    Message.AppendInt32(1);                                    
                    break;
            }

            Message.AppendInt32(Item.WiredData.Data2);
            Message.AppendInt32(0);
            Message.AppendInt32(Item.Definition.BehaviorData);
            List<Item> Items = Instance.WiredManager.ContainsRequiresActor(Item.RoomPosition.GetVector2());
            Message.AppendInt32(Items.Count); // Contains Event that needs a User, but there is a trigger, that isn't triggere by the User
            foreach (Item Blocked in Items)
            {
                Message.AppendUInt32(Blocked.Definition.SpriteId);
            }
            return Message; 
        }
    }
}
