using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;

namespace CastleMarketTowns
{
    // Vanilla Lord Needs Garrison Troops only adds "Talk to the garrison commander" to castle_guard (Approach the gates).
    // Covered castles walk in Direct like towns, so that menu is skipped. Towns also have the option on the main town menu;
    // mirror that on the main castle market menu (not under Go to the castle — donate/manage garrison stay there).
    internal static class CastleGarrisonCommander
    {
        private static readonly MethodInfo ConditionMethod = AccessTools.Method(typeof(LordNeedsGarrisonTroopsIssueQuestBehavior), "talk_to_garrison_commander_on_condition");
        private static readonly MethodInfo ConsequenceMethod = AccessTools.Method(typeof(LordNeedsGarrisonTroopsIssueQuestBehavior), "talk_to_garrison_commander_on_consequence");

        internal static void AddMenus(CampaignGameStarter starter, int insertIndex)
        {
            if (ConditionMethod == null || ConsequenceMethod == null)
            {
                return;
            }

            starter.AddGameMenuOption(
                "castle",
                "castle_market_towns_talk_garrison_commander",
                "{=FirEOQaI}Talk to the garrison commander",
                Condition,
                Consequence,
                false,
                insertIndex,
                false,
                null);
        }

        private static LordNeedsGarrisonTroopsIssueQuestBehavior Behavior()
        {
            return Campaign.Current == null ? null : Campaign.Current.GetCampaignBehavior<LordNeedsGarrisonTroopsIssueQuestBehavior>();
        }

        private static bool Condition(MenuCallbackArgs args)
        {
            if (!CastleTownRules.Covered(Settlement.CurrentSettlement))
            {
                return false;
            }

            LordNeedsGarrisonTroopsIssueQuestBehavior behavior = Behavior();
            if (behavior == null)
            {
                return false;
            }

            return (bool)ConditionMethod.Invoke(behavior, new object[] { args });
        }

        private static void Consequence(MenuCallbackArgs args)
        {
            LordNeedsGarrisonTroopsIssueQuestBehavior behavior = Behavior();
            if (behavior == null)
            {
                return;
            }

            ConsequenceMethod.Invoke(behavior, new object[] { args });
        }
    }
}
