using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace CastleMarketTowns
{
    // Improved Garrisons is optional and not referenced: its methods are looked up by name, and nothing is patched without it.
    internal static class CastleImprovedGarrisons
    {
        internal const string MenuOptionId = "town_garrison_options";

        internal static bool Active
        {
            get
            {
                CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
                return settings == null || settings.ImprovedGarrisonsSupport;
            }
        }

        public static void TryPatch(Harmony harmony)
        {
            if (AccessTools.TypeByName("ImprovedGarrisons.Main") == null)
            {
                return;
            }

            Type buildingVM = AccessTools.TypeByName("ImprovedGarrisons.ImprovedGarrisonsUI.SubMenus.ManagementUtils.BuildingVM");
            ConstructorInfo tooltip = buildingVM == null ? null : AccessTools.Constructor(buildingVM, new Type[] { typeof(Building) });
            if (Report(tooltip, "project tooltips"))
            {
                harmony.Patch(tooltip, prefix: new HarmonyMethod(AccessTools.Method(typeof(CastleImprovedGarrisons), "TooltipPrefix")), finalizer: new HarmonyMethod(AccessTools.Method(typeof(CastleImprovedGarrisons), "TooltipFinalizer")));
            }

            Type ai = AccessTools.TypeByName("ImprovedGarrisons.AI.AITypes.ImprovedAi");
            RedirectIsTown(harmony, ai == null ? null : AccessTools.Method(ai, "SettlementIsAValidTradePartner"), "trade and recruit stops");
            Type partyAi = AccessTools.TypeByName("ImprovedGarrisons.AI.AITypes.ImprovedPartyAi");
            RedirectIsTown(harmony, partyAi == null ? null : AccessTools.Method(partyAi, "SellItems"), "loot selling");
        }

        private static void RedirectIsTown(Harmony harmony, MethodInfo target, string feature)
        {
            if (!Report(target, feature))
            {
                return;
            }

            bool readsIsTown = false;
            foreach (CodeInstruction instruction in PatchProcessor.GetOriginalInstructions(target))
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == "get_IsTown" && called.DeclaringType == typeof(Settlement))
                {
                    readsIsTown = true;
                    break;
                }
            }

            if (!readsIsTown)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns found nothing to change in Improved Garrisons' " + feature + "; that support is inactive, likely because an Improved Garrisons update changed it.");
                return;
            }

            harmony.Patch(target, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleImprovedGarrisons), "TradeTranspiler")));
        }

        private static bool Report(MethodBase target, string feature)
        {
            if (target != null)
            {
                return true;
            }

            TaleWorlds.Library.Debug.Print("Castle Market Towns could not find Improved Garrisons' " + feature + "; that support is inactive, likely because an Improved Garrisons update changed it.");
            return false;
        }

        private static void TooltipPrefix(object[] __args, out object __state)
        {
            Building building = __args != null && __args.Length > 0 ? __args[0] as Building : null;
            __state = Active ? CastleBuildingEffects.Swap(building) : null;
        }

        private static void TooltipFinalizer(object __state)
        {
            CastleBuildingEffects.Restore(__state);
        }

        private static IEnumerable<CodeInstruction> TradeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "ImprovedGarrisonsTradesAsTown");
        }
    }
}
