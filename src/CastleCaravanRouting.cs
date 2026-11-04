using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CastleMarketTowns
{
    internal static class CastleCaravanRouting
    {
        private const float RecentStopDays = 5f;
        private const float RecentStopPenalty = 0.35f;
        // Unbounded soft decay: each extra visitor keeps reducing the score (no 0.30 floor).
        private const float CongestionWeight = 0.4f;
        private const int HardCrowdReject = 12;
        private const float ProfitablePriceFactor = 1.05f;

        // Adjusts GetTradeScoreForTown for covered castles only.
        internal static void AdjustScore(MobileParty caravan, Town town, ref float score)
        {
            if (score <= 0f || caravan == null || town == null || town.Settlement == null || !town.Settlement.IsCastle)
            {
                return;
            }

            Settlement castle = town.Settlement;
            if (!CastleMarketTownsBehavior.AppliesTo(castle) || !CastleCaravanStops.Enabled())
            {
                return;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null)
            {
                return;
            }

            if (CastleCaravanOrigins.IsRecentPreviousStop(caravan, castle, RecentStopDays))
            {
                score *= RecentStopPenalty;
            }

            int crowding = CastleCaravanOrigins.CountTargetingOrPresent(castle, caravan);
            if (crowding >= HardCrowdReject)
            {
                score = 0f;
                return;
            }

            if (crowding > 0)
            {
                score *= 1f / (1f + crowding * CongestionWeight);
            }

            // Only dampen when the caravan actually has goods that look profitable to sell here.
            long sellCargoValue = ApproximateProfitableSellValue(caravan, town);
            if (sellCargoValue > 0)
            {
                if (town.Gold < sellCargoValue / 4)
                {
                    score *= 0.55f;
                }
                else if (town.Gold < sellCargoValue / 2)
                {
                    score *= 0.8f;
                }
            }

            if (!settings.CaravansFavourLowStock)
            {
                return;
            }

            float shortage = CategoryShortage(castle, caravan, settings);
            if (shortage <= 0f)
            {
                return;
            }

            float boost = settings.CaravanBonusPercent / 100f * shortage;
            if (CaravanCarriesFood(caravan) && CastleHasFoodDeficit(castle))
            {
                boost += 0.15f * shortage;
            }

            score *= 1f + boost;
        }

        private static float CategoryShortage(Settlement castle, MobileParty caravan, CastleMarketTownsSettings settings)
        {
            if (castle == null || castle.ItemRoster == null || caravan == null || caravan.ItemRoster == null)
            {
                return 0f;
            }

            float targetScale = Math.Max(0.1f, settings.MarketPercent / 100f);
            Settlement marketTown = NearestTownFor(castle);
            float worst = 0f;
            for (int i = 0; i < caravan.ItemRoster.Count; i++)
            {
                ItemRosterElement element = caravan.ItemRoster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || !item.IsTradeGood || element.Amount <= 0)
                {
                    continue;
                }

                int castleCount = castle.ItemRoster.GetItemNumber(item);
                int townCount = marketTown == null || marketTown.ItemRoster == null ? 0 : marketTown.ItemRoster.GetItemNumber(item);
                float intended = Math.Max(1f, townCount * targetScale);
                float ratio = castleCount / intended;
                if (ratio < 0.35f)
                {
                    worst = Math.Max(worst, 1f - ratio / 0.35f);
                }
            }

            return worst;
        }

        private static bool CaravanCarriesFood(MobileParty caravan)
        {
            if (caravan == null || caravan.ItemRoster == null)
            {
                return false;
            }

            for (int i = 0; i < caravan.ItemRoster.Count; i++)
            {
                ItemRosterElement element = caravan.ItemRoster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item != null && element.Amount > 0 && item.ItemCategory != null && item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CastleHasFoodDeficit(Settlement castle)
        {
            if (castle == null || castle.Town == null)
            {
                return false;
            }

            return castle.Town.FoodStocks < 1f
                || castle.Town.FoodChangeWithoutMarketStocks < -0.01f;
        }

        private static long ApproximateProfitableSellValue(MobileParty caravan, Town town)
        {
            if (caravan == null || caravan.ItemRoster == null || town == null || town.MarketData == null)
            {
                return 0L;
            }

            long value = 0L;
            for (int i = 0; i < caravan.ItemRoster.Count; i++)
            {
                ItemRosterElement element = caravan.ItemRoster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || !item.IsTradeGood || element.Amount <= 0 || item.ItemCategory == null)
                {
                    continue;
                }

                float factor = town.MarketData.GetPriceFactor(item.ItemCategory);
                if (factor < ProfitablePriceFactor)
                {
                    continue;
                }

                long line = (long)item.Value * element.Amount;
                if (line <= 0L)
                {
                    continue;
                }

                if (value > long.MaxValue - line)
                {
                    return long.MaxValue;
                }

                value += line;
            }

            return value;
        }

        private static Settlement NearestTownFor(Settlement castle)
        {
            Settlement best = null;
            float bestDistance = float.MaxValue;
            foreach (Town town in Town.AllTowns)
            {
                if (town == null || town.Settlement == null)
                {
                    continue;
                }

                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(castle, town.Settlement, false, false, MobileParty.NavigationType.Default);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = town.Settlement;
                }
            }

            return best;
        }
    }
}
