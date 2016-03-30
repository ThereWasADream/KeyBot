using SteamKit2;
using SteamTrade;
using System;
using System.Text;
using SteamTrade.TradeWebAPI;
using SteamTrade.Exceptions;
using SteamTrade.TradeOffer;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Timers;
using SteamBot.SteamGroups;
using System.Linq;

namespace SteamBot
{
	public class KeyUserHandler : UserHandler
	{
		private const string BotVersion = "3.2.0";
		public TF2Value UserMetalAdded, NonTradeInventoryMetal, InventoryMetal, BotMetalAdded, ExcessRefined, KeysToScrap, AdditionalRefined, ChangeAdded, LeftoverMetal;
		public static TF2Value SellPricePerKey = TF2Value.FromRef(18.22); //high
		public static TF2Value BuyPricePerKey = TF2Value.FromRef(17.88); //low

		int KeysCanBuy, NonTradeKeysCanBuy, ValidateMetaltoKey, PreviousKeys, UserKeysAdded, BotKeysAdded, InventoryKeys, NonTradeInventoryKeys, IgnoringBot, ScamAttempt, NonTradeScrap, Scrap, ScrapAdded, NonTradeReclaimed, Reclaimed, ReclaimedAdded, NonTradeRefined, Refined, RefinedAdded, InvalidItem, NumKeys, TradeFrequency;
        double Item;
        ulong Slots;
		bool InventoryFailed, HasErrorRun, ChooseDonate, GaveChange, ChangeValidate, HasNonTradeCounted, HasCounted, WasBuying;

		System.Timers.Timer InviteMsgTimer = new System.Timers.Timer(2000);
		System.Timers.Timer CraftCheckTimer = new System.Timers.Timer(100);
        System.Timers.Timer ConfirmationTimer = new System.Timers.Timer(60000);
		System.Timers.Timer Cron = new System.Timers.Timer(1000);

		public KeyUserHandler(Bot bot, SteamID sid)
			: base(bot, sid)
		{
		}

		public override bool OnGroupAdd()
		{
			return false;
		}

		public override void OnLoginCompleted()
		{
			Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
            Bot.AcceptAllMobileTradeConfirmations();
			CraftCheckTimer.Elapsed += new ElapsedEventHandler(OnCraftCheckTimerElapsed);
			CraftCheckTimer.Enabled = true;
			Cron.Elapsed += new ElapsedEventHandler(OnCron);
			Cron.Enabled = true;
			TradeFrequency = 12;
		}

		public override bool OnFriendAdd()
		{
			Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID + ") added me!");
			InviteMsgTimer.Elapsed += new ElapsedEventHandler(OnInviteTimerElapsed);
			InviteMsgTimer.Enabled = true;
			return true;
		}

		public bool InFriendsList(SteamID sid)
		{
			int friendCount = Bot.SteamFriends.GetFriendCount();
			for (int x = 0; x < friendCount; x++)
			{
				SteamID steamIdFriend = Bot.SteamFriends.GetFriendByIndex(x);
				if (steamIdFriend.Equals(sid))
				{
					return true;
				}
			}
			return false;
		}

		private void OnInviteTimerElapsed(object source, ElapsedEventArgs e)
		{
			SendChatMessage("Thanks for adding me, let's trade! Just trade me, and add your keys or metal to begin! Type \"commands\" for a list of useful commands.");
			Bot.Log.Success("Sent welcome message.");
			InviteMsgTimer.Enabled = false;
		}

		private void OnCron(object source, ElapsedEventArgs e)
		{
			Cron.Interval = 10800000;
            CountInventory(false);
            double FullCheck = Item / Slots;
            var HeadAdmin = new SteamID(Bot.Admins.First());
            if (FullCheck > 0.9)
            {
                Bot.SteamFriends.SendChatMessage(HeadAdmin, EChatEntryType.ChatMsg, "My inventory is getting full, boss.");
            }
			if (TradeFrequency < 12)
			{
				TradeFrequency++;
			}
		}

