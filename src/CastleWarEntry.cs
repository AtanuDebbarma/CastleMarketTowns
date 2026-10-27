using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;

namespace CastleMarketTowns
{
    // Covered castles mirror town settlement-entry rules for war and crime, keep castle NoAccess for bad lord relation,
    // and use castle wording for sneak on castle_outside. Peacetime relation refuse stays on menu_castle_entry_denied.
    internal static class CastleWarEntry
    {
        private const string OutsideMenuId = "castle_outside";

        private static readonly MethodInfo DisguiseConditionMethod = AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_town_disguise_yourself_on_condition");
        private static readonly MethodInfo DisguiseConsequenceMethod = AccessTools.Method(typeof(EncounterGameMenuBehavior), "game_menu_town_initial_disguise_yourself_on_consequence");
        private static readonly MethodInfo InitializeAccessDetailsMethod = AccessTools.Method(typeof(EncounterGameMenuBehavior), "InitializeAccessDetails");
        private static readonly FieldInfo AccessDetailsField = AccessTools.Field(typeof(EncounterGameMenuBehavior), "_accessDetails");

        internal static void AddMenus(CampaignGameStarter starter)
        {
            if (DisguiseConditionMethod == null || DisguiseConsequenceMethod == null)
            {
                return;
            }

            // After Approach the gates, before Besiege — same order as town_outside.
            starter.AddGameMenuOption(OutsideMenuId, "castle_market_towns_disguise", "{=cmt_castle_disguise}Disguise yourself and sneak through the castle gate. ({SNEAK_CHANCE}%)", DisguiseCondition, DisguiseConsequence, false, 1, false, null);
        }

        private static EncounterGameMenuBehavior Behavior()
        {
            return Campaign.Current == null ? null : Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
        }

        private static bool DisguiseCondition(MenuCallbackArgs args)
        {
            if (!CastleTownRules.Covered(Settlement.CurrentSettlement))
            {
                return false;
            }

            EncounterGameMenuBehavior behavior = Behavior();
            if (behavior == null)
            {
                return false;
            }

            return (bool)DisguiseConditionMethod.Invoke(behavior, new object[] { args });
        }

        private static void DisguiseConsequence(MenuCallbackArgs args)
        {
            EncounterGameMenuBehavior behavior = Behavior();
            if (behavior == null)
            {
                return;
            }

            // Starts the same night / contact flow as towns; SwitchToMenu patches below send success/back to castle menus.
            DisguiseConsequenceMethod.Invoke(behavior, new object[] { args });
        }

        internal static bool IsDisguiseAccess(SettlementAccessModel.AccessDetails access)
        {
            return access.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess
                && access.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise;
        }

        internal static bool IsBlockedMarketEntry(Settlement settlement)
        {
            if (settlement == null || Campaign.Current == null || Campaign.Current.Models == null)
            {
                return false;
            }

            SettlementAccessModel.AccessDetails access;
            Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(settlement, out access);
            if (access.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess)
            {
                return true;
            }

            if (IsDisguiseAccess(access))
            {
                return true;
            }

            return access.PreliminaryActionObligation == SettlementAccessModel.PreliminaryActionObligation.Optional
                && access.PreliminaryActionType == SettlementAccessModel.PreliminaryActionType.FaceCharges;
        }

        private static bool CurrentIsDisguise()
        {
            EncounterGameMenuBehavior behavior = Behavior();
            if (behavior == null || AccessDetailsField == null)
            {
                return false;
            }

            object boxed = AccessDetailsField.GetValue(behavior);
            if (!(boxed is SettlementAccessModel.AccessDetails))
            {
                return false;
            }

            return IsDisguiseAccess((SettlementAccessModel.AccessDetails)boxed);
        }

        private static bool HasTownLevelCrime(IFaction faction)
        {
            CrimeModel crime = Campaign.Current.Models.CrimeModel;
            return crime.IsPlayerCrimeRatingModerate(faction) || crime.IsPlayerCrimeRatingSevere(faction);
        }

        // Vanilla: same-faction refuse at relation <= -4 (leader not you); neutral refuse at relation < 0.
        private static bool HasBadLordRelation(Settlement settlement)
        {
            if (settlement == null || settlement.OwnerClan == null || settlement.OwnerClan.Leader == null || Hero.MainHero == null)
            {
                return false;
            }

            if (settlement.Town != null && settlement.Town.IsOwnerUnassigned)
            {
                return false;
            }

            Hero leader = settlement.OwnerClan.Leader;
            if (leader == Hero.MainHero)
            {
                return false;
            }

            float relation = leader.GetRelationWithPlayer();
            IFaction playerFaction = Hero.MainHero.MapFaction;
            IFaction settlementFaction = settlement.MapFaction;
            if (DiplomacyHelper.IsSameFactionAndNotEliminated(playerFaction, settlementFaction))
            {
                return relation <= -4f;
            }

            if (FactionManager.IsNeutralWithFaction(playerFaction, settlementFaction))
            {
                return relation < 0f;
            }

            return false;
        }

        private static void RefuseForBadRelation(ref SettlementAccessModel.AccessDetails accessDetails)
        {
            accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
            accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.None;
            accessDetails.LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.None;
            accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.RelationshipWithOwner;
            accessDetails.PreliminaryActionType = SettlementAccessModel.PreliminaryActionType.None;
            accessDetails.PreliminaryActionObligation = SettlementAccessModel.PreliminaryActionObligation.None;
        }

