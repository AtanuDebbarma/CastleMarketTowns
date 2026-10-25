using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace CastleMarketTowns
{
    // CompanionsCraft is optional and not referenced: its method is looked up by name, and nothing is patched without it.
    // When the party leaves a settlement, its companions buy smithing materials there, priced as in a town or else as in a
    // village. A castle is neither, so once its market stocks those materials the village price throws and the game crashes.
    internal static class CastleCompanionsCraft
    {
        public static void TryPatch(Harmony harmony)
        {
            Type behavior = AccessTools.TypeByName("CompanionsCraft.CompanionsCraftingBehavior");
            if (behavior == null)
            {
                return;
            }

            MethodInfo buy = AccessTools.Method(behavior, "BuyMaterialsUpToLimit");
            if (buy == null)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not find CompanionsCraft's material buying; that support is inactive, likely because a CompanionsCraft update changed it.");
                return;
            }

            if (!ReadsIsTown(buy))
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns found nothing to change in CompanionsCraft's material buying; that support is inactive, likely because a CompanionsCraft update changed it.");
                return;
            }

            harmony.Patch(buy, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleCompanionsCraft), "BuyTranspiler")));
        }

        private static bool ReadsIsTown(MethodInfo target)
        {
            foreach (CodeInstruction instruction in PatchProcessor.GetOriginalInstructions(target))
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == "get_IsTown" && called.DeclaringType == typeof(Settlement))
                {
                    return true;
                }
            }

            return false;
        }

        // Castles have a town component with its own market prices and gold, so they buy and pay as towns do.
        private static IEnumerable<CodeInstruction> BuyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "TradesAsTown");
        }
    }
}
