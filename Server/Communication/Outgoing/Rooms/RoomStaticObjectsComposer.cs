using System;
using System.Collections.Generic;

using Snowlight.Game.Items;

namespace Snowlight.Communication.Outgoing
{
    public static class RoomStaticObjectsComposer
    {
        public static ServerMessage Compose(List<StaticObject> Objects)
        {
            // QDHC00cornerchair2HHHPAHB01cornerchair1IHHPAHA02chair1JHHPAHA03chair1KHHPAHH05table1QAHHPAHE06chair1lineRAHHPAHA07chair1SAHHPAHF08chair1frontendPBHHPAHJ010hububarRBHHPAHB10cornerchair1HIHJHA20chair1HJHJHA30chair1HKHJHA40chair1HPAHJHF50chair1frontendHQAHJHI53table2KQAHPAHD55modchairQAQAHHHI58table2PBQAHPA
            ServerMessage Message = new ServerMessage(OpcodesOut.ROOM_STATIC_OBJECTS);
            Message.AppendInt32(Objects.Count);
            Message.AppendInt32(0);
            foreach (StaticObject Object in Objects)
            {               
                Message.AppendUInt32(Object.Id);  // ?? Seems to be any Id; No Idea what it's used for
                Message.AppendStringWithBreak("SNW");              // Just a prefix to show the same object more than once                      
                Message.AppendStringWithBreak(Object.Name);              // Name              
                Message.AppendInt32(Object.Position.X);                                     // X
                Message.AppendInt32(Object.Position.Y);                                     // Y
                Message.AppendInt32(Object.Height);                                     // Z(??)
                Message.AppendInt32(Object.Rotation);                                     // rot??? Has no effect 
            }

            return Message;
        }
    }
}