		public override void OnFriendRemove()
		{
			Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID + ") removed me!");
		}

		public override void OnMessage(string message, EChatEntryType type)
		{
			message = message.ToLower();
            if (message == "price")
            {
                SendChatMessage("I buy keys for " + BuyPricePerKey.ToRefString() + ", and sell keys for " + SellPricePerKey.ToRefString() + ".");
            }
            else if ((message.Contains("love") || message.Contains("luv") || message.Contains("<3")) && (message.Contains("y") || message.Contains("u")))
            {
                if (message.Contains("do"))
                {
                    SendChatMessage("I love you lots. <3");
                }
                else
                {
                    SendChatMessage("I love you too!");
                }
            }
            else if (message.Contains("<3"))
            {
                SendChatMessage("<3");
            }
            else if (message.Contains("thank"))
            {
                SendChatMessage("You're welcome!");
            }
            else if (message == "donate")
            {
                SendChatMessage("Please type that command into the trade window. And thanks!");
            }
            else if (message == "buy")
            {
                SendChatMessage("That's not a command. Please trade to begin and add keys or metal. Type 'help' for more info.");
            }
            else if (message == "sell")
            {
                SendChatMessage("That's not a command. Please trade to begin and add keys or metal. Type 'help' for more info.");
            }
            else if (message == "trade")
            {
                SendChatMessage("That's not a command. Please trade to begin and add keys or metal. Type 'help' for more info.");
            }
            else if (message.Contains("stupid") || message.Contains("fuck") || message.Contains("can't") || message.Contains("cant") || message.Contains("what"))
            {
                SendChatMessage("Hey, do you need help? Type \"help\" for more info. Or else, are trades failing? Chances are that Steam is having the issues -- not me!");
            }
            else if (message.Contains("help"))
            {
                SendChatMessage("Type \"price\" to see my current prices. Type \"stock\" to see what I have. Then trade me, and put up your keys or metal and I will add my keys or exact price in metal automatically. Type \"commands\" for a list of commands.");
            }
            else if (message == "commands")
            {
                SendChatMessage("Type \"price\" to see my current prices. Type \"stock\" to see what I have. Type \"confirm\" to have your trades confirmed if the bot does not do so. Type \"info\" for more information on this bot via a link to the bot's TF2Outpost page. Type \"help\" for a guide on how to trade with the bot. Type \"group\" to be invited to the group (WIP). To donate, type \"donate\" in the trade window!");
            }
            else if (message == "info")
            {
                SendChatMessage("More information about this bot can be found here: http://steamcommunity.com/groups/NarthsBots .");
            }
            else if (message == "group")
            {
                SendChatMessage("Coming soon...invite to group feature. But here is the group: http://steamcommunity.com/groups/NarthsBots .");
            }
            else if (message == "confirm")
            {
                Bot.AcceptAllMobileTradeConfirmations();
                SendChatMessage("Confirming all my trades. Message from owner: If this does not work, Steam failed to send a confirmation. Redo your trade. This is out of my control.");
            }
			else if (message == "stock" || message == "inventory")
			{
				HasNonTradeCounted = false;
				CountInventory(true);
				if (InventoryFailed)
				{
					SendChatMessage("I failed to start your trade because Steam is down. Try again later when Steam is working.");
					Bot.Log.Warn("I notified user of failure to count inventory due to failed Web Request.");
				}
				if (NonTradeInventoryKeys == 0 && HasNonTradeCounted)
				{
					SendChatMessage("I don't have any keys to sell at the moment.");
					SendChatMessage("I can afford to buy " + NonTradeKeysCanBuy + " key(s). I am buying for " + BuyPricePerKey.ToRefString() + " each.");
				}
				else if (NonTradeInventoryKeys > 0 && HasNonTradeCounted)
				{
					if (NonTradeInventoryMetal < BuyPricePerKey)
					{
						SendChatMessage("Currently I have " + NonTradeInventoryKeys + " key(s) for " + SellPricePerKey.ToRefString() + " each, but unfortunately I can not afford to buy keys from you at this time.");
					}
					else
					{
						SendChatMessage("Currently I have " + NonTradeInventoryKeys + " key(s) for " + SellPricePerKey.ToRefString() + " each.");
						SendChatMessage("I can afford to buy " + NonTradeKeysCanBuy + " key(s). I am buying for " + BuyPricePerKey.ToRefString() + " each.");
					}
				}
				else
				{
					SendChatMessage("Oh no, I'm broke!");
				}
				if (NonTradeKeysCanBuy > 0 && BuyPricePerKey.ScrapPart > NonTradeScrap && BuyPricePerKey.ReclaimedPart > NonTradeReclaimed + (NonTradeScrap - BuyPricePerKey.ScrapPart) / 3)
				{
					SendChatMessage("I do not have exact change! Open and close a trade with me if you'd like me to make scrap/rec for you.");
					Bot.Log.Warn("I warned a user of a lack of scrap or rec during a stock request.");
				}
			}
			else if (IsAdmin)
			{
				if (message.StartsWith(".sell"))
				{
					double NewSellPrice = 0.0;
					SendChatMessage("Current selling price: " + SellPricePerKey.ToRefString() + ".");
					SellPricePerKey = TF2Value.Zero;
					if (message.Length >= 6)
					{
						double.TryParse(message.Substring(5), out NewSellPrice);
						Bot.Log.Success("Admin has requested that I set the new selling price to " + NewSellPrice + " ref.");
						SellPricePerKey = TF2Value.FromRef(NewSellPrice);
						SendChatMessage("Setting new selling price to: " + SellPricePerKey.ToRefString() + ".");
						Bot.Log.Success("Successfully set new price.");
					}
					else
					{
						SendChatMessage("I need more arguments. Current selling price: " + SellPricePerKey.ToRefString() + ".");
					}
				}
				else if (message.StartsWith(".buy"))
				{
					double NewBuyPrice = 0.0;
					SendChatMessage("Current buying price: " + BuyPricePerKey.ToRefString() + ".");
					BuyPricePerKey = TF2Value.Zero;
					if (message.Length >= 5)
					{
						double.TryParse(message.Substring(4), out NewBuyPrice);
						Bot.Log.Success("Admin has requested that I set the new buying price to " + NewBuyPrice + " ref.");
						BuyPricePerKey = TF2Value.FromRef(NewBuyPrice);
						SendChatMessage("Setting new buying price to: " + BuyPricePerKey.ToRefString() + ".");
						Bot.Log.Success("Successfully set new price.");
					}
					else
					{
						SendChatMessage("I need more arguments. Current buying price: " + BuyPricePerKey.ToRefString() + ".");
					}
				}
				else if (message.StartsWith(".play"))
				{
					if (message.Length >= 7)
					{
						if (message.Substring(6) == "tf2")
						{
							Bot.SetGamePlaying(440);
							Bot.Log.Success("Successfully simulated in-game status for TF2.");
						}
						Bot.SetGamePlaying(0);
						Bot.Log.Success("Exited game simulation.");
					}
				}
				else if (message.StartsWith(".removefriend") || message.StartsWith(".deletefriend"))
				{
					if (message.Substring(14) == "all")
					{
						int friendCount = Bot.SteamFriends.GetFriendCount();
						for (int x = 0; x < friendCount; x++)
						{
							SteamID DeletingFriendID = Bot.SteamFriends.GetFriendByIndex(x);
							if (!Bot.Admins.Contains(DeletingFriendID))
							{
								Bot.SteamFriends.RemoveFriend(DeletingFriendID);
							}
							else
							{
								Bot.Log.Success("Skipped Admin " + Bot.SteamFriends.GetFriendPersonaName(DeletingFriendID) + ".");
							}
						}
						Bot.Log.Success("Deleted all friends.");
					}
					else
					{
						string FriendID;
						FriendID = message.Substring(14);
						FriendDelete(FriendID);
					}
				}
				else if (message == ".canceltrade")
				{
                    if (Bot.CurrentTrade != null)
                    {
                        SteamID LastTradedSID = Trade.OtherSID;
                        Trade.CancelTrade();
                        Bot.SteamFriends.SendChatMessage(LastTradedSID, EChatEntryType.ChatMsg, "Trade forcefully closed. Please retry as soon as you are ready to trade instead of whatever you were doing.");
                        Bot.Log.Warn("Trade with " + Bot.SteamFriends.GetFriendPersonaName(LastTradedSID) + " cancelled.");
                        IgnoringBot += 4;
                        Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
                    }
                    else
                    {
                        SendChatMessage("There is no current trade to cancel.");
                    }
				}
				else if (message == ".auth")
				{
					Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, Bot.SteamGuardAccount.GenerateSteamGuardCode());
					Bot.Log.Warn("Generated code:" + Bot.SteamGuardAccount.GenerateSteamGuardCode() + ".");
				}
			}
			else
			{
				SendChatMessage(Bot.ChatResponse);
			}
		}

		public override bool OnTradeRequest()
		{
			Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID + ") has requested to trade with me!");
			if (InFriendsList(OtherSID))
			{
				for (int retries = 0; retries < 5; retries++)
				{
					try
					{
						if (!OtherWebClients.IsMarked(OtherSID.Render(false)))
						{
                            Bot.Log.Success("Began trade!");
						    return true;
						}
                        else
                        {
                            Bot.Log.Error("Declined trade, user is marked as a scammer on SteamRep.");
                            SendChatMessage("I'm sorry, it looks like you are marked as a scammer on SteamRep. Per TF2 Outpost rules, I cannot trade with you. Nothing personal! If this is an error, please appeal on SteamRep. Thank you!");
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(50);
                        Bot.Log.Error("Failed to accept trade, retrying.");
                    }
                }
				Bot.Log.Error("Failed to start trade after 5 retries.");
				Bot.Log.Error("SteamRep may be down. Beginning trade anyway.");
				return true;
			}
			else
			{
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " tried to trade me, though they are not on my friend's list.");
				return false;
			}
		}

		public override void OnTradeError(string error)
		{
			if (!HasErrorRun)
			{
                if (error.Contains("cancelled by other user"))
                {
                    Bot.Log.Warn(error);
                    SendChatMessage("Trade cancelled. Thanks for your time. If you closed the trade due to the \"There was an error...\" message, wait a few seconds before closing the trade; the error often goes away.");
                }
                else if (error.Contains("cancelled by bot"))
                {
                    Bot.Log.Warn(error);
                }
                else if (error.Contains("expired because") && error.Contains("timed out"))
                {
                    Bot.Log.Error(error);
                    SendChatMessage("Steam reports that your trade timed out. This means Steam is slow and lagging. Try again later.");
                }
                else if (error.Contains("failed unexpectedly"))
                {
                    Bot.Log.Error(error);
                    SendChatMessage("Steam reports that the \"trade failed unexpectedly.\" This is likely due to problems with Steam servers.");
                }
				else
				{
                    Bot.Log.Error(error);
                    SendChatMessage("Steam reports that the trade ended because it of an unknown Steam error. Try again when Steam is working better.");
				}
				HasErrorRun = true;
			}
			Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
		}

		public override void OnTradeTimeout()
		{
			SendChatMessage("Sorry, but you were either AFK or took too long and the trade was canceled.");
			if (WasBuying)
			{
				SendChatMessage("Type \"price\" if you don't know how much to add for a key.");
			}
			Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " was kicked because he or she was AFK or took too long.");
			Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
		}

		public override void OnTradeInit()
		{
			if (ScamAttempt > 9 || IgnoringBot > 12)
			{
				SendChatMessage("You have had several warnings about properly trading with me. Add me again when you're ready.");
				FriendDelete(Trade.OtherSID.ToString());
				Trade.CancelTrade();
				Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
			}
			ReInit();
			SendTradeMessage("Welcome to Narth's keybanking bot " + BotVersion + ".");
			SendTradeMessage("To use this bot, just add your metal or keys,");
			SendTradeMessage("and I will automatically add keys or metal when you have put up enough.");
			SendTradeMessage("Type 'help' for more information.");
			TradeCountInventory(true);
			if (InventoryFailed)
			{
				SendChatMessage("I failed to start your trade because Steam is down. Try again later when Steam is working.");
				Bot.Log.Warn("I notified user of failure to count inventory due to failed Web Request.");
			}
			if (KeysCanBuy > 0 && BuyPricePerKey.ScrapPart > Scrap && BuyPricePerKey.ReclaimedPart > Reclaimed + (Scrap - BuyPricePerKey.ScrapPart) / 3)
			{
				SendTradeMessage("I do not have exact change!");
				SendTradeMessage("Close this trade if you'd like me to make scrap/rec for you.");
				Bot.Log.Warn("I warned user of a lack of scrap or rec during a trade initiation.");
			}
			if (InventoryKeys == 0)
			{
				SendTradeMessage("I don't have any keys to sell right now.");
				SendTradeMessage("I am buying keys for " + BuyPricePerKey.ToRefString() + ".");
			}
			else if (InventoryMetal < BuyPricePerKey)
			{
				SendTradeMessage("I don't have enough metal to buy keys.");
				SendTradeMessage("I am selling keys for " + SellPricePerKey.ToRefString() + ".");
			}
			else
			{
				SendTradeMessage("I am currently buying keys for " + BuyPricePerKey.ToRefString() + ",");
				SendTradeMessage("and selling keys for " + SellPricePerKey.ToRefString() + ".");
			}
			Bot.SteamFriends.SetPersonaState(EPersonaState.Busy);
		}

		public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			if (schemaItem != null && inventoryItem != null)
			{
				Schema.Item item = Trade.CurrentSchema.GetItem((int)schemaItem.Defindex);
				if (inventoryItem.AppId != 440)
				{
					SendTradeMessage("That's an item from the wrong game!");
				}
				if (!HasCounted)
				{
					SendTradeMessage("I haven't finished counting my inventory yet.");
					SendTradeMessage("Please remove any items you added, and then re-add them or there could be errors.");
				}
				else if (InvalidItem > 2)
				{
					Trade.CancelTrade();
					IgnoringBot++;
					SendChatMessage("I am used for buying and selling keys only. I can only accept metal or keys as payment.");
					Bot.Log.Warn("Booted user for adding invalid items.");
					Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
				}
				else if (IgnoringBot > 4)
				{
					Trade.CancelTrade();
					SendChatMessage("It would appear that you haven't checked my inventory. I either do not have enough metal, or do not have enough keys to complete your trade.");
					Bot.Log.Warn("Booted user for ignoring me.");
					Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
				}
				else if (ScamAttempt > 3)
				{
					Trade.CancelTrade();
					SendChatMessage("The trade isn't even again. Please be more careful when trading or I will delete you.");
					Bot.Log.Warn("Booted user for trying to scam me.");
					Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
				}
				else if (item.Defindex == 5000)
				{
					UserMetalAdded += TF2Value.Scrap;
					Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added: " + item.ItemName);
					WasBuying = true;
				}
				else if (item.Defindex == 5001)
				{
					UserMetalAdded += TF2Value.Reclaimed;
					Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added: " + item.ItemName);
					WasBuying = true;
				}
				else if (item.Defindex == 5002)
				{
					UserMetalAdded += TF2Value.Refined;
					Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added: " + item.ItemName);
					WasBuying = true;
				}
				else if (item.Defindex == 5021)
				{
					UserKeysAdded++;
					KeysToScrap = UserKeysAdded * BuyPricePerKey;
					WasBuying = false;
					Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added: " + item.ItemName);
					if (!ChooseDonate)
					{
						if (InventoryMetal < KeysToScrap)
						{
							SendTradeMessage("I only have " + InventoryMetal.ToRefString() + ".");
							SendTradeMessage("I pay " + BuyPricePerKey.ToRefString() + " per key. You need to remove some keys.");
							Bot.Log.Warn("I don't have enough metal for the user.");
							IgnoringBot++;
						}
						else
						{
							SendTradeMessage("You have given me " + UserKeysAdded + " key(s).");
							SendTradeMessage("I will give you " + KeysToScrap.ToRefString() + ".");
							bool RefIsDone = false;
							bool RecIsDone = false;
							bool ScrapIsDone = false;
							bool DoneAddingMetal = false;
							int WhileLoop = 0;
							while (!DoneAddingMetal)
							{
								WhileLoop++;
								if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Refined && !RefIsDone)
								{
									if (Refined > 0)
									{
										Trade.AddItemByDefindex(5002);
										Bot.Log.Warn("I added Refined Metal.");
										BotMetalAdded += TF2Value.Refined;
										Refined--;
										RefinedAdded++;
									}
									else
									{
										RefIsDone = true;
									}
								}
								else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) < TF2Value.Refined && !RefIsDone)
								{
									RefIsDone = true;
								}
								else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Reclaimed && RefIsDone && !RecIsDone)
								{
									if (ReclaimedAdded == 2 && Refined > 0)
									{
										for (int removed = 0; removed < 2; removed++)
										{
											Trade.RemoveItemByDefindex(5001);
											Bot.Log.Warn("I removed Reclaimed Metal.");
											BotMetalAdded -= TF2Value.Reclaimed;
											Reclaimed++;
											ReclaimedAdded--;
										}
										if (Refined > 0)
										{
											Trade.AddItemByDefindex(5002);
											Bot.Log.Warn("I added Refined Metal.");
											BotMetalAdded += TF2Value.Refined;
											Refined--;
											RefinedAdded++;
										}
										else
										{
											RecIsDone = true;
										}
									}
									else if (Reclaimed > 0)
									{
										Trade.AddItemByDefindex(5001);
										Bot.Log.Warn("I added Reclaimed Metal.");
										BotMetalAdded += TF2Value.Reclaimed;
										Reclaimed--;
										ReclaimedAdded++;
									}
									else
									{
										RecIsDone = true;
									}
								}
								else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) < TF2Value.Reclaimed && !RecIsDone)
								{
									RecIsDone = true;
								}
								else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Scrap && RefIsDone && RecIsDone && !ScrapIsDone)
								{
									if (ScrapAdded == 2 && Reclaimed > 0)
									{
										for (int removed2 = 0; removed2 < 2; removed2++)
										{
											Trade.RemoveItemByDefindex(5000);
											Bot.Log.Warn("I removed Scrap Metal.");
											BotMetalAdded -= TF2Value.Scrap;
											Scrap++;
											ScrapAdded--;
										}
										if (Reclaimed > 0)
										{
											Trade.AddItemByDefindex(5001);
											Bot.Log.Warn("I added Reclaimed Metal.");
											BotMetalAdded += TF2Value.Reclaimed;
											Reclaimed--;
											ReclaimedAdded++;
										}
									}
									else if (Scrap > 0)
									{
										Trade.AddItemByDefindex(5000);
										Bot.Log.Warn("I added Scrap Metal.");
										BotMetalAdded += TF2Value.Scrap;
										Scrap--;
										ScrapAdded++;
									}
									else
									{
										ScrapIsDone = true;
									}
								}
								if (BotMetalAdded == KeysToScrap)
								{
									SendTradeMessage("Added enough metal (" + BotMetalAdded.ToRefString() + ").");
									Bot.Log.Success("Gave user enough metal!");
									DoneAddingMetal = true;
								}
								else if (RefIsDone && RecIsDone && ScrapIsDone && WhileLoop > 50)
								{
									SendTradeMessage("Sorry, but I don't have enough scrap or rec to give you!");
									SendTradeMessage("Please close the trade and try again. I craft more scrap and rec every " + (CraftCheckTimer.Interval / 1000).ToString() + " seconds.");
									Bot.Log.Warn("Couldn't add enough scrap or rec for the user!");
									IgnoringBot++;
									DoneAddingMetal = true;
								}
								else if (WhileLoop > 100)
								{
									SendTradeMessage("Error: I cannot add metal at the moment. Either Steam is broken or I am broken!");
									Bot.Log.Error("Error: Bot could not add metal to a trade.");
									break;
								}
							}
						}
					}
				}
				else
				{
					SendTradeMessage("Sorry, I don't accept " + item.ItemName + "!");
					SendTradeMessage("I only accept metal/keys! Please remove it from the trade to continue.");
					Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added:  " + item.ItemName);
					InvalidItem++;
				}
				if (!ChooseDonate && UserMetalAdded.ScrapTotal % SellPricePerKey.ScrapTotal >= 0.0 && UserMetalAdded.ScrapTotal > 0.0)
				{
					TF2Value excess;
					NumKeys = UserMetalAdded.GetPriceUsingItem(SellPricePerKey, out excess);
					TF2Value MetalForNextKey = SellPricePerKey - excess;
					if (NumKeys > 0 && NumKeys != PreviousKeys)
					{
						SendTradeMessage("You put up enough metal for " + NumKeys + " key(s).");
						SendTradeMessage("Add " + MetalForNextKey.ToRefString() + " to buy a key/another key.");
						int AddNumKeys = NumKeys + 1;
						int AddMoreNumKeys = NumKeys + 2;
						TF2Value CurrentKeys = SellPricePerKey * NumKeys;
						TF2Value PlusOneKey = SellPricePerKey * (NumKeys + 1);
						TF2Value PlusTwoKeys = SellPricePerKey * (NumKeys + 2);
						SendTradeMessage("For reference: " + CurrentKeys.ToPartsString(false) + " = " + NumKeys + " key(s),");
						SendTradeMessage(PlusOneKey.ToPartsString(false) + " = " + AddNumKeys + " key(s),");
						SendTradeMessage(PlusTwoKeys.ToPartsString(false) + " = " + AddMoreNumKeys + " key(s)...");
						if (NumKeys > InventoryKeys)
						{
							SendTradeMessage("I only have " + InventoryKeys + " in my backpack.");
							Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " wanted to buy " + NumKeys + " key(s), but I only have " + InventoryKeys + " key(s).");
							SendTradeMessage("Please remove " + excess.ToRefString() + ".");
							NumKeys = InventoryKeys;
							IgnoringBot++;
						}
						for (int count = BotKeysAdded; count < NumKeys; count++)
						{
							Trade.AddItemByDefindex(5021);
							Bot.Log.Warn("I am adding Mann Co. Supply Crate Key.");
							BotKeysAdded++;
						}
						PreviousKeys = NumKeys;
						return;
					}
				}
			}
			else
			{
				SendChatMessage("Steam is currently having trouble getting your inventory. Try again in a minute.");
				Bot.Log.Warn("User was warned of Steam inventory issues.");
				Trade.CancelTrade();
				Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
			}
		}

		public void FriendDelete(string input)
		{
			var EnteredID = Convert.ToUInt64(input);
			var DeleteID = new SteamID(EnteredID);
			if (InFriendsList(DeleteID))
			{
				Bot.SteamFriends.RemoveFriend(DeleteID);
				Bot.Log.Success("Deleted " + Bot.SteamFriends.GetFriendPersonaName(DeleteID) + ".");
                var HeadAdmin = new SteamID(Bot.Admins.First());
                Bot.SteamFriends.SendChatMessage(HeadAdmin, EChatEntryType.ChatMsg, "Deleted " + Bot.SteamFriends.GetFriendPersonaName(DeleteID) + ".");
			}
			else
			{
				Bot.Log.Error("Failed to remove friend. Input was " + EnteredID + ".");
                var HeadAdmin = new SteamID(Bot.Admins.First());
                Bot.SteamFriends.SendChatMessage(HeadAdmin, EChatEntryType.ChatMsg, "Failed to remove friend. Input was " + EnteredID + ".");
			}
		}

		public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			Schema.Item item = Trade.CurrentSchema.GetItem((int)schemaItem.Defindex);
			int WhileLoop = 0;
			if (item.Defindex == 5000)
			{
				UserMetalAdded -= TF2Value.Scrap;
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " removed: " + item.ItemName);
			}
			else if (item.Defindex == 5001)
			{
				UserMetalAdded -= TF2Value.Reclaimed;
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " removed: " + item.ItemName);
			}
			else if (item.Defindex == 5002)
			{
				UserMetalAdded -= TF2Value.Refined;
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " removed: " + item.ItemName);
			}
			else if (item.Defindex == 5021)
			{
				UserKeysAdded--;
				KeysToScrap = UserKeysAdded * BuyPricePerKey;
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " removed: " + item.ItemName);
			}
			else
			{
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " removed: " + item.ItemName);
			}
			if ((double)UserKeysAdded < BotMetalAdded.GetPriceUsingItem(BuyPricePerKey))
			{
				bool RefIsDone = false;
				bool RecIsDone = false;
				bool ScrapIsDone = false;
				bool DoneAddingMetal = false;
				WhileLoop = 0;
				while (!DoneAddingMetal)
				{
					WhileLoop++;
					if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Refined && !RefIsDone)
					{
						if (RefinedAdded > 0)
						{
							Trade.RemoveItemByDefindex(5002);
							Bot.Log.Warn("I removed Refined Metal.");
							BotMetalAdded -= TF2Value.Refined;
							Refined++;
							RefinedAdded--;
						}
						else
						{
							RefIsDone = true;
						}
					}
					else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) < TF2Value.Refined && !RefIsDone)
					{
						RefIsDone = true;
					}
					else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Reclaimed && RefIsDone && !RecIsDone)
					{
						if (ReclaimedAdded > 0)
						{
							Trade.RemoveItemByDefindex(5001);
							Bot.Log.Warn("I removed Reclaimed Metal.");
							BotMetalAdded -= TF2Value.Reclaimed;
							Reclaimed++;
							ReclaimedAdded--;
						}
						else if (RefinedAdded > 0)
						{
							Trade.RemoveItemByDefindex(5002);
							Bot.Log.Warn("I removed Refined Metal.");
							BotMetalAdded -= TF2Value.Refined;
							Refined++;
							RefinedAdded--;
							for (int addareclaimed = 0; addareclaimed < 2; addareclaimed++)
							{
								if (Reclaimed > 0)
								{
									Trade.AddItemByDefindex(5001);
									Bot.Log.Warn("I added Reclaimed Metal.");
									BotMetalAdded += TF2Value.Reclaimed;
									Reclaimed--;
									ReclaimedAdded++;
								}
							}
						}
					}
					else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) < TF2Value.Reclaimed && !RecIsDone)
					{
						RecIsDone = true;
					}
					else if (TF2Value.Difference(BotMetalAdded, KeysToScrap) >= TF2Value.Scrap && RefIsDone && RecIsDone && !ScrapIsDone)
					{
						if (ScrapAdded > 0)
						{
							Trade.RemoveItemByDefindex(5000);
							Bot.Log.Warn("I removed Scrap Metal.");
							BotMetalAdded -= TF2Value.Scrap;
							Scrap++;
							ScrapAdded--;
						}
						else if (ReclaimedAdded > 0)
						{
							Trade.RemoveItemByDefindex(5001);
							Bot.Log.Warn("I removed Reclaimed Metal.");
							BotMetalAdded -= TF2Value.Reclaimed;
							Reclaimed++;
							ReclaimedAdded--;
							for (int addascrap = 0; addascrap < 2; addascrap++)
							{
								if (Scrap > 0)
								{
									Trade.AddItemByDefindex(5000);
									Bot.Log.Warn("I added Scrap Metal.");
									BotMetalAdded += TF2Value.Scrap;
									Scrap--;
									ScrapAdded++;
								}
							}
						}
						else
						{
							ScrapIsDone = true;
						}
					}
					else if (BotMetalAdded == KeysToScrap)
					{
						DoneAddingMetal = true;
						ScrapIsDone = true;
					}
					else if (ScrapIsDone && RecIsDone && RefIsDone)
					{
						ScrapIsDone = false;
						RecIsDone = false;
						RefIsDone = false;
					}
					else if (WhileLoop > 100)
					{
						SendTradeMessage("Error: I cannot remove metal at the moment. Either Steam is broken or I am broken!");
						Bot.Log.Error("Error: Bot could not remove metal from a trade.");
						break;
					}
				}
			}
			WhileLoop = 0;
			while (UserMetalAdded.GetPriceUsingItem(SellPricePerKey) < (double)BotKeysAdded)
			{
				WhileLoop++;
				if (BotKeysAdded > 0)
				{
					Trade.RemoveItemByDefindex(5021);
					Bot.Log.Warn("I removed Mann Co. Supply Crate Key.");
					SendTradeMessage("I removed a key.");
					BotKeysAdded--;
					PreviousKeys = BotKeysAdded;
				}
				else if (WhileLoop > 100)
				{
					SendTradeMessage("Error: I cannot remove the key(s) at the moment. Either Steam is broken or I am broken!");
					Bot.Log.Error("Error: Bot could not remove a key from a trade.");
					ResetTrade(true);
					break;
				}
			}
		}

		public override void OnTradeMessage(string message)
		{
			Log.Info("[TRADE MESSAGE] " + message);
			message = message.ToLower();
			if (message == "donate")
			{
				ChooseDonate = true;
				SendTradeMessage("Thanks a lot! Just put up your items and simply click \"Ready to Trade\" when done!");
				SendTradeMessage("If you want to buy or sell keys again you need to start a new trade with me!");
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " wants to donate!");
			}
			if (message == "help")
			{
				SendTradeMessage("Just add your metal or keys to the trade to begin.");
				SendTradeMessage("If I am not adding anything, I will tell you why.");
				SendTradeMessage("Here's a list of all the commands:");
				SendTradeMessage("donate, price, stock, help, commands");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " asked for help.");
			}
			if (message == "commands")
			{
				SendTradeMessage("Here's a list of all the commands:");
				SendTradeMessage("donate, price, stock, help, commands");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " asked for the commands.");
			}
			if (message == "price")
			{
				SendTradeMessage("Current price to buy a key is " + BuyPricePerKey.ToRefString() + ".");
				SendTradeMessage("Current price to sell a key is " + SellPricePerKey.ToRefString() + ".");
				SendTradeMessage("Currently you've added " + UserMetalAdded.ToRefString() + ".");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " asked for price.");
			}
			if (message == "stock")
			{
				SendTradeMessage("Currently I have " + NonTradeInventoryKeys + " and " + NonTradeInventoryMetal.ToRefString() + ".");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " asked for current stock.");
			}
			if (message == "metal")
			{
				if (UserMetalAdded >= TF2Value.Zero)
				{
					SendTradeMessage("You've currently added " + UserMetalAdded.ToRefString());
				}
				else
				{
					SendTradeMessage("You've currently added " + UserKeysAdded + " keys, worth " + UserMetalAdded.ToItemString(SellPricePerKey, "key") + ".");
				}
			}
		}

		public override void OnTradeReady(bool ready)
		{
			if (!ready)
			{
				Trade.SetReady(false);
				return;
			}
			Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " is ready to trade!");
			if (Validate())
			{
				Bot.Log.Success("Trade successfully validated.");
				Trade.SetReady(true);
			}
			else if (ChangeValidate)
			{
				Bot.Log.Warn("I did not ready in order to make change.");
				GiveChange();
			}
			else
			{
				ResetTrade(false);
			}
		}

		public void ResetTrade(bool message)
		{
			Trade.RemoveAllItems();
			BotKeysAdded = 0;
			BotMetalAdded = TF2Value.Zero;
			ReclaimedAdded = 0;
			ScrapAdded = 0;
			RefinedAdded = 0;
			ChangeAdded = TF2Value.Zero;
			GaveChange = false;
			ChangeValidate = false;
			ChooseDonate = false;
			TradeCountInventory(message);
			SendTradeMessage("I'm sorry, there were errors. Scroll up to read them.");
			Bot.Log.Warn("There were errors and the trade was reset.");
			SendTradeMessage("I have reset the trade. Please try again.");
			Bot.Log.Success("Reset trade.");
		}

		public override void OnTradeAccept()
		{
			if (Validate() || IsAdmin)
			{
				try
				{
					Trade.AcceptTrade();
				}
				catch
				{
					Log.Warn("Trade might have failed.");
					Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
				}
			}
			Log.Success("Trade was successful!");
			if (TradeFrequency > 1)
			{
				TradeFrequency -= 2;
			}
			ScamAttempt = 0;
			IgnoringBot = 0;
			SendChatMessage("I was coded by http://steamcommunity.com/id/Narthalion. Please report all bugs/problems to me! It helps fix issues and make me better.");
            ConfirmationTimer.Elapsed += new ElapsedEventHandler(OnConfirmationTimerElapsed);
            ConfirmationTimer.Enabled = true;
			Bot.SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);
		}

        private void OnConfirmationTimerElapsed(object source, ElapsedEventArgs e)
        {
            ConfirmationTimer.Enabled = false;
            Bot.AcceptAllMobileTradeConfirmations();
        }

		public override void OnTradeAwaitingConfirmation(long tradeOfferID)
		{
            Bot.Log.Warn("Trade ended, awaiting confirmation.");
			SendChatMessage("Please complete any mobile or email confirmations. Type \"confirm\" if the bot does not confirm within 5 minutes.");
            var tradeid = tradeOfferID.ToString();
            Bot.AcceptTradeConfirmation(tradeid);
		}

        public override void OnTradeOfferUpdated(TradeOffer offer)
        {
            switch (offer.OfferState)
            {
                case TradeOfferState.TradeOfferStateAccepted:
                    break;
                case TradeOfferState.TradeOfferStateActive:
                    if (Bot.Admins.Contains(offer.PartnerSteamId))
                    {
                        int WhileLoop = 0;
                        bool success = false;
                        while (!success)
                        {
                            WhileLoop++;
                            TradeOfferAcceptResponse acceptResp = offer.Accept();
                            if (acceptResp.Accepted)
                            {
                                Log.Success("Accepted Admin trade offer successfully.");
                                //Log.Success("Accepted trade offer successfully : Trade ID: " + acceptResp.TradeId);
                                success = true;
                            }
                            else if (WhileLoop > 100)
                            {
                                Bot.Log.Error("Unable to accept Admin trade offer.");
                                break;
                            }
                        }
                    }
                    else
                    {
                        //nothing
                    }
                    break;
                case TradeOfferState.TradeOfferStateNeedsConfirmation:
                    Bot.AcceptAllMobileTradeConfirmations();
                    break;
                case TradeOfferState.TradeOfferStateInEscrow:
                    //Trade is still active but incomplete
                    break;
                case TradeOfferState.TradeOfferStateCountered:
                    Bot.Log.Info("Trade offer {offer.TradeOfferId} was countered");
                    break;
                default:
                    Bot.Log.Info("Trade offer {offer.TradeOfferId} failed");
                    break;
            }
        }

		private void OnCraftCheckTimerElapsed(object source, ElapsedEventArgs e)
		{
			CraftCheckTimer.Interval = TradeFrequency * 300000;
			HasNonTradeCounted = false;
			CountInventory(false);
			bool Complete = false;
			int WhileLoop = 0;
			while (!Complete && WhileLoop < 100)
			{
				WhileLoop++;
				if (NonTradeInventoryMetal < BuyPricePerKey)
				{
					break;
				}
				if (NonTradeRefined < 4)
				{
					break;
				}
				if (NonTradeReclaimed > 15 && HasNonTradeCounted)
				{
					Bot.Log.Warn("Crafting refined.");
					Bot.SetGamePlaying(440);
					Recipes.CraftRefined(Bot);
					NonTradeRefined++;
					NonTradeReclaimed -= 3;
				}
				if (NonTradeReclaimed < 9 && HasNonTradeCounted)
				{
					Bot.Log.Warn("Smelting refined.");
					Bot.SetGamePlaying(440);
					Recipes.SmeltRefined(Bot);
					NonTradeRefined--;
					NonTradeReclaimed += 3;
				}
				if (NonTradeScrap > 15 && HasNonTradeCounted)
				{
					Bot.Log.Warn("Crafting reclaimed.");
					Bot.SetGamePlaying(440);
					Recipes.CraftReclaimed(Bot);
					NonTradeReclaimed++;
					NonTradeScrap -= 3;
				}
				if (NonTradeScrap < 9 && NonTradeReclaimed > 0 && HasNonTradeCounted)
				{
					Bot.Log.Warn("Smelting reclaimed.");
					Bot.SetGamePlaying(440);
					Recipes.SmeltReclaimed(Bot);
					NonTradeReclaimed--;
					NonTradeScrap += 3;
				}
				if (NonTradeScrap > 9 && NonTradeScrap < 15 && NonTradeReclaimed > 9 && NonTradeReclaimed < 15 && HasNonTradeCounted)
				{
					Complete = true;
				}
			}
			Bot.SetGamePlaying(0);
		}

		private void GiveChange()
		{
			if (!GaveChange && ValidateMetaltoKey > 0 && ExcessRefined > TF2Value.Zero)
			{
				SendTradeMessage("Change calculated. Here is " + ExcessRefined.ToRefString() + " back.");
				bool RefIsDone = false;
				bool RecIsDone = false;
				bool ScrapIsDone = false;
				bool DoneAddingMetal = false;
				int WhileLoop = 0;
				while (!DoneAddingMetal)
				{
					WhileLoop++;
					if (ExcessRefined >= TF2Value.Refined && !RefIsDone)
					{
						if (Refined > 0)
						{
							Trade.AddItemByDefindex(5002);
							Bot.Log.Warn("I added Refined Metal.");
							BotMetalAdded += TF2Value.Refined;
							ExcessRefined -= TF2Value.Refined;
							ChangeAdded += TF2Value.Refined;
							Refined--;
							RefinedAdded++;
						}
						else
						{
							RefIsDone = true;
						}
					}
					else if (ExcessRefined < TF2Value.Refined && !RefIsDone)
					{
						RefIsDone = true;
					}
					else if (ExcessRefined >= TF2Value.Reclaimed && RefIsDone && !RecIsDone)
					{
						if (Reclaimed > 0)
						{
							Trade.AddItemByDefindex(5001);
							Bot.Log.Warn("I added Reclaimed Metal.");
							BotMetalAdded += TF2Value.Reclaimed;
							ExcessRefined -= TF2Value.Reclaimed;
							ChangeAdded += TF2Value.Reclaimed;
							Reclaimed--;
							ReclaimedAdded++;
						}
						else
						{
							RecIsDone = true;
						}
					}
					else if (ExcessRefined < TF2Value.Reclaimed && !RecIsDone)
					{
						RecIsDone = true;
					}
					else if (ExcessRefined >= TF2Value.Scrap && RefIsDone && RecIsDone && !ScrapIsDone)
					{
						if (Scrap > 0)
						{
							Trade.AddItemByDefindex(5000);
							Bot.Log.Warn("I added Scrap Metal.");
							BotMetalAdded += TF2Value.Scrap;
							ExcessRefined -= TF2Value.Scrap;
							ChangeAdded += TF2Value.Scrap;
							Scrap--;
							ScrapAdded++;
						}
						else
						{
							ScrapIsDone = true;
						}
					}
					else if (ExcessRefined == TF2Value.Zero)
					{
						SendTradeMessage("Successfully gave change. Please click \"Ready to Trade\" again to complete.");
						Bot.Log.Success("Successfully gave change.");
						ScrapIsDone = true;
						DoneAddingMetal = true;
						GaveChange = true;
					}
					else if (RefIsDone && RecIsDone && ScrapIsDone && WhileLoop > 50)
					{
						SendTradeMessage("Error: Sorry, not enough change at the moment.");
						SendTradeMessage("I craft change every " + (CraftCheckTimer.Interval / 1000).ToString() + " seconds.");
						Bot.Log.Error("Couldn't add enough metal for the user while giving change!");
						ResetTrade(false);
						DoneAddingMetal = true;
					}
					else if (WhileLoop > 100)
					{
						SendTradeMessage("Error: I cannot add metal at the moment. Either Steam is broken or I am broken!");
						Bot.Log.Error("Error: Bot could not add metal to a trade.");
						ResetTrade(false);
						break;
					}
				}
			}
			else if (GaveChange)
			{
				SendTradeMessage("Error: I already gave you change and at this time I can't do so again.");
				SendTradeMessage("Doing so would cause the trade to fail.");
				ResetTrade(false);
				Bot.Log.Error("User tried to get change more than once.");
			}
			else
			{
				SendTradeMessage("Error: It looks like I shouldn't be trying to give change.");
				Bot.Log.Error("I received a change request, but conditions did not pass!");
				ResetTrade(false);
			}
		}

		public bool Validate()
		{
			TF2Value MetalCount = TF2Value.Zero;
			int KeyCount = 0;
			List<string> errors = new List<string>();
			foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
			{
				Inventory.Item item = Trade.OtherInventory.GetItem(asset.assetid);
				Schema.Item schemaItem = Trade.CurrentSchema.GetItem((int)item.Defindex);
				if (item.Defindex == 5000)
				{
					MetalCount += TF2Value.Scrap;
				}
				else if (item.Defindex == 5001)
				{
					MetalCount += TF2Value.Reclaimed;
				}
				else if (item.Defindex == 5002)
				{
					MetalCount += TF2Value.Refined;
				}
				else if (item.Defindex == 5021)
				{
					KeyCount++;
				}
				else
				{
					errors.Add("I can't accept " + schemaItem.ItemName + "!");
				}
			}
			ValidateMetaltoKey = MetalCount.GetPriceUsingItem(SellPricePerKey, out ExcessRefined);
			if (UserMetalAdded == TF2Value.Zero && UserKeysAdded == 0)
			{
				errors.Add("Error: You need to add either keys or metal to the trade before readying up.");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " readied without adding any items.");
			}
			if (!ChooseDonate && UserMetalAdded > TF2Value.Zero && UserKeysAdded > 0)
			{
				errors.Add("Error: You cannot add keys and metal to a trade at the same time.");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added both keys and metal to a trade.");
			}
			if (ChooseDonate)
			{
				foreach (TradeUserAssets asset2 in Trade.OtherOfferedItems)
				{
					Inventory.Item item2 = Trade.OtherInventory.GetItem(asset2.assetid);
					Schema.Item schemaItem2 = Trade.CurrentSchema.GetItem((int)item2.Defindex);
					if (schemaItem2.ItemName != "Mann Co. Supply Crate Key" && schemaItem2.ItemName != "#TF_Tool_DecoderRing" && item2.Defindex != 5000 && item2.Defindex != 5001 && item2.Defindex != 5002)
					{
						errors.Add("I'm sorry, but I cannot accept " + schemaItem2.ItemName + "!");
					}
				}
				if (BotMetalAdded > TF2Value.Zero || BotKeysAdded > 0)
				{
					errors.Add("Let me remove my items first. Then type \"donate\" again, or else you will get errors.");
				}
			}
			else if (UserKeysAdded > 0)
			{
				TF2Value ValidateRemainder;
				int KeysBought = BotMetalAdded.GetPriceUsingItem(BuyPricePerKey, out ValidateRemainder);
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " has " + KeyCount + " key(s) put up. Verifying if " + KeysBought + " key(s) bought == " + KeyCount + " key(s) user added.");
				if (KeysBought == KeyCount && ValidateRemainder == TF2Value.Zero)
				{
					Bot.Log.Success("Amount of metal added equalled price per key.");
				}
				else if (KeysBought > KeyCount)
				{
					errors.Add("Error: I have somehow added too much metal.");
					Bot.Log.Error("I somehow added too much metal.");
				}
				else if (KeysBought < KeyCount)
				{
					errors.Add("Error: I did not add enough metal. I may have run out of scrap.");
					errors.Add("Please close this trade and start again so I can smelt more scrap.");
					Bot.Log.Error("I did not add enough metal.");
				}
				else
				{
					errors.Add("Error: I failed to match key(s) bought. Either Steam is broken, or I am broken. Sorry!");
					Bot.Log.Error("I failed to validate key(s) bought.");
				}
			}
			else if (ValidateMetaltoKey == 0)
			{
				AdditionalRefined = SellPricePerKey - ExcessRefined;
				errors.Add("Error: Price is " + SellPricePerKey.ToRefString() + " per key. You need to add " + AdditionalRefined.ToRefString() + ".");
				Bot.Log.Warn(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " doesn't have enough metal added, and needs to add " + AdditionalRefined.ToRefString() + " for a key.");
			}
			else if (!GaveChange && ExcessRefined > TF2Value.Zero)
			{
				SendTradeMessage("You put up enough metal for " + ValidateMetaltoKey + " key(s), with " + ExcessRefined.ToRefString() + " extra.");
				Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " put up enough metal for " + ValidateMetaltoKey + " key(s), with " + ExcessRefined.ToRefString() + " extra.");
				ChangeValidate = true;
				return false;
			}
			else if (UserMetalAdded > TF2Value.Zero)
			{
				TF2Value ValidateKeys = BotKeysAdded * SellPricePerKey;
				TF2Value ValidateMetal = MetalCount - ChangeAdded;
				if (ValidateMetal != ValidateKeys)
				{
					errors.Add("Error: I failed to match your metal to my key(s). Either Steam is broken, or I am broken. Sorry!");
					Bot.Log.Error("I failed to validate metal to key(s).");
					ScamAttempt++;
				}
				else
				{
					Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " added exact amount for key(s).");
				}
			}
			if (errors.Count != 0)
			{
				SendTradeMessage("There were errors in your trade: ");
			}
			foreach (string error in errors)
			{
				SendTradeMessage(error);
			}
			return errors.Count == 0;
		}

