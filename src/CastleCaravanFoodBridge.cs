using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    // After ordinary ItemConsumptionBehavior, convert remaining market food into Food Stocks
    // via SoldItems only. Town.DailyTick applies FoodChange from SoldItems — do not also
    // add FoodStocks directly or the same units are counted twice.
    internal static class CastleCaravanFoodBridge
    {
        private static readonly FieldInfo SoldItemsField = AccessTools.Field(typeof(Town), "_soldItems");

        internal static void ApplyRemainingFoodDeficit(Town town)
        {
            if (town == null || town.Settlement == null || !town.Settlement.IsCastle)
            {
                return;
            }

            if (!CastleMarketTownsBehavior.AppliesTo(town.Settlement) || town.Settlement.ItemRoster == null)
            {
                return;
            }

            if (town.IsUnderSiege)
            {
                return;
            }

            float withoutMarket = town.FoodChangeWithoutMarketStocks;
            float ordinaryFood = OrdinaryFoodAlreadyCredited(town);
            float remaining = Math.Max(0f, -(withoutMarket + ordinaryFood));
            if (remaining < 0.01f)
            {
                return;
            }

            Dictionary<ItemCategory, int> takenByCategory = ConsumeMarketFood(town.Settlement, (int)Math.Ceiling(remaining));
            if (takenByCategory == null || takenByCategory.Count == 0)
            {
                return;
            }

            MergeFoodIntoSoldItems(town, takenByCategory);
        }

        private static float OrdinaryFoodAlreadyCredited(Town town)
        {
            float food = 0f;
            try
            {
                foreach (Town.SellLog log in town.SoldItems)
                {
                    if (log.Category != null && log.Category.Properties == ItemCategory.Property.BonusToFoodStores)
                    {
                        food += log.Number;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not read SoldItems for food bridge: " + exception.Message);
            }

            return food;
        }

        private static Dictionary<ItemCategory, int> ConsumeMarketFood(Settlement settlement, int needed)
        {
            Dictionary<ItemCategory, int> takenByCategory = new Dictionary<ItemCategory, int>();
            if (needed <= 0 || settlement.ItemRoster == null)
            {
                return takenByCategory;
            }

            int remaining = needed;
            List<ItemObject> foods = new List<ItemObject>();
            for (int i = 0; i < settlement.ItemRoster.Count; i++)
            {
                ItemRosterElement element = settlement.ItemRoster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item != null && element.Amount > 0 && item.ItemCategory != null
                    && item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores
                    && !foods.Contains(item))
                {
                    foods.Add(item);
                }
            }

            for (int i = 0; i < foods.Count && remaining > 0; i++)
            {
                ItemObject item = foods[i];
                int have = settlement.ItemRoster.GetItemNumber(item);
                int use = Math.Min(have, remaining);
                if (use <= 0)
                {
                    continue;
                }

                settlement.ItemRoster.AddToCounts(item, -use);
                remaining -= use;

                ItemCategory category = item.ItemCategory ?? DefaultItemCategories.Grain;
                int prior;
                takenByCategory.TryGetValue(category, out prior);
                takenByCategory[category] = prior + use;
            }

            return takenByCategory;
        }

        private static void MergeFoodIntoSoldItems(Town town, Dictionary<ItemCategory, int> takenByCategory)
        {
            if (takenByCategory == null || takenByCategory.Count == 0 || SoldItemsField == null)
            {
                return;
            }

            try
            {
                Town.SellLog[] existing = SoldItemsField.GetValue(town) as Town.SellLog[];
                if (existing == null)
                {
                    existing = new Town.SellLog[0];
                }

                foreach (KeyValuePair<ItemCategory, int> pair in takenByCategory)
                {
                    ItemCategory foodCategory = pair.Key ?? DefaultItemCategories.Grain;
                    int units = pair.Value;
                    if (units <= 0)
                    {
                        continue;
                    }

                    bool merged = false;
                    for (int i = 0; i < existing.Length; i++)
                    {
                        if (existing[i].Category == foodCategory)
                        {
                            existing[i] = new Town.SellLog(foodCategory, existing[i].Number + units);
                            merged = true;
                            break;
                        }
                    }

                    if (!merged)
                    {
                        Town.SellLog[] expanded = new Town.SellLog[existing.Length + 1];
                        Array.Copy(existing, expanded, existing.Length);
                        expanded[existing.Length] = new Town.SellLog(foodCategory, units);
                        existing = expanded;
                    }
                }

                SoldItemsField.SetValue(town, existing);
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not merge food into SoldItems: " + exception.Message);
            }
        }
    }
}
