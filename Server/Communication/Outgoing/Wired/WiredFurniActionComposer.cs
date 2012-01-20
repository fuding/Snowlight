using System;
using Snowlight.Game.Items;

namespace Snowlight.Communication.Outgoing
{
    public static class WiredFurniActionComposer
    {
        public static ServerMessage Compose(Item Item)
        {
            // com.sulake.habbo.communication.messages.incoming.userdefinedroomevents.WiredFurniActionEvent;
            ServerMessage Message = new ServerMessage(OpcodesOut.WIRED_FURNI_ACTION);
            Message.AppendInt32(2);
            Message.AppendInt32(3);
            Message.AppendInt32(0);
            Message.AppendUInt32(Item.Definition.SpriteId);
            Message.AppendUInt32(Item.Id);
            Message.AppendStringWithBreak(Item.WiredData.Data1);
            Message.AppendInt32(Item.WiredData.Data2);
            Message.AppendInt32(Item.WiredData.Data2);
            Message.AppendInt32(Item.Definition.BehaviorData);
            Message.AppendInt32(0);
            Message.AppendInt32(0);
            return Message; 
        }
    }
}