//		public override void OnNewTradeOffer(TradeOffer offer)
//		{
//			if (Bot.Admins.Contains(offer.PartnerSteamId))
//			{
//				int WhileLoop = 0;
//				bool success = false;
//				while (!success)
//				{
//					WhileLoop++;
//                    TradeOfferAcceptResponse acceptResp = offer.Accept();
//                    if (acceptResp.Accepted)
//					{
//						Log.Success("Accepted Admin trade offer successfully.");
//                        //Log.Success("Accepted trade offer successfully : Trade ID: " + acceptResp.TradeId);
//						success = true;
//					}
//					else if (WhileLoop > 100)
//					{
//						Bot.Log.Error("Unable to accept Admin trade offer.");
//						break;
//					}
//				}
//			}
//			else
//			{
//                //if (offer = offer.)
//                //{
//                    //Bot.SteamFriends.SendChatMessage(offer.PartnerSteamId, EChatEntryType.ChatMsg, "I'm sorry, at this time I do not accept trade offers. Please check back some time, in the future I will have the ability to send and receive trade offers.");
//                //}
//			}
//		}

		public void CountInventory(bool message)
		{
			bool Done = false;
			InventoryFailed = false;
			int retries = 0;
			int WhileLoop = 0;
			while (!Done)
			{
				WhileLoop++;
				if (WhileLoop >= 100)
				{
					Console.WriteLine("CountInventory was stuck in a loop and forceably exited.");
					Bot.Log.Error("CountInventory was stuck in a loop and forceably exited.");
					InventoryFailed = true;
					break;
				}
				try
				{
					Bot.GetInventory();
					Inventory.Item[] inventory = Bot.MyInventory.Items;
                    Slots = Bot.MyInventory.NumSlots;
					NonTradeScrap = 0;
					NonTradeReclaimed = 0;
					NonTradeRefined = 0;
					NonTradeInventoryMetal = TF2Value.Zero;
					NonTradeKeysCanBuy = 0;
					NonTradeInventoryKeys = 0;
                    Item = 0;
					Inventory.Item[] array = inventory;
					for (int i = 0; i < array.Length; i++)
					{
						Inventory.Item item = array[i];
						if (item.Defindex == 5000)
						{
							NonTradeInventoryMetal += TF2Value.Scrap;
							NonTradeScrap++;
                            Item++;
						}
						else if (item.Defindex == 5001)
						{
							NonTradeInventoryMetal += TF2Value.Reclaimed;
							NonTradeReclaimed++;
                            Item++;
						}
						else if (item.Defindex == 5002)
						{
							NonTradeInventoryMetal += TF2Value.Refined;
							NonTradeRefined++;
                            Item++;
						}
						else if (item.Defindex == 5021)
						{
							NonTradeInventoryKeys++;
                            Item++;
						}
                        else
                        {
                            Item++;
                        }
					}
					NonTradeKeysCanBuy = NonTradeInventoryMetal.GetPriceUsingItem(BuyPricePerKey, out LeftoverMetal);
					if (message)
					{
						SendChatMessage("I have " + NonTradeInventoryMetal.ToRefString() + ". I have " + NonTradeScrap + " scrap and " + NonTradeReclaimed + " reclaimed.");
						Bot.Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " queried inventory.");
					}
					HasNonTradeCounted = true;
					Done = true;
				}
				catch (Exception e)
				{
					retries++;
					Thread.Sleep((retries * 50) * (retries / 2));
					if (retries >= 3)
					{
						Bot.Log.Error("Inventory request failed after " + retries.ToString() + " retries.");
						InventoryFailed = true;
						Done = true;
					}
				}
			}
		}

		public void TradeCountInventory(bool message)
		{
			bool Done = false;
			int retries = 0;
			int WhileLoop = 0;
			InventoryFailed = false;
			while (!Done)
			{
				WhileLoop++;
				if (WhileLoop >= 100)
				{
					Bot.Log.Error("TradeCountInventory was stuck in a loop and forceably exited.");
					InventoryFailed = true;
					break;
				}
				try
				{
					Inventory.Item[] inventory = Trade.MyInventory.Items;
					InventoryMetal = TF2Value.Zero;
					Scrap = 0;
					Reclaimed = 0;
					InventoryKeys = 0;
					KeysCanBuy = 0;
					Inventory.Item[] array = inventory;
					for (int i = 0; i < array.Length; i++)
					{
						Inventory.Item item = array[i];
						Trade.CurrentSchema.GetItem((int)item.Defindex);
						if (item.Defindex == 5000)
						{
							InventoryMetal += TF2Value.Scrap;
							Scrap++;
						}
						else if (item.Defindex == 5001)
						{
							InventoryMetal += TF2Value.Reclaimed;
							Reclaimed++;
						}
						else if (item.Defindex == 5002)
						{
							InventoryMetal += TF2Value.Refined;
							Refined++;
						}
						else if (item.Defindex == 5021)
						{
							InventoryKeys++;
						}
					}
					KeysCanBuy = InventoryMetal.GetPriceUsingItem(BuyPricePerKey, out LeftoverMetal);
					if (message)
					{
						SendTradeMessage("Current stock: I have " + InventoryMetal.ToRefString());
						SendTradeMessage("and " + InventoryKeys + " key(s) in my backpack.");
						SendTradeMessage("I have " + Scrap + " scrap and " + Reclaimed + " reclaimed.");
						Bot.Log.Success("Current stock: I have " + InventoryMetal.ToRefString() + " and " + InventoryKeys + " key(s) in my backpack.");
					}
					HasCounted = true;
					Done = true;
				}
				catch (Exception e)
				{
					retries++;
					Thread.Sleep((retries * 100) * (retries / 2));
					if (retries >= 3)
					{
						Bot.Log.Error("Inventory request failed after " + retries.ToString() + " retries.");
						InventoryFailed = true;
						Done = true;
					}
				}
			}
		}

		public void ReInit()
		{
			IgnoringBot = 0;
			NumKeys = 0;
			AdditionalRefined = TF2Value.Zero;
			UserMetalAdded = TF2Value.Zero;
			UserKeysAdded = 0;
			BotKeysAdded = 0;
			BotMetalAdded = TF2Value.Zero;
			RefinedAdded = 0;
			ReclaimedAdded = 0;
			ScrapAdded = 0;
			KeysToScrap = TF2Value.Zero;
			ValidateMetaltoKey = 0;
			PreviousKeys = 0;
			ExcessRefined = TF2Value.Zero;
			ChangeAdded = TF2Value.Zero;
			InvalidItem = 0;
			HasErrorRun = false;
			ChooseDonate = false;
			GaveChange = false;
			ChangeValidate = false;
			HasCounted = false;
		}
	}
}