using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace CastleMarketTowns
{
    internal static class CastleTownRules
    {
        public static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, Type declaring, string getter, string helper)
        {
            MethodInfo target = AccessTools.Method(typeof(CastleTownRules), helper);
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == getter && called.DeclaringType == declaring)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = target;
                }

                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> ScaleConstant(IEnumerable<CodeInstruction> instructions, float constant, string helper, int maxCount)
        {
            MethodInfo target = AccessTools.Method(typeof(CastleTownRules), helper);
            int done = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (done < maxCount && instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float && Math.Abs((float)instruction.operand - constant) < 0.0001f)
                {
                    done++;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, target);
                }
            }
        }

        private static CastleMarketTownsSettings Settings
        {
            get { return CastleMarketTownsSettings.Current; }
        }

        public static bool Covered(Settlement settlement)
        {
            return settlement != null && settlement.IsCastle && settlement.Town != null && CastleMarketTownsBehavior.AppliesTo(settlement);
        }

        private static bool CoveredTown(Town town)
        {
            return town != null && Covered(town.Settlement);
        }

        public static bool TradesAsTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (settlement.IsCastle && settlement.Town != null));
        }

        public static bool TaxedAsTown(SettlementComponent component)
        {
            return component != null && (component.IsTown || Covered(component.Settlement));
        }

        public static bool MarketRulesAsTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || Covered(settlement));
        }

        public static bool RulingIncomeAsTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (Covered(settlement) && settlement.Town.Workshops != null));
        }

        public static bool HealsAsTown(Settlement settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            return settlement.IsTown || ((Settings == null || Settings.CastleHealing) && CastleMarketTownsBehavior.TavernAvailable(settlement));
        }

        public static bool RepairsAsTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || ((Settings == null || Settings.CastleRepair) && Covered(settlement)));
        }

        public static bool WorkshopsAsTown(Settlement settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            if (settlement.IsTown)
            {
                return true;
            }

            if (!settlement.IsCastle || settlement.Town == null || settlement.Town.Workshops == null || settlement.Town.Workshops.Length == 0)
            {
                return false;
            }

            foreach (Workshop workshop in settlement.Town.Workshops)
            {
                if (workshop == null || workshop.Owner == null)
                {
                    return false;
                }
            }

            foreach (Hero notable in settlement.Notables)
            {
                if (notable != null && notable.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool SiegeFoodAsTown(SettlementComponent component)
        {
            return component != null && (component.IsTown || ((Settings == null || Settings.SiegeFoodFromMarket) && Covered(component.Settlement)));
        }

        public static bool SellsFoodAsTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || ((Settings == null || Settings.LordsBuyCastleFood) && Covered(settlement)));
        }

        public static bool MarketProsperityAsTown(SettlementComponent component)
        {
            return component != null && (component.IsTown || (Settings != null && Settings.TradeProsperityPercent > 0 && Covered(component.Settlement)));
        }

        public static float ScaleMarketProsperity(float rate, Town town)
        {
            return CoveredTown(town) && Settings != null ? rate * Settings.TradeProsperityPercent / 100f : rate;
        }

        public static float ScaleTax(float rate, Town town)
        {
            return CoveredTown(town) && Settings != null ? rate * Settings.CastleTaxPercent / 100f : rate;
        }
    }

    [HarmonyPatch]
    internal static class CastleTradeTaxPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(SellItemsAction), "ApplyInternal"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "TradesAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleTaxRatePatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(DefaultSettlementTaxModel), "CalculateDailyTax"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.ScaleConstant(instructions, 0.35f, "ScaleTax", 1);
        }
    }

    [HarmonyPatch]
    internal static class CastleTaxPolicyPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(DefaultSettlementTaxModel), "CalculateDailyTaxInternal"),
                AccessTools.Method(typeof(DefaultSettlementTaxModel), "CalculatePolicyGoldCut"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(SettlementComponent), "get_IsTown", "TaxedAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleRulingIncomePatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(DefaultClanFinanceModel), "AddRulingClanIncome"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "RulingIncomeAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleMarketPerksPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(CaravansCampaignBehavior), "OnSettlementEntered"),
                AccessTools.Method(typeof(DefaultTradeItemPriceFactorModel), "GetTradePenalty"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "MarketRulesAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleTradeProsperityPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(DefaultSettlementProsperityModel), "CalculateProsperityChangeInternal"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int sold = -1;
            for (int i = 0; i < codes.Count && sold < 0; i++)
            {
                if (!Calls(codes[i], typeof(SettlementComponent), "get_IsTown"))
                {
                    continue;
                }

                for (int j = i + 1; j < codes.Count && j <= i + 4; j++)
                {
                    if (Calls(codes[j], typeof(Town), "get_SoldItems"))
                    {
                        codes[i].opcode = OpCodes.Call;
                        codes[i].operand = AccessTools.Method(typeof(CastleTownRules), "MarketProsperityAsTown");
                        sold = j;
                        break;
                    }
                }
            }

            for (int i = sold + 1; sold >= 0 && i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float && Math.Abs((float)codes[i].operand - 0.1f) < 0.0001f)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_1));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CastleTownRules), "ScaleMarketProsperity")));
                    break;
                }
            }

            return codes;
        }

        private static bool Calls(CodeInstruction instruction, Type declaring, string name)
        {
            MethodInfo called = instruction.operand as MethodInfo;
            return called != null && called.Name == name && called.DeclaringType == declaring;
        }
    }

    [HarmonyPatch]
    internal static class CastleHealingPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(DefaultPartyHealingModel), "GetDailyHealingForRegulars"),
                AccessTools.Method(typeof(DefaultPartyHealingModel), "GetDailyHealingHpForHeroes"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "HealsAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleRepairPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(VillageHealCampaignBehavior), "DailyTickSettlement"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "RepairsAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleWorkshopOwnerPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(WorkshopsCampaignBehavior), "RemoveDeadOwnersFromWorkshops"),
                AccessTools.Method(typeof(WorkshopsCampaignBehavior), "OnSettlementOwnerChanged"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "WorkshopsAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleSiegeFoodPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(ItemConsumptionBehavior), "GetFoodFromMarket"),
                AccessTools.Method(typeof(TownHelpers), "GetTownFoodAndMarketStocks"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(SettlementComponent), "get_IsTown", "SiegeFoodAsTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleLordFoodPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(PartiesBuyFoodCampaignBehavior), "TryBuyingFood"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "SellsFoodAsTown");
        }

        static void Prefix(MobileParty mobileParty, Settlement settlement, out List<KeyValuePair<EquipmentElement, int>> __state)
        {
            __state = null;
            if (mobileParty == null || mobileParty.LeaderHero == null || settlement == null || settlement.IsTown || !CastleTownRules.SellsFoodAsTown(settlement) || settlement.ItemRoster == null)
            {
                return;
            }

            ItemRoster roster = settlement.ItemRoster;
            List<KeyValuePair<EquipmentElement, int>> held = new List<KeyValuePair<EquipmentElement, int>>();
            Dictionary<ItemObject, int> left = new Dictionary<ItemObject, int>();
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement element = roster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || !item.IsFood || element.Amount <= 0)
                {
                    continue;
                }

                int reserve;
                if (!left.TryGetValue(item, out reserve))
                {
                    reserve = CastleMarketTownsBehavior.Reserve(settlement, item);
                }

                int keep = Math.Min(element.Amount, reserve);
                left[item] = reserve - keep;
                if (keep > 0)
                {
                    held.Add(new KeyValuePair<EquipmentElement, int>(element.EquipmentElement, keep));
                }
            }

            foreach (KeyValuePair<EquipmentElement, int> pair in held)
            {
                roster.AddToCounts(pair.Key, -pair.Value);
            }

            __state = held;
        }

        static Exception Finalizer(Exception __exception, Settlement settlement, List<KeyValuePair<EquipmentElement, int>> __state)
        {
            if (__state != null && settlement != null && settlement.ItemRoster != null)
            {
                foreach (KeyValuePair<EquipmentElement, int> pair in __state)
                {
                    settlement.ItemRoster.AddToCounts(pair.Key, pair.Value);
                }
            }

            return __exception;
        }
    }
}
