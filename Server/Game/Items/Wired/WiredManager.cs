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
using Snowlight.Communication.Outgoing;

namespace Snowlight.Game.Items.Wired
{
	public enum WiredTriggerTypes
	{
		says_something = 0, 
		walks_on_furni = 1,
		walks_off_furni = 2,
		at_given_time = 3,
		state_changed = 4,
		periodically = 6,
		enter_room = 7
	}

	public enum WiredEffectTypes
	{
		toggle_state = 0,
		match_to_sshot = 3,
		move_rotate = 4,
		show_message = 7,
		teleport_to = 8
	}

	public static class WiredTypesUtil
	{
		public static WiredTriggerTypes TriggerFromInt (int Type)
		{
			switch (Type) {
			default:
			case 0:
				return WiredTriggerTypes.says_something;
			case 1:
				return WiredTriggerTypes.walks_on_furni;
			case 2:
				return WiredTriggerTypes.walks_off_furni;
			case 3:
				return WiredTriggerTypes.at_given_time;
			case 4:
				return WiredTriggerTypes.state_changed;
			case 6:
				return WiredTriggerTypes.periodically;
			case 7:
				return WiredTriggerTypes.enter_room;
			}
		}

		public static WiredEffectTypes EffectFromInt (int Type)
		{
			switch (Type) {
			default:
			case 0:
				return WiredEffectTypes.toggle_state;
			case 3:
				return WiredEffectTypes.match_to_sshot;
			case 4:
				return WiredEffectTypes.move_rotate;
			case 7:
				return WiredEffectTypes.show_message;
			case 8:
				return WiredEffectTypes.teleport_to;
			}
		}
	}

	public class WiredManager
	{
		private Dictionary<uint, WiredData> mWired;
		private RoomInstance mInstance;
		private Dictionary<uint, uint> mRegisteredWalkItems;

		public WiredManager (RoomInstance Instance)
		{
			mInstance = Instance;
			mWired = new Dictionary<uint, WiredData> ();
			mRegisteredWalkItems = new Dictionary<uint, uint> ();
		}

		public WiredData LoadWired (uint ItemId, int Type)
		{
            
			if (!mWired.ContainsKey (ItemId)) {               
				mWired.Add (ItemId, new WiredData (ItemId, Type));
			}
			

			return mWired [ItemId];
		}

		public void RemoveWired (uint ItemId, SqlDatabaseClient MySqlClient)
		{
			if (mWired.ContainsKey (ItemId)) {
				mWired.Remove (ItemId);				
				DeRegisterWalkItems (ItemId);						
				MySqlClient.SetParameter ("id", ItemId);
				MySqlClient.ExecuteNonQuery ("DELETE FROM wired_items WHERE item_id = @id");
			}
		}

		public void SynchronizeDatabase (SqlDatabaseClient MySqlClient)
		{
			foreach (WiredData data in mWired.Values) {
				data.SynchronizeDatabase (MySqlClient);
			}
		}