        // Match CanMainHeroEnterTown for war/crime; bad lord relation always wins over crime conversion.
        [HarmonyPatch(typeof(DefaultSettlementAccessModel), "CanMainHeroEnterCastle")]
        private static class CastleTownLikeAccessPatch
        {
            static void Postfix(Settlement settlement, ref SettlementAccessModel.AccessDetails accessDetails)
            {
                if (!CastleTownRules.Covered(settlement) || Hero.MainHero == null || settlement.MapFaction == null)
                {
                    return;
                }

                IFaction playerFaction = Hero.MainHero.MapFaction;
                IFaction settlementFaction = settlement.MapFaction;
                bool atWar = playerFaction != null && settlementFaction != null && playerFaction.IsAtWarWith(settlementFaction);

                // War uses disguise; peacetime bad relation is a hard refuse and must not be overridden by crime.
                if (!atWar && HasBadLordRelation(settlement))
                {
                    RefuseForBadRelation(ref accessDetails);
                    return;
                }

                if (DiplomacyHelper.IsSameFactionAndNotEliminated(playerFaction, settlementFaction))
                {
                    if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess)
                    {
                        accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.Direct;
                        if (HasTownLevelCrime(settlementFaction))
                        {
                            accessDetails.PreliminaryActionType = SettlementAccessModel.PreliminaryActionType.FaceCharges;
                            accessDetails.PreliminaryActionObligation = SettlementAccessModel.PreliminaryActionObligation.Optional;
                        }
                    }

                    return;
                }

                if (FactionManager.IsNeutralWithFaction(playerFaction, settlementFaction))
                {
                    if (HasTownLevelCrime(settlementFaction))
                    {
                        accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess;
                        accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.None;
                        accessDetails.LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise;
                        accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.CrimeRating;
                    }
                    else if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess
                        && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
                    {
                        accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.FullAccess;
                        accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.Direct;
                        accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.None;
                    }

                    return;
                }

                if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess
                    && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.HostileFaction)
                {
                    accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess;
                    accessDetails.LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise;
                }
            }
        }

        // Run FaceCharges before vanilla can SwitchToMenu("castle") on FullAccess + Direct.
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_castle_outside_on_init")]
        private static class CastleFaceChargesMenuPatch
        {
            static bool Prefix(EncounterGameMenuBehavior __instance, MenuCallbackArgs args)
            {
                if (!CastleTownRules.Covered(Settlement.CurrentSettlement) || AccessDetailsField == null || InitializeAccessDetailsMethod == null)
                {
                    return true;
                }

                InitializeAccessDetailsMethod.Invoke(__instance, new object[] { args });
                object boxed = AccessDetailsField.GetValue(__instance);
                if (!(boxed is SettlementAccessModel.AccessDetails))
                {
                    return true;
                }

                SettlementAccessModel.AccessDetails access = (SettlementAccessModel.AccessDetails)boxed;
                if (access.PreliminaryActionObligation == SettlementAccessModel.PreliminaryActionObligation.Optional
                    && access.PreliminaryActionType == SettlementAccessModel.PreliminaryActionType.FaceCharges)
                {
                    GameMenu.SwitchToMenu("town_inside_criminal");
                    return false;
                }

                return true;
            }
        }

        // Town disguise handlers hard-code town / town_outside; redirect when the settlement is a covered castle.
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "menu_sneak_into_town_succeeded_continue_on_consequence")]
        private static class CastleDisguiseSuccessPatch
        {
            static bool Prefix()
            {
                if (!CastleTownRules.Covered(Settlement.CurrentSettlement))
                {
                    return true;
                }

                GameMenu.SwitchToMenu("castle");
                return false;
            }
        }

        // Disguise leave / fail handlers on <>c hard-code town_outside.
        [HarmonyPatch]
        private static class CastleDisguiseBackToOutsidePatch
        {
            static bool Prepare()
            {
                return AccessTools.Inner(typeof(EncounterGameMenuBehavior), "<>c") != null;
            }

            static IEnumerable<MethodBase> TargetMethods()
            {
                System.Type nested = AccessTools.Inner(typeof(EncounterGameMenuBehavior), "<>c");
                if (nested == null)
                {
                    yield break;
                }

                string[] names = new string[]
                {
                    "<AddGameMenus>b__17_7",
                    "<AddGameMenus>b__17_8",
                    "<AddGameMenus>b__17_10",
                };
                for (int i = 0; i < names.Length; i++)
                {
                    MethodInfo method = AccessTools.Method(nested, names[i]);
                    if (method != null)
                    {
                        yield return method;
                    }
                }
            }

            static bool Prefix()
            {
                if (!CastleTownRules.Covered(Settlement.CurrentSettlement))
                {
                    return true;
                }

                GameMenu.SwitchToMenu(OutsideMenuId);
                return false;
            }
        }

        // escape_continue applies crime then town_outside — keep Apply, land on castle_outside.
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "escape_continue_on_consequence")]
        private static class CastleDisguiseEscapePatch
        {
            static bool Prefix()
            {
                if (!CastleTownRules.Covered(Settlement.CurrentSettlement))
                {
                    return true;
                }

                if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.MapFaction != null)
                {
                    ChangeCrimeRatingAction.Apply(Settlement.CurrentSettlement.MapFaction, 10f, true);
                }

                GameMenu.SwitchToMenu(OutsideMenuId);
                return false;
            }
        }

        // Towns do not offer "request entry" while sneak is available.
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_town_guard_request_shelter_on_condition")]
        private static class CastleHideRequestEntryWhenDisguisePatch
        {
            static void Postfix(ref bool __result)
            {
                if (!__result || !CastleTownRules.Covered(Settlement.CurrentSettlement) || !CurrentIsDisguise())
                {
                    return;
                }

                __result = false;
            }
        }
    }
}
