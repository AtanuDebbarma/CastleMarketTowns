using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    // They Married is optional and not referenced: its method is looked up by name, and nothing is patched without it.
    // A companion sent to another clan gets a lord party whose first troops come from the notables of its home settlement
    // when that is a town or village. A castle home is neither, so its merchants and artisans were skipped.
    internal static class CastleTheyMarried
    {
        private const int ExpectedIsTownCalls = 1;

        private static bool _transpilerOk;
        private static int _isTownMatches;

        public static void TryPatch(Harmony harmony)
        {
            Type dialogue = AccessTools.TypeByName("TheyMarried.CompanionClanTransferDialogue");
            if (dialogue == null)
            {
                return;
            }

            MethodInfo troops = AccessTools.Method(dialogue, "AddInitialTroopsToParty");
            if (troops == null)
            {
                Reject("They Married's starting troops for new lords were not found.");
                return;
            }

            if (CountIsTownCalls(troops) != ExpectedIsTownCalls)
            {
                Reject("They Married AddInitialTroopsToParty did not contain exactly one Settlement.IsTown check.");
                return;
            }

            _transpilerOk = false;
            _isTownMatches = 0;
            harmony.Patch(troops, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleTheyMarried), "TroopsTranspiler")));

            if (!_transpilerOk || _isTownMatches != ExpectedIsTownCalls || !HasOwnTranspiler(harmony, troops) || !RedirectedExactly(troops, ExpectedIsTownCalls))
            {
                try
                {
                    harmony.Unpatch(troops, HarmonyPatchType.Transpiler, harmony.Id);
                }
                catch
                {
                }

                Reject("They Married AddInitialTroopsToParty could not be redirected with exactly one IsTown replacement.");
            }
        }

        private static void Reject(string reason)
        {
            Debug.Print("Castle Market Towns could not enable They Married support: " + reason
                + " That support is inactive, likely because a They Married update changed it.");
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

        // Same rule as castle recruiting by you and by lords: a castle the mod covers, with Recruit troops in castles on.
        private static IEnumerable<CodeInstruction> TroopsTranspiler(IEnumerable<CodeInstruction> instructions)
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

            MethodInfo helper = AccessTools.Method(typeof(CastleVolunteerPatch), "RecruitTown");
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
            MethodInfo helper = AccessTools.Method(typeof(CastleVolunteerPatch), "RecruitTown");
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
