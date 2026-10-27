using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // Deliver a Herd when the public gate is closed: peacetime denial, or wartime castle_outside (with sneak).
    // Not used for Go to the castle.
    internal static class CastleHerdGuardDelivery
    {
        private const string DeniedMenuId = "menu_castle_entry_denied";
        private const string OutsideMenuId = "castle_outside";
        private const string OfferMenuId = "castle_market_towns_herd_guard_offer";
        private const string BackgroundMesh = "encounter_guards";

        // Guards keep half the purse; the rest is paid as the quest reward.
        private const int PlayerRewardPercent = 50;

        private static readonly Type QuestType = AccessTools.Inner(typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "HeadmanNeedsToDeliverAHerdIssueQuest");
        private static readonly FieldInfo TargetSettlementField = QuestType == null ? null : AccessTools.Field(QuestType, "_targetSettlement");
        private static readonly FieldInfo RewardGoldField = QuestType == null ? null : AccessTools.Field(QuestType, "_rewardGold");
        private static readonly FieldInfo AnimalCountField = QuestType == null ? null : AccessTools.Field(QuestType, "_animalCountToDeliver");
        private static readonly FieldInfo HerdTypeField = QuestType == null ? null : AccessTools.Field(QuestType, "_herdTypeToDeliver");

        private static string _returnMenuId = DeniedMenuId;

        internal static void AddMenus(CampaignGameStarter starter)
        {
            if (QuestType == null || TargetSettlementField == null || RewardGoldField == null || AnimalCountField == null || HerdTypeField == null)
            {
                return;
            }

            starter.AddGameMenuOption(DeniedMenuId, "castle_market_towns_herd_deliver_to_guards", "{=cmt_herd_deliver_to_guards}Deliver Herd to Guards", DeliverCondition, DeliverConsequence, false, 0, false, null);
            // After sneak on castle_outside (war / neutral crime disguise); before besiege.
            starter.AddGameMenuOption(OutsideMenuId, "castle_market_towns_herd_deliver_to_guards_war", "{=cmt_herd_deliver_to_guards}Deliver Herd to Guards", DeliverCondition, DeliverConsequence, false, 2, false, null);
            // Same-faction moderate/severe crime uses the town FaceCharges menu (already castle-aware).
            starter.AddGameMenuOption("town_inside_criminal", "castle_market_towns_herd_deliver_to_guards_crime", "{=cmt_herd_deliver_to_guards}Deliver Herd to Guards", CriminalDeliverCondition, DeliverConsequence, false, 0, false, null);

            starter.AddGameMenu(OfferMenuId, "{=cmt_herd_guard_offer}The guards offer to take the herd in for you and hand it to the buyer inside, but they will keep a cut of the payment. You would receive {GUARD_HERD_REWARD}{GOLD_ICON}.", OfferInit, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(OfferMenuId, "castle_market_towns_herd_guard_accept", "{=cmt_herd_guard_accept}Accept", AcceptCondition, AcceptConsequence, false, -1, false, null);
            starter.AddGameMenuOption(OfferMenuId, "castle_market_towns_herd_guard_reject", "{=cmt_herd_guard_reject}Reject", RejectCondition, RejectConsequence, true, -1, false, null);
        }

        private static bool FeatureOn()
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || settings.QuestDeliverHerd;
        }

        private static QuestBase FindQuestAt(Settlement settlement)
        {
            if (!FeatureOn() || settlement == null || !settlement.IsCastle || Campaign.Current == null || Campaign.Current.QuestManager == null)
            {
                return null;
            }

            foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
            {
                if (IsOngoingHerdQuestFor(quest, settlement))
                {
                    return quest;
                }
            }

            return null;
        }

        private static bool IsOngoingHerdQuestFor(QuestBase quest, Settlement settlement)
        {
            if (quest == null || !quest.IsOngoing || QuestType == null || quest.GetType() != QuestType || TargetSettlementField == null)
            {
                return false;
            }

            return TargetSettlementField.GetValue(quest) as Settlement == settlement;
        }

        private static int AnimalCountOnPlayer(QuestBase quest)
        {
            ItemObject herd = HerdTypeField.GetValue(quest) as ItemObject;
            if (herd == null || PartyBase.MainParty == null || PartyBase.MainParty.ItemRoster == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ItemRosterElement element in PartyBase.MainParty.ItemRoster)
            {
                if (element.EquipmentElement.Item == herd)
                {
                    count += element.Amount;
                }
            }

            return count;
        }

        private static int RequiredAnimals(QuestBase quest)
        {
            return (int)AnimalCountField.GetValue(quest);
        }

        private static int ReducedReward(QuestBase quest)
        {
            int full = (int)RewardGoldField.GetValue(quest);
            return Math.Max(0, full * PlayerRewardPercent / 100);
        }

        private static bool HasHerdReady(QuestBase quest)
        {
            return quest != null && AnimalCountOnPlayer(quest) >= RequiredAnimals(quest);
        }

        private static bool DeliverCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            Settlement settlement = Settlement.CurrentSettlement;
            // Only when the public gate is closed (denial, disguise, or FaceCharges) — not while entry is free.
            if (settlement == null || !CastleTownRules.Covered(settlement) || !CastleWarEntry.IsBlockedMarketEntry(settlement))
            {
                return false;
            }

            QuestBase quest = FindQuestAt(settlement);
            if (quest == null)
            {
                return false;
            }

            if (!HasHerdReady(quest))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_herd_guard_need_animals}You do not have the full herd with you.");
            }

            return true;
        }

        private static bool CriminalDeliverCondition(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || !settlement.IsCastle)
            {
                return false;
            }

            return DeliverCondition(args);
        }

        private static void DeliverConsequence(MenuCallbackArgs args)
        {
            _returnMenuId = DeniedMenuId;
            if (Campaign.Current != null && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu != null)
            {
                string id = Campaign.Current.CurrentMenuContext.GameMenu.StringId;
                if (!string.IsNullOrEmpty(id))
                {
                    _returnMenuId = id;
                }
            }

            GameMenu.SwitchToMenu(OfferMenuId);
        }

        private static void OfferInit(MenuCallbackArgs args)
        {
            args.MenuContext.SetBackgroundMeshName(BackgroundMesh);
            QuestBase quest = FindQuestAt(Settlement.CurrentSettlement);
            int reward = quest == null ? 0 : ReducedReward(quest);
            MBTextManager.SetTextVariable("GUARD_HERD_REWARD", reward);
        }

        private static bool AcceptCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            QuestBase quest = FindQuestAt(Settlement.CurrentSettlement);
            if (quest == null)
            {
                return false;
            }

            if (!HasHerdReady(quest))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_herd_guard_need_animals}You do not have the full herd with you.");
            }

            return true;
        }

        private static void AcceptConsequence(MenuCallbackArgs args)
        {
            QuestBase quest = FindQuestAt(Settlement.CurrentSettlement);
            if (quest == null || !HasHerdReady(quest))
            {
                GameMenu.SwitchToMenu(_returnMenuId);
                return;
            }

            int reduced = ReducedReward(quest);
            RewardGoldField.SetValue(quest, reduced);
            quest.CompleteQuestWithSuccess();
            GameMenu.SwitchToMenu(OutsideMenuId);
        }

        private static bool RejectCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private static void RejectConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu(_returnMenuId);
        }
    }
}
