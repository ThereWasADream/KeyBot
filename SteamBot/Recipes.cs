using SteamBot.TF2GC;
using SteamTrade;
using System;
using System.Collections.Generic;

namespace SteamBot
{
	public static class Recipes
	{
		public static void SmeltReclaimed(Bot bot)
		{
			List<ulong> recs = new List<ulong>();
			bot.GetInventory();
			Inventory.Item[] items = bot.MyInventory.Items;
			for (int i = 0; i < items.Length; i++)
			{
				Inventory.Item item = items[i];
				if (recs.Count == 1)
				{
					break;
				}
				if (item.Defindex == 5001 && !recs.Contains(item.Id))
				{
					recs.Add(item.Id);
				}
			}
			if (recs.Count == 1)
			{
				Crafting.CraftItems(bot, ECraftingRecipe.SmeltReclaimed, recs.ToArray());
			}
		}

		public static void SmeltRefined(Bot bot)
		{
			List<ulong> refs = new List<ulong>();
			bot.GetInventory();
			Inventory.Item[] items = bot.MyInventory.Items;
			for (int i = 0; i < items.Length; i++)
			{
				Inventory.Item item = items[i];
				if (refs.Count == 1)
				{
					break;
				}
				if (item.Defindex == 5002 && !refs.Contains(item.Id))
				{
					refs.Add(item.Id);
				}
			}
			if (refs.Count == 1)
			{
				Crafting.CraftItems(bot, ECraftingRecipe.SmeltRefined, refs.ToArray());
			}
		}

		public static void CraftReclaimed(Bot bot)
		{
			List<ulong> scraps = new List<ulong>();
			bot.GetInventory();
			Inventory.Item[] items = bot.MyInventory.Items;
			for (int i = 0; i < items.Length; i++)
			{
				Inventory.Item item = items[i];
				if (scraps.Count == 3)
				{
					break;
				}
				if (item.Defindex == 5000 && !scraps.Contains(item.Id))
				{
					scraps.Add(item.Id);
				}
			}
			if (scraps.Count == 3)
			{
				Crafting.CraftItems(bot, ECraftingRecipe.CombineScrap, scraps.ToArray());
			}
		}

		public static void CraftRefined(Bot bot)
		{
			List<ulong> recs = new List<ulong>();
			bot.GetInventory();
			Inventory.Item[] items = bot.MyInventory.Items;
			for (int i = 0; i < items.Length; i++)
			{
				Inventory.Item item = items[i];
				if (recs.Count == 3)
				{
					break;
				}
				if (item.Defindex == 5001 && !recs.Contains(item.Id))
				{
					recs.Add(item.Id);
				}
			}
			if (recs.Count == 3)
			{
				Crafting.CraftItems(bot, ECraftingRecipe.CombineReclaimed, recs.ToArray());
			}
		}
	}
}
