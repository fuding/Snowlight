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
using Snowlight.Specialized;

namespace Snowlight.Game.Items.DefaultBehaviorHandlers
{ 
	public static class WiredHandler
	{
		public static void Register ()
		{
			ItemEventDispatcher.RegisterEventHandler (ItemBehavior.WiredTrigger, new ItemEventHandler (HandleWired));
			ItemEventDispatcher.RegisterEventHandler (ItemBehavior.WiredEffect, new ItemEventHandler (HandleWired));
			ItemEventDispatcher.RegisterEventHandler (ItemBehavior.WiredCondition, new ItemEventHandler (HandleWired));
			ItemEventDispatcher.RegisterEventHandler (ItemBehavior.Switchable, new ItemEventHandler (HandleSwitch));
		}

		private static bool HandleWired (Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
		{
			switch (Event) {
			case ItemEventType.Interact:
				switch (Item.Definition.Behavior) {
				case ItemBehavior.WiredTrigger:
					Session.SendData (WiredFurniTriggerComposer.Compose (Item, Instance));
					break;

				case ItemBehavior.WiredEffect:
					Session.SendData (WiredFurniActionComposer.Compose (Item, Instance));
					break;
				}
				break;
			case ItemEventType.Placed:
				Item.WiredData = Instance.WiredManager.LoadWired (Item.Id, Item.Definition.BehaviorData);
				break;
			case ItemEventType.Removing:
				using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient()) {
					Instance.WiredManager.RemoveWired (Item.Id, MySqlClient);
				}
				
				Instance.WiredManager.DeRegisterWalkItem(Item.Id);
				break;
			case ItemEventType.UpdateTick: 
				if (Item.Definition.Behavior == ItemBehavior.WiredTrigger) {
					switch (WiredTypesUtil.TriggerFromInt (Item.Definition.BehaviorData)) {
					case WiredTriggerTypes.periodically:
						Instance.WiredManager.ExecuteActions (Item, null);
						Item.RequestUpdate (Item.WiredData.Data2);
						break;
					case WiredTriggerTypes.at_given_time:
						Instance.WiredManager.ExecuteActions (Item, null);
						break;
					}								
					return true;
				}
				Item.BroadcastStateUpdate (Instance);
				break;
			}
			return true;
		}
  
		private static bool HandleSwitch (Session Session, Item Item, RoomInstance Instance, ItemEventType Event, int RequestData, uint Opcode)
		{
			if (Event != ItemEventType.Interact) {
				return true;
			}
			RoomActor actor = Instance.GetActor (Session.CharacterId);
			if (actor == null) {
				return true;
			}
			
			foreach (Item item in Instance.GetFloorItems()) {
				if (item.Definition.Behavior != ItemBehavior.WiredTrigger || WiredTypesUtil.TriggerFromInt (item.Definition.BehaviorData) != WiredTriggerTypes.state_changed) {
					continue;
				}
				
				String[] Selected = item.WiredData.Data1.Split ('|');

				if (Selected.Contains (Item.Id.ToString ())) {
					Instance.WiredManager.ExecuteActions (item, actor);
				}
			}
			return true;
		}
       
	} 
} 