		public void HandleSave (Session Session, ClientMessage Message)
		{
			uint ItemId = Message.PopWiredUInt32 ();	
			


			if (!mInstance.CheckUserRights (Session) || !mWired.ContainsKey (ItemId)) {
				return;
			}
			
			Item item = mInstance.GetItem (ItemId);
			
			if (item == null) {
				return;
			}
			
			WiredData data = mWired [ItemId];
			
			String Data1 = "";
			int Data2 = 0;
			int Data3 = 0;
			int Data4 = 0;
			int Time = 0;
			String Data5 = "";
			
			Message.PopWiredInt32 ();
			Data2 = Message.PopWiredInt32 ();		
			
			Boolean Simple = true;
			
			if (item.Definition.Behavior == ItemBehavior.WiredEffect) {
				switch (WiredTypesUtil.EffectFromInt (item.Definition.BehaviorData)) {
				case WiredEffectTypes.match_to_sshot:
				case WiredEffectTypes.move_rotate:
				case WiredEffectTypes.teleport_to:
				case WiredEffectTypes.toggle_state:
					Simple = false;				
					break;
				}
			}
			
			if (item.Definition.Behavior == ItemBehavior.WiredTrigger) {
				switch (WiredTypesUtil.TriggerFromInt (item.Definition.BehaviorData)) {
				case WiredTriggerTypes.state_changed:
				case WiredTriggerTypes.walks_off_furni:
				case WiredTriggerTypes.walks_on_furni:
					Simple = false;				
					break;
				}
			}
			
			if (!Simple) {
				Data3 = Message.PopWiredInt32 ();
				
				if (item.Definition.Behavior == ItemBehavior.WiredEffect && WiredTypesUtil.EffectFromInt (item.Definition.BehaviorData) == WiredEffectTypes.match_to_sshot) {
					Data4 = Message.PopWiredInt32 ();
				}
				
				Message.PopString ();
				int c = Message.PopWiredInt32 ();
				for (int i = 0; i<c; i++) {
					uint tmp = Message.PopWiredUInt32 ();
					if (mInstance.GetItem (tmp) == null) {
						continue;
					}
					if (tmp != 0) {
						Data1 += "" + tmp.ToString () + "|";
					}
				}

				Time = Message.PopWiredInt32 ();
			} else {
				Data1 = Message.PopString ();  
				Data3 = Message.PopWiredInt32 ();
			}			
			
			
			if (item.Definition.Behavior == ItemBehavior.WiredEffect) {
				switch (WiredTypesUtil.EffectFromInt (item.Definition.BehaviorData)) {
				case WiredEffectTypes.match_to_sshot:
					String[] Selected = Data1.Split ('|');
					
					foreach (String ItemIdS in Selected) {
						uint SelectedItemId;
						uint.TryParse (ItemIdS, out SelectedItemId);
						Item Item = mInstance.GetItem (SelectedItemId);
						if (Item == null) {
							continue;
						}
						
						Data5 += Item.Id + "#" + Item.RoomPosition.ToString () + "#" + Item.RoomRotation + "#" + Item.Flags + "+";
					}
					break;
				}
			}

			if (data.Data1 == Data1 && data.Data2 == Data2 && data.Data3 == Data3 && data.Data4 == Data4 && data.Time == Time && data.Data5 == Data5) {
				return;
			}					
               				                        
			using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient()) {
                    
				data.Data1 = Data1;
				data.Data2 = Data2; 
				data.Data3 = Data3;
				data.Data4 = Data4;
				data.Data5 = Data5;
				data.Time = Time;
				data.SynchronizeDatabase (MySqlClient);           
			}
			
			if (item.Definition.Behavior == ItemBehavior.WiredTrigger) {
				switch (WiredTypesUtil.TriggerFromInt (item.Definition.BehaviorData)) {
				case WiredTriggerTypes.at_given_time:
					item.RequestUpdate (Data2);
					break;
				case WiredTriggerTypes.walks_on_furni:
				case WiredTriggerTypes.walks_off_furni:
					DeRegisterWalkItems (item.Id);
					RegisterWalkItems (item.Id);
					break;
				}				
			}	
						
		}
		
		public uint GetRegisteredWalkItem (uint Id)
		{   
			if (mRegisteredWalkItems.ContainsKey (Id)) {
				return mRegisteredWalkItems [Id];
			}			
			return 0;
		}
		
		public void RegisterWalkItems (uint ItemId)
		{
		
			String[] Selected = mWired [ItemId].Data1.Split ('|');
			
			foreach (String ItemIdS in Selected) {
				uint Id;
				uint.TryParse (ItemIdS, out Id);
				Item check = mInstance.GetItem (Id);
				if (check == null) {
					continue;								
				}
				
				if (!mRegisteredWalkItems.ContainsKey (Id)) {
					mRegisteredWalkItems.Add (Id, ItemId);
				}								
			}
		}
		
