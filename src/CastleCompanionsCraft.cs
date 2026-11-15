using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    // CompanionsCraft is optional and not referenced: its method is looked up by name, and nothing is patched without it.
    // When the party leaves a settlement, its companions buy smithing materials there, priced as in a town or else as in a
    // village. A castle is neither, so once its market stocks those materials the village price throws and the game crashes.
    // Exactly two Settlement.IsTown checks in BuyMaterialsUpToLimit are redirected (price + payment). Refining stays town-only
    // in CompanionsCraft's own outer logic, which this adapter does not touch.
    internal static class CastleCompanionsCraft
    {
        private const int ExpectedIsTownCalls = 2;

        private static bool _transpilerOk;
        private static int _isTownMatches;

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
                Reject("CompanionsCraft's material buying was not found.");
                return;
            }

            if (CountIsTownCalls(buy) != ExpectedIsTownCalls)
            {
                Reject("CompanionsCraft BuyMaterialsUpToLimit did not contain exactly two Settlement.IsTown checks.");
                return;
            }

            _transpilerOk = false;
            _isTownMatches = 0;
            harmony.Patch(buy, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleCompanionsCraft), "BuyTranspiler")));

            if (!_transpilerOk || _isTownMatches != ExpectedIsTownCalls || !HasOwnTranspiler(harmony, buy) || !RedirectedExactly(buy, ExpectedIsTownCalls))
            {
                try
                {
                    harmony.Unpatch(buy, HarmonyPatchType.Transpiler, harmony.Id);
                }
                catch
                {
                }

                Reject("CompanionsCraft BuyMaterialsUpToLimit could not be redirected with exactly two IsTown replacements.");
            }
        }

        private static void Reject(string reason)
        {
            Debug.Print("Castle Market Towns could not enable CompanionsCraft support: " + reason
                + " That support is inactive, likely because a CompanionsCraft update changed it.");
        }

        private static int CountIsTownCalls(MethodInfo target)
        {
            int count = 0;
            foreach (CodeInstruction instruction in PatchProcessor.GetOriginalInstructions(target))
            {
                if (IsSettlementIsTown(instruction))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsSettlementIsTown(CodeInstruction instruction)
        {
            MethodInfo called = instruction.operand as MethodInfo;
            return called != null && called.Name == "get_IsTown" && called.DeclaringType == typeof(Settlement);
        }

        private static IEnumerable<CodeInstruction> BuyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            int matches = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (IsSettlementIsTown(list[i]))
                {
                    matches++;
                }
            }

            _isTownMatches = matches;
            if (matches != ExpectedIsTownCalls)
            {
                _transpilerOk = false;
                return list;
            }

            MethodInfo helper = AccessTools.Method(typeof(CastleTownRules), "CompanionsCraftTradesAsTown");
            if (helper == null)
            {
                _transpilerOk = false;
                return list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (IsSettlementIsTown(list[i]))
                {
                    list[i].opcode = OpCodes.Call;
                    list[i].operand = helper;
                }
            }

            _transpilerOk = true;
            return list;
        }

        private static bool HasOwnTranspiler(Harmony harmony, MethodBase target)
        {
            Patches info = Harmony.GetPatchInfo(target);
            if (info == null)
            {
                return false;
            }

            foreach (Patch patch in info.Transpilers)
            {
                if (patch.owner == harmony.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RedirectedExactly(MethodBase target, int expected)
        {
            MethodInfo helper = AccessTools.Method(typeof(CastleTownRules), "CompanionsCraftTradesAsTown");
            if (helper == null)
            {
                return false;
            }

            int count = 0;
            foreach (CodeInstruction instruction in PatchProcessor.GetCurrentInstructions(target))
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called == helper)
                {
                    count++;
                }
            }

            return count == expected;
        }
    }
}
