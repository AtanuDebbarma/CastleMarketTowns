using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // When a prepared castle loses CMT coverage, soft-cancel ongoing quests tied to it (and open issues from its notables).
    // Individual MCM quest toggles do not call this — notables stay, so those quests can still finish.
    internal static class CastleQuestCoverageCancel
    {
        private static readonly Type HerdQuestType = AccessTools.Inner(typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "HeadmanNeedsToDeliverAHerdIssueQuest");
        private static readonly FieldInfo HerdAnimalCountField = HerdQuestType == null ? null : AccessTools.Field(HerdQuestType, "_animalCountToDeliver");

        private static readonly Type[] TrackedQuestTypes = BuildQuestTypes();
        private static readonly Type[] TrackedIssueTypes = BuildIssueTypes();

        private static Type[] BuildQuestTypes()
        {
            List<Type> types = new List<Type>();
            AddInner(types, typeof(EscortMerchantCaravanIssueBehavior), "EscortMerchantCaravanIssueQuest");
            AddInner(types, typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "MerchantNeedsHelpWithOutlawsIssueQuest");
            AddInner(types, typeof(ArtisanOverpricedGoodsIssueBehavior), "ArtisanOverpricedGoodsIssueQuest");
            AddInner(types, typeof(ArtisanCantSellProductsAtAFairPriceIssueBehavior), "ArtisanCantSellProductsAtAFairPriceIssueQuest");
            AddInner(types, typeof(CaravanAmbushIssueBehavior), "CaravanAmbushIssueQuest");
            AddInner(types, typeof(MerchantArmyOfPoachersIssueBehavior), "MerchantArmyOfPoachersIssueQuest");
            AddInner(types, typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "HeadmanNeedsToDeliverAHerdIssueQuest");
            AddInner(types, AccessTools.TypeByName("SandBox.Issues.RuralNotableInnAndOutIssueBehavior"), "RuralNotableInnAndOutIssueQuest");
            AddInner(types, AccessTools.TypeByName("SandBox.Issues.FamilyFeudIssueBehavior"), "FamilyFeudIssueQuest");
            return types.ToArray();
        }

        private static Type[] BuildIssueTypes()
        {
            List<Type> types = new List<Type>();
            AddInner(types, typeof(EscortMerchantCaravanIssueBehavior), "EscortMerchantCaravanIssue");
            AddInner(types, typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "MerchantNeedsHelpWithOutlawsIssue");
            AddInner(types, typeof(ArtisanOverpricedGoodsIssueBehavior), "ArtisanOverpricedGoodsIssue");
            AddInner(types, typeof(ArtisanCantSellProductsAtAFairPriceIssueBehavior), "ArtisanCantSellProductsAtAFairPriceIssue");
            AddInner(types, typeof(CaravanAmbushIssueBehavior), "CaravanAmbushIssue");
            AddInner(types, typeof(MerchantArmyOfPoachersIssueBehavior), "MerchantArmyOfPoachersIssue");
            AddInner(types, typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "HeadmanNeedsToDeliverAHerdIssue");
            AddInner(types, AccessTools.TypeByName("SandBox.Issues.RuralNotableInnAndOutIssueBehavior"), "RuralNotableInnAndOutIssue");
            AddInner(types, AccessTools.TypeByName("SandBox.Issues.FamilyFeudIssueBehavior"), "FamilyFeudIssue");
            return types.ToArray();
        }

        private static void AddInner(List<Type> types, Type behavior, string nestedName)
        {
            if (behavior == null || string.IsNullOrEmpty(nestedName))
            {
                return;
            }

            Type nested = AccessTools.Inner(behavior, nestedName);
            if (nested != null)
            {
                types.Add(nested);
            }
        }

        // castleId|questId — scoped so retries for one castle cannot cancel another castle's failures.
        internal static string QuestFailureKey(Settlement castle, QuestBase quest)
        {
            if (castle == null || quest == null || string.IsNullOrEmpty(quest.StringId))
            {
                return null;
            }

            return castle.StringId + "|" + quest.StringId;
        }

        // castleId|ownerId|issueType
        internal static string IssueFailureKey(Settlement castle, IssueBase issue)
        {
            if (castle == null || issue == null)
            {
                return null;
            }

            Hero owner = issue.IssueOwner;
            string ownerId = owner == null ? "?" : owner.StringId;
            return castle.StringId + "|" + ownerId + "|" + issue.GetType().FullName;
        }

        internal static void ClearFailuresForCastle(Settlement castle, HashSet<string> failedQuestIds, HashSet<string> failedIssueKeys)
        {
            if (castle == null)
            {
                return;
            }

            string prefix = castle.StringId + "|";
            RemovePrefixed(failedQuestIds, prefix);
            RemovePrefixed(failedIssueKeys, prefix);
        }

        private static void RemovePrefixed(HashSet<string> values, string prefix)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            List<string> remove = new List<string>();
            foreach (string value in values)
            {
                if (value != null && value.StartsWith(prefix, StringComparison.Ordinal))
                {
                    remove.Add(value);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                values.Remove(remove[i]);
            }
        }

        // Returns true when every matched quest/issue cancelled successfully.
        // failedQuestIds / failedIssueKeys retain castle-scoped identities; retryStoredFailures includes them after givers move.
        internal static bool CancelForLostCoverage(Settlement settlement, HashSet<string> failedQuestIds, HashSet<string> failedIssueKeys, bool retryStoredFailures)
        {
            if (settlement == null || !settlement.IsCastle || Campaign.Current == null)
            {
                return true;
            }

            if (failedQuestIds == null)
            {
                failedQuestIds = new HashSet<string>();
            }

            if (failedIssueKeys == null)
            {
                failedIssueKeys = new HashSet<string>();
            }

            TextObject log = new TextObject("{=cmt_quest_cancel_coverage}{SETTLEMENT} no longer has a castle market, so this task there was called off.");
            log.SetTextVariable("SETTLEMENT", settlement.EncyclopediaLinkWithName);

            bool ok = CancelQuests(settlement, log, failedQuestIds, retryStoredFailures);
            if (!CancelIssues(settlement, log, failedIssueKeys, retryStoredFailures))
            {
                ok = false;
            }

            return ok;
        }

        private static bool CancelQuests(Settlement settlement, TextObject log, HashSet<string> failedQuestIds, bool retryStoredFailures)
        {
            if (Campaign.Current.QuestManager == null)
            {
                return true;
            }

            List<QuestBase> toCancel = new List<QuestBase>();
            foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
            {
                if (quest == null || !quest.IsOngoing || !IsTrackedQuest(quest.GetType()))
                {
                    continue;
                }

                string key = QuestFailureKey(settlement, quest);
                if (TiesToCastle(quest, settlement) || (retryStoredFailures && key != null && failedQuestIds.Contains(key)))
                {
                    toCancel.Add(quest);
                }
            }

            bool ok = true;
            foreach (QuestBase quest in toCancel)
            {
                string key = QuestFailureKey(settlement, quest);
                if (TryCancelQuest(quest, log))
                {
                    if (key != null)
                    {
                        failedQuestIds.Remove(key);
                    }
                }
                else
                {
                    if (key != null)
                    {
                        failedQuestIds.Add(key);
                    }

                    ok = false;
                }
            }

            return ok;
        }

        private static bool TryCancelQuest(QuestBase quest, TextObject log)
        {
            bool isHerd = HerdQuestType != null && HerdAnimalCountField != null && quest.GetType() == HerdQuestType;
            object originalAnimalCount = null;
            bool animalCountChanged = false;

            try
            {
                // Deliver a Herd OnCanceled strips animals; coverage loss should not.
                if (isHerd)
                {
                    originalAnimalCount = HerdAnimalCountField.GetValue(quest);
                    HerdAnimalCountField.SetValue(quest, 0);
                    animalCountChanged = true;
                }

                quest.CompleteQuestWithCancel(log);
                return true;
            }
            catch (Exception exception)
            {
                if (animalCountChanged)
                {
                    try
                    {
                        HerdAnimalCountField.SetValue(quest, originalAnimalCount);
                    }
                    catch (Exception restoreException)
                    {
                        Debug.Print("Castle Market Towns could not restore herd animal count after a failed coverage cancel: " + restoreException);
                    }
                }

                Debug.Print("Castle Market Towns could not cancel quest " + quest.StringId + " after coverage loss: " + exception);
                return false;
            }
        }

        private static bool CancelIssues(Settlement settlement, TextObject log, HashSet<string> failedIssueKeys, bool retryStoredFailures)
        {
            if (Campaign.Current.IssueManager == null || Campaign.Current.IssueManager.Issues == null)
            {
                return true;
            }

            List<IssueBase> toCancel = new List<IssueBase>();
            foreach (KeyValuePair<Hero, IssueBase> pair in Campaign.Current.IssueManager.Issues)
            {
                IssueBase issue = pair.Value;
                if (issue == null || !IsTrackedIssue(issue.GetType()))
                {
                    continue;
                }

                // Already running as a quest — handled above.
                if (issue.IssueQuest != null && issue.IssueQuest.IsOngoing)
                {
                    continue;
                }

                string key = IssueFailureKey(settlement, issue);
                if (TiesToCastle(issue, settlement) || (retryStoredFailures && key != null && failedIssueKeys.Contains(key)))
                {
                    toCancel.Add(issue);
                }
            }

            bool ok = true;
            foreach (IssueBase issue in toCancel)
            {
                string key = IssueFailureKey(settlement, issue);
                try
                {
                    issue.CompleteIssueWithCancel(log);
                    if (key != null)
                    {
                        failedIssueKeys.Remove(key);
                    }
                }
                catch (Exception exception)
                {
                    ok = false;
                    if (key != null)
                    {
                        failedIssueKeys.Add(key);
                    }

                    Debug.Print("Castle Market Towns could not cancel issue after coverage loss: " + exception);
                }
            }

            return ok;
        }

        private static bool TiesToCastle(QuestBase quest, Settlement castle)
        {
            if (!IsTrackedQuest(quest.GetType()))
            {
                return false;
            }

            Hero giver = quest.QuestGiver;
            if (giver != null && giver.IsNotable && SettlementTiesToCastle(giver.CurrentSettlement, castle))
            {
                return true;
            }

            return HasLinkedSettlementOrVillage(quest, castle);
        }

        private static bool TiesToCastle(IssueBase issue, Settlement castle)
        {
            if (SettlementTiesToCastle(issue.IssueSettlement, castle))
            {
                return true;
            }

            Hero owner = issue.IssueOwner;
            if (owner != null && owner.IsNotable && SettlementTiesToCastle(owner.CurrentSettlement, castle))
            {
                return true;
            }

            return HasLinkedSettlementOrVillage(issue, castle);
        }

        // Castle itself, or a village bound to that castle (Family Feud, herd giver, Inn and Out giver, …).
        private static bool SettlementTiesToCastle(Settlement settlement, Settlement castle)
        {
            if (settlement == null || castle == null)
            {
                return false;
            }

            if (settlement == castle)
            {
                return true;
            }

            return settlement.IsVillage
                && settlement.Village != null
                && settlement.Village.Bound == castle;
        }

        private static bool IsTrackedQuest(Type type)
        {
            for (int i = 0; i < TrackedQuestTypes.Length; i++)
            {
                if (TrackedQuestTypes[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTrackedIssue(Type type)
        {
            for (int i = 0; i < TrackedIssueTypes.Length; i++)
            {
                if (TrackedIssueTypes[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        // Settlement fields (direct castle or bound village) and Village fields (Bound == castle).
        private static bool HasLinkedSettlementOrVillage(object instance, Settlement castle)
        {
            if (instance == null)
            {
                return false;
            }

            Type type = instance.GetType();
            while (type != null && type != typeof(object))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field.FieldType == typeof(Settlement))
                    {
                        if (SettlementTiesToCastle(field.GetValue(instance) as Settlement, castle))
                        {
                            return true;
                        }
                    }
                    else if (field.FieldType == typeof(Village))
                    {
                        Village village = field.GetValue(instance) as Village;
                        if (village != null && village.Bound == castle)
                        {
                            return true;
                        }
                    }
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