		private void DeRegisterWalkItems (uint ItemId)  // DeRegister by WiredItem
		{
			if (!mRegisteredWalkItems.ContainsValue (ItemId)) {
				return;
			}
			
			List<uint> ToRemove = new List<uint> ();
			foreach (uint Id in mRegisteredWalkItems.Keys) {
				if (mRegisteredWalkItems [Id] == ItemId) {
					ToRemove.Add (Id);			
				}
			}
			
			foreach (uint Id in ToRemove) {
				if (mRegisteredWalkItems.ContainsKey (Id)) {
					mRegisteredWalkItems.Remove (Id);			
				}				
			}
		}
		
		public void DeRegisterWalkItem (uint Id)  // Deregister by Walkable Item
		{  
			if (mRegisteredWalkItems.ContainsKey (Id)) {
				mRegisteredWalkItems.Remove (Id);
			}
		}
		
		public void HandleEnterRoom (RoomActor Actor)
		{
			foreach (WiredData data in mWired.Values) {
				Item Item = mInstance.GetItem (data.ItemId);
				if (Item.Definition.Behavior == ItemBehavior.WiredTrigger && WiredTypesUtil.TriggerFromInt (Item.Definition.BehaviorData) == WiredTriggerTypes.enter_room) {
					if (data.Data1 != "" && data.Data1 != Actor.Name) {
						continue;
					}

					Item.DisplayFlags = "1";
					Item.BroadcastStateUpdate (mInstance);
					Item.DisplayFlags = "";
					Item.RequestUpdate (4);

					ExecuteActions (Item, Actor);
				}
			}
		}

		public List<Item> TriggerRequiresActor (int BehaviorData, Vector2 Position)
		{
			List<Item> Items = new List<Item> ();
			
			if (WiredTypesUtil.TriggerFromInt (BehaviorData) != WiredTriggerTypes.periodically) {
				return Items;
			}
			
            
			foreach (Item Item in mInstance.GetItemsOnPosition(Position)) {			
				if (Item.Definition.Behavior != ItemBehavior.WiredEffect) {
					continue;
				}
				if (WiredTypesUtil.EffectFromInt (Item.Definition.BehaviorData) == WiredEffectTypes.show_message) {
					Items.Add (Item);
				}
			}

			return Items;
		}
		
		public List<Item> ActionRequiresActor (int BehaviorData, Vector2 Position)
		{
			List<Item> Items = new List<Item> ();
			
			if (WiredTypesUtil.EffectFromInt (BehaviorData) != WiredEffectTypes.show_message) {
				return Items;
			}
			
			foreach (Item Item in mInstance.GetItemsOnPosition(Position)) {	
				if (Item.Definition.Behavior != ItemBehavior.WiredTrigger) {
					continue;
				}
				if (WiredTypesUtil.TriggerFromInt (Item.Definition.BehaviorData) == WiredTriggerTypes.periodically) {
					Items.Add (Item);
				}						
			}
			

			return Items;
		}

		public bool HandleChat (String Message, RoomActor Actor)
		{
			Boolean doneAction = false;
			foreach (WiredData data in mWired.Values) {
				Item Item = mInstance.GetItem (data.ItemId);
				if (Item.Definition.Behavior == ItemBehavior.WiredTrigger &&
                    WiredTypesUtil.TriggerFromInt (Item.Definition.BehaviorData) == WiredTriggerTypes.says_something &&
                    data.Data1 == Message && (data.Data2 == 0 || data.Data2 == Actor.Id)
                    ) {

					Item.DisplayFlags = "1";
					Item.BroadcastStateUpdate (mInstance);
					Item.DisplayFlags = "2";
					Item.RequestUpdate (4);

					ExecuteActions (Item, Actor);
					doneAction = true;
				}
			}
			return doneAction;
		}
		
