using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snowlight.Game.Sessions;
using Snowlight.Game.Rooms;
using Snowlight.Communication.Outgoing;
using Snowlight.Specialized;
using System.Collections.ObjectModel;

namespace Snowlight.Game.Items.DefaultBehaviorHandlers
{
    public static class RollerHandler
    {
        public static void Register()
        {
            ItemEventDispatcher.RegisterEventHandler(ItemBehavior.Roller, new ItemEventHandler(HandleRoller));
        }

        private static bool HandleRoller(Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
        {
            switch (Event)
            {
                case ItemEventType.UpdateTick:

                    List<RoomActor> ActorsToMove = Instance.GetActorsOnPosition(Item.RoomPosition.GetVector2());
                    List<Item> ItemsToMove = new List<Item>();
                    ItemsToMove.AddRange(Instance.GetItemsOnPosition(Item.RoomPosition.GetVector2()));

                    if (ActorsToMove != null)
                    {
                        foreach (RoomActor Actor in ActorsToMove)
                        {
                            if (Actor.IsMoving)
                            {
                                continue;
                            }

                            if (Instance.IsValidStep(Actor.Position.GetVector2(), Item.SquareInFront, true))
                            {
                                Actor.PositionToSet = Item.SquareInFront;
                                Instance.BroadcastMessage(RollerEventComposer.Compose(Actor.Position, new Vector3(
                                    Actor.PositionToSet.X, Actor.PositionToSet.Y,
                                    Instance.GetUserStepHeight(Actor.PositionToSet)), Item.Id, Actor.Id, 0));
                            }
                        }
                    }
                    if (ItemsToMove.Count != 0)
                    {
                        foreach (Item item in ItemsToMove)
                        {
                            if (item == Item)
                            {
                                continue;
                            }

                            if (Item.RoomPosition.X == item.RoomPosition.X && Item.RoomPosition.Y == item.RoomPosition.Y)
                            {
                                Vector2 NewPosition = new Vector2(Item.SquareInFront.X, Item.SquareInFront.Y);
                                int NewRotation = item.RoomRotation;
                                Vector3 FinalizedPosition = Instance.SetFloorItem(Session, item, NewPosition, NewRotation);
                                Vector3 oldpos = item.RoomPosition;

                                if (FinalizedPosition != null)
                                {
                                    item.MoveToRoom(null, Instance.RoomId, FinalizedPosition, NewRotation, string.Empty);
                                    RoomManager.MarkWriteback(item, false);

                                    Instance.RegenerateRelativeHeightmap();
                                    Instance.BroadcastMessage(RoomItemUpdatedComposer.Compose(item));

                                    ItemEventDispatcher.InvokeItemEventHandler(Session, item, Instance, ItemEventType.Moved, 0);
                                    Instance.BroadcastMessage(RollerEventComposer.Compose(oldpos, FinalizedPosition, Item.Id, 0, item.Id));
                                }
                            }
                        }
                    }
                        

                    goto case ItemEventType.InstanceLoaded;

                case ItemEventType.InstanceLoaded:
                case ItemEventType.Placed:

                    Item.RequestUpdate(4);
                    break;
                    }

            return true;
        }
    }
}
