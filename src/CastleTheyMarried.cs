using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace CastleMarketTowns
{
    // They Married is optional and not referenced: its method is looked up by name, and nothing is patched without it.
    // A companion sent to another clan gets a lord party whose first troops come from the notables of its home settlement
    // when that is a town or village. A castle home is neither, so its merchants and artisans were skipped.
    internal static class CastleTheyMarried
    {
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
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not find They Married's starting troops for new lords; that support is inactive, likely because a They Married update changed it.");
                return;
            }

            if (!ReadsIsTown(troops))
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns found nothing to change in They Married's starting troops for new lords; that support is inactive, likely because a They Married update changed it.");
                return;
            }

            harmony.Patch(troops, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleTheyMarried), "TroopsTranspiler")));
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

        // Same rule as castle recruiting by you and by lords: a castle the mod covers, with Recruit troops in castles on.
        private static IEnumerable<CodeInstruction> TroopsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleVolunteerPatch), "RecruitTown");
        }
    }
}
