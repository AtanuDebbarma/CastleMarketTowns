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
    // Artisan Can't Sell when the public gate is closed: peacetime denial, or wartime/crime castle_outside.
    // Not used for Go to the castle. Mirrors Deliver Herd to Guards with product wording.
    internal static class CastleArtisanGuardDelivery
    {
        private const string DeniedMenuId = "menu_castle_entry_denied";
        private const string OutsideMenuId = "castle_outside";
        private const string OfferMenuId = "castle_market_towns_artisan_guard_offer";
        private const string BackgroundMesh = "encounter_guards";

        // Guards keep half the purse; the rest is paid as the quest reward.
        private const int PlayerRewardPercent = 50;

        private static readonly Type QuestType = AccessTools.Inner(typeof(ArtisanCantSellProductsAtAFairPriceIssueBehavior), "ArtisanCantSellProductsAtAFairPriceIssueQuest");
        private static readonly FieldInfo TargetSettlementField = QuestType == null ? null : AccessTools.Field(QuestType, "_targetSettlement");
        private static readonly FieldInfo RewardGoldField = QuestType == null ? null : AccessTools.Field(QuestType, "_rewardGold");
        private static readonly FieldInfo AmountField = QuestType == null ? null : AccessTools.Field(QuestType, "_amountOfRawGoodsToBeDelivered");
        private static readonly FieldInfo DeliveredField = QuestType == null ? null : AccessTools.Field(QuestType, "_deliveredRawGoods");
        private static readonly FieldInfo ItemField = QuestType == null ? null : AccessTools.Field(QuestType, "_rawMaterialsToBeDelivered");
        private static readonly MethodInfo DeliverFullyMethod = QuestType == null ? null : AccessTools.Method(QuestType, "DeliverItemsFullyOnConsequence");

        private static string _returnMenuId = DeniedMenuId;

        internal static void AddMenus(CampaignGameStarter starter)
        {
            if (QuestType == null || TargetSettlementField == null || RewardGoldField == null || AmountField == null || DeliveredField == null || ItemField == null || DeliverFullyMethod == null)
            {
                return;
            }

            // After Deliver Herd to Guards on the same menus when both apply.
            starter.AddGameMenuOption(DeniedMenuId, "castle_market_towns_artisan_deliver_to_guards", "{=cmt_artisan_deliver_to_guards}Deliver Items to Guards", DeliverCondition, DeliverConsequence, false, 1, false, null);
            starter.AddGameMenuOption(OutsideMenuId, "castle_market_towns_artisan_deliver_to_guards_war", "{=cmt_artisan_deliver_to_guards}Deliver Items to Guards", DeliverCondition, DeliverConsequence, false, 3, false, null);
            starter.AddGameMenuOption("town_inside_criminal", "castle_market_towns_artisan_deliver_to_guards_crime", "{=cmt_artisan_deliver_to_guards}Deliver Items to Guards", CriminalDeliverCondition, DeliverConsequence, false, 1, false, null);

            starter.AddGameMenu(OfferMenuId, "{=cmt_artisan_guard_offer}The guards offer to take the {PRODUCT} in for you and hand it to the buyer inside, but they will keep a cut of the payment. You would receive {GUARD_ARTISAN_REWARD}{GOLD_ICON}.", OfferInit, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(OfferMenuId, "castle_market_towns_artisan_guard_accept", "{=cmt_artisan_guard_accept}Accept", AcceptCondition, AcceptConsequence, false, -1, false, null);
            starter.AddGameMenuOption(OfferMenuId, "castle_market_towns_artisan_guard_reject", "{=cmt_artisan_guard_reject}Reject", RejectCondition, RejectConsequence, true, -1, false, null);
        }

        private static bool FeatureOn()
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || settings.QuestArtisanCantSell;
        }

        private static QuestBase FindQuestAt(Settlement settlement)
        {
            if (!FeatureOn() || settlement == null || !settlement.IsCastle || Campaign.Current == null || Campaign.Current.QuestManager == null)
            {
                return null;
            }

            foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
            {
                if (IsOngoingArtisanQuestFor(quest, settlement))
                {
                    return quest;
                }
            }

            return null;
        }

        private static bool IsOngoingArtisanQuestFor(QuestBase quest, Settlement settlement)
        {
            if (quest == null || !quest.IsOngoing || QuestType == null || quest.GetType() != QuestType || TargetSettlementField == null)
            {
                return false;
            }

            return TargetSettlementField.GetValue(quest) as Settlement == settlement;
        }

        private static ItemObject Product(QuestBase quest)
        {
            return ItemField.GetValue(quest) as ItemObject;
        }

        private static int AmountNeeded(QuestBase quest)
        {
            return (int)AmountField.GetValue(quest);
        }

        private static int AlreadyDelivered(QuestBase quest)
        {
            return (int)DeliveredField.GetValue(quest);
        }

        private static int Remaining(QuestBase quest)
        {
            return Math.Max(0, AmountNeeded(quest) - AlreadyDelivered(quest));
        }

        private static int ProductCountOnPlayer(QuestBase quest)
        {
            ItemObject item = Product(quest);
            if (item == null || PartyBase.MainParty == null || PartyBase.MainParty.ItemRoster == null)
            {
                return 0;
            }

            return PartyBase.MainParty.ItemRoster.GetItemNumber(item);
        }

        private static int ReducedReward(QuestBase quest)
        {
            int full = (int)RewardGoldField.GetValue(quest);
            return Math.Max(0, full * PlayerRewardPercent / 100);
        }

        private static bool HasGoodsReady(QuestBase quest)
        {
            return quest != null && Remaining(quest) > 0 && ProductCountOnPlayer(quest) >= Remaining(quest);
        }

        private static void SetProductName(QuestBase quest)
        {
            ItemObject item = quest == null ? null : Product(quest);
            if (item != null)
            {
                MBTextManager.SetTextVariable("PRODUCT", item.Name);
            }
            else
            {
                MBTextManager.SetTextVariable("PRODUCT", new TextObject("{=cmt_artisan_guard_products}goods"));
            }
        }

        private static bool DeliverCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || !CastleTownRules.Covered(settlement) || !CastleWarEntry.IsBlockedMarketEntry(settlement))
            {
                return false;
            }

            QuestBase quest = FindQuestAt(settlement);
            if (quest == null)
            {
                return false;
            }

            SetProductName(quest);
            if (!HasGoodsReady(quest))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_artisan_guard_need_items}You do not have all of the {PRODUCT} with you.");
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
            SetProductName(quest);
            int reward = quest == null ? 0 : ReducedReward(quest);
            MBTextManager.SetTextVariable("GUARD_ARTISAN_REWARD", reward);
        }

        private static bool AcceptCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            QuestBase quest = FindQuestAt(Settlement.CurrentSettlement);
            if (quest == null)
            {
                return false;
            }

            SetProductName(quest);
            if (!HasGoodsReady(quest))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=cmt_artisan_guard_need_items}You do not have all of the {PRODUCT} with you.");
            }

            return true;
        }

        private static void AcceptConsequence(MenuCallbackArgs args)
        {
            QuestBase quest = FindQuestAt(Settlement.CurrentSettlement);
            if (quest == null || !HasGoodsReady(quest))
            {
                GameMenu.SwitchToMenu(_returnMenuId);
                return;
            }

            // Half reward first; vanilla DeliverItemsFullyOnConsequence removes goods, updates the journal task, and completes.
            RewardGoldField.SetValue(quest, ReducedReward(quest));
            DeliverFullyMethod.Invoke(quest, null);
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