		public void ExecuteActions (Item Item, RoomActor Actor)
		{
			Random rnd = new Random ();
			foreach (Item ActionItem in mInstance.GetItemsOnPosition(Item.RoomPosition.GetVector2())) {
				if (ActionItem.Definition.Behavior == ItemBehavior.WiredEffect) {
					ActionItem.DisplayFlags = "1";
					ActionItem.BroadcastStateUpdate (mInstance);
					ActionItem.DisplayFlags = "2";
					ActionItem.RequestUpdate (4);

					switch (WiredTypesUtil.EffectFromInt (ActionItem.Definition.BehaviorData)) {
						#region show_message
					case WiredEffectTypes.show_message:
						if (Actor == null) {
							continue;
						}
						Actor.Whisper (mWired [ActionItem.Id].Data1, 0, true);
						break;
						#endregion
						#region move_rotate
					case WiredEffectTypes.move_rotate:
						if (ActionItem.WiredData.Data2 == 0 && ActionItem.WiredData.Data3 == 0) {
							continue;	
						}
						String[] ItemsToMove = ActionItem.WiredData.Data1.Split ('|');
						foreach (String toMove in ItemsToMove) {
							uint ItemId;
							uint.TryParse (toMove, out ItemId);
							Item Move = mInstance.GetItem (ItemId);
							if (Move == null) {
								continue;
							}
							Vector2 NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y);
							
							switch (ActionItem.WiredData.Data2) {
							case 1:
								switch (rnd.Next (1, 5)) {
								case 1:
									NewPosition = new Vector2 (Move.RoomPosition.X - 1, Move.RoomPosition.Y);
									break;
								case 2:
									NewPosition = new Vector2 (Move.RoomPosition.X + 1, Move.RoomPosition.Y);
									break;
								
								
								case 3:
									NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y + 1);
									break;
								
							
								case 4:
									NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y - 1);
									break;
								}
								break;
							case 2:
								if (rnd.Next (0, 2) == 1) {
									NewPosition = new Vector2 (Move.RoomPosition.X - 1, Move.RoomPosition.Y);
								} else {
									NewPosition = new Vector2 (Move.RoomPosition.X + 1, Move.RoomPosition.Y);
								}
								
								break;
							case 3:							
								if (rnd.Next (0, 2) == 1) {
									NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y - 1);
								} else {
									NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y + 1);
								}
								
								break;
							case 4:
								NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y - 1);
								break;
							case 5:
								NewPosition = new Vector2 (Move.RoomPosition.X + 1, Move.RoomPosition.Y);
								break;
							case 6:
								NewPosition = new Vector2 (Move.RoomPosition.X, Move.RoomPosition.Y + 1);
								break;
							case 7:
								NewPosition = new Vector2 (Move.RoomPosition.X - 1, Move.RoomPosition.Y);
								break;
							}
							
							int NewRotation = Move.RoomRotation;
							
							switch (ActionItem.WiredData.Data3) {
							case 1:							
								NewRotation = NewRotation + 2;
								if (NewRotation == 8) {
									NewRotation = 0;
								}
								break;
								
							case 2:							
								NewRotation = (NewRotation - 2);
								if (NewRotation == -2) {
									NewRotation = 6;
								}
								break;	
							case 3:
								if (rnd.Next (0, 2) == 1) {
									goto case 1;
								} else {
								    goto case 2;	
								}
							}
							
							

							bool IsRotationOnly = (ActionItem.WiredData.Data2 == 0);
							Vector3 FinalizedPosition = mInstance.SetFloorItem (null, Move, NewPosition, NewRotation);

							if (FinalizedPosition != null) {
								Move.MoveToRoom (null, mInstance.RoomId, FinalizedPosition, NewRotation, "");
								RoomManager.MarkWriteback (Move, false);         

								mInstance.RegenerateRelativeHeightmap ();
								mInstance.BroadcastMessage (RoomItemUpdatedComposer.Compose (Move));

								ItemEventDispatcher.InvokeItemEventHandler (null, Move, mInstance, ItemEventType.Moved, IsRotationOnly ? 1 : 0);

							}
						}
						break;
						#endregion
						#region match_to_sshot
					case WiredEffectTypes.match_to_sshot:
						String[] Selected = ActionItem.WiredData.Data5.Split ('+');
						foreach (String FullData in Selected) {
							
							if (!FullData.Contains ('#')) {
								continue;
							}
							
							String[] Data = FullData.Split ('#');
							if (Data.Length != 4) {
								continue;
							}
							
							uint Id = uint.Parse (Data [0]);
							String[] Position = Data [1].Split ('|');
							int Rotation = int.Parse (Data [2]);
							String Flags = Data [3];												
							
							int X = int.Parse (Position [0]);
							int Y = int.Parse (Position [1]);
							uint Z = uint.Parse (Position [2]);
							
							Item AffectedItem = mInstance.GetItem (Id);
							
							if (AffectedItem == null) {
								continue;
							}
							
							Boolean IsRotationOnly = (X == AffectedItem.RoomPosition.X && Y == AffectedItem.RoomPosition.Y && Z == AffectedItem.RoomPosition.Z);
													
							Vector2 NewPosition = new Vector2 (X, Y);
							
							if (ActionItem.WiredData.Data2 == 1) {
								AffectedItem.Flags = Flags;
								AffectedItem.DisplayFlags = Item.Flags;
								AffectedItem.BroadcastStateUpdate (mInstance);
							}
							
							if (ActionItem.WiredData.Data3 == 0) {
								Rotation = AffectedItem.RoomRotation;
							}
							
							if (ActionItem.WiredData.Data4 == 0) {
								NewPosition = AffectedItem.RoomPosition.GetVector2 ();
							}
							
							if (ActionItem.WiredData.Data4 == 1 || ActionItem.WiredData.Data3 == 1) {
								Vector3 FinalizedPosition = mInstance.SetFloorItem (null, AffectedItem, NewPosition, Rotation);
								AffectedItem.MoveToRoom (null, mInstance.RoomId, FinalizedPosition, Rotation, "");													
							
								RoomManager.MarkWriteback (AffectedItem, false);         

								mInstance.RegenerateRelativeHeightmap ();
								mInstance.BroadcastMessage (RoomItemUpdatedComposer.Compose (AffectedItem));								        	
							
								ItemEventDispatcher.InvokeItemEventHandler (null, AffectedItem, mInstance, ItemEventType.Moved, IsRotationOnly ? 1 : 0);
							} else if (ActionItem.WiredData.Data2 == 1) {
								RoomManager.MarkWriteback (AffectedItem, true);  
							}
						}
						break;
						#endregion
					case WiredEffectTypes.teleport_to:
						if (Actor == null) {
							continue;
						}
						
						String[] Selected2 = ActionItem.WiredData.Data1.Split ('|');
						String ItemIdS = Actor.FurniOnId.ToString ();							
						
						while (Actor.FurniOnId.ToString() == ItemIdS) {
							ItemIdS = Selected2 [rnd.Next (0, Selected2.Length)];
						}
						
						uint ItemId2;
						uint.TryParse (ItemIdS, out ItemId2);
						Item AffectedItem2 = mInstance.GetItem (ItemId2);
						if (AffectedItem2 == null) {
							continue;
						}						
											
						Actor.PositionToSet = AffectedItem2.RoomPosition.GetVector2 ();
						Actor.UpdateNeeded = true;
						break;
											
					case WiredEffectTypes.toggle_state:
						String[] Selected3 = ActionItem.WiredData.Data1.Split ('|');
						
						foreach (String ItemIdS2 in Selected3) {
							uint ItemId3;
							uint.TryParse (ItemIdS2, out ItemId3);
							Item AffectedItem3 = mInstance.GetItem (ItemId3);
							if (AffectedItem3 == null) {
								continue;
							}	
							
							int CurrentState = 0;
							int.TryParse (AffectedItem3.Flags, out CurrentState);

							int NewState = CurrentState + 1;

							if (CurrentState < 0 || CurrentState >= (AffectedItem3.Definition.BehaviorData - 1)) {
								NewState = 0;
							}

							if (CurrentState != NewState) {
								AffectedItem3.Flags = NewState.ToString ();
								AffectedItem3.DisplayFlags = AffectedItem3.Flags;

								RoomManager.MarkWriteback (AffectedItem3, true);

								AffectedItem3.BroadcastStateUpdate (mInstance);
							}
						}
						break;
					}
				}
			}
		}
	}
}
