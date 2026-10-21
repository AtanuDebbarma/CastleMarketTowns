using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.Library;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CastleMarketTowns
{
    [HarmonyPatch(typeof(DefaultVolunteerModel), "GetDailyVolunteerProductionProbability")]
    internal static class CastleRecruitSpeedPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleRecruitSpeedPatch), "TownOrCastle");
        }

        public static bool TownOrCastle(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || settlement.IsCastle);
        }

        static void Postfix(Hero hero, Settlement settlement, ref float __result)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            Settlement home = settlement;
            if (home == null && hero != null)
            {
                home = hero.CurrentSettlement;
            }

            if (settings == null || !settings.Enabled || home == null || !CastleMarketTownsBehavior.AppliesTo(home))
            {
                return;
            }

            __result = __result * (settings.RecruitPercent / 100f);
        }
    }

    [HarmonyPatch(typeof(TavernEmployeesCampaignBehavior), "LocationCharactersAreReadyToSpawn")]
    internal static class CastleTavernStaffPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleTavernStaffPatch), "IsTownOrMiniCastle");
        }

        public static bool IsTownOrMiniCastle(Settlement settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            if (settlement.IsTown)
            {
                return true;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return CastleMarketTownsBehavior.AppliesTo(settlement) && settings != null && settings.EnableTavern;
        }
    }

    [HarmonyPatch(typeof(BoardGameCampaignBehavior), "LocationCharactersAreReadyToSpawn")]
    internal static class CastleTavernGameHostPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleTavernStaffPatch), "IsTownOrMiniCastle");
        }
    }

    internal static class SettlementGetter
    {
        public static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, string getter, System.Type owner, string helper)
        {
            MethodInfo target = AccessTools.Method(owner, helper);
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == getter && called.DeclaringType == typeof(Settlement))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = target;
                }

                yield return instruction;
            }
        }

        public static List<MethodBase> Existing(params MethodBase[] methods)
        {
            List<MethodBase> found = new List<MethodBase>();
            foreach (MethodBase method in methods)
            {
                if (method != null)
                {
                    found.Add(method);
                }
            }

            return found;
        }
    }

    [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "LocationCharactersAreReadyToSpawn")]
    internal static class CastleTavernCrowdPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsCastle", typeof(CastleTavernCrowdPatch), "SkipsCrowd");
        }

        public static bool SkipsCrowd(Settlement settlement)
        {
            if (settlement == null || !settlement.IsCastle)
            {
                return false;
            }

            TaleWorlds.CampaignSystem.Settlements.Locations.Location location = CampaignMission.Current == null ? null : CampaignMission.Current.Location;
            return location == null || location.StringId != "tavern" || !CastleMarketTownsBehavior.TavernAvailable(settlement);
        }
    }

    [HarmonyPatch]
    internal static class CastleTavernMercenaryPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(RecruitmentAgentSpawnBehavior), "AddMercenaryCharacterToTavern"),
                AccessTools.Method(typeof(RecruitmentAgentSpawnBehavior), "CheckIfMercenaryCharacterNeedsToRefresh"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "buy_mercenaries_condition"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "buy_mercenaries_on_consequence"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "conversation_mercenary_recruit_plural_start_on_condition"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "conversation_mercenary_recruit_single_start_on_condition"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleTavernMercenaryPatch), "TownOrCastleTavern");
        }

        public static bool TownOrCastleTavern(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || CastleMarketTownsBehavior.TavernAvailable(settlement));
        }
    }

    [HarmonyPatch]
    internal static class CastleVolunteerPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "HourlyTickParty"),
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "CheckRecruiting"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleVolunteerPatch), "RecruitTown");
        }

        public static bool RecruitTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || CastleMarketTownsBehavior.RecruitsAvailable(settlement));
        }
    }

    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "CheckRecruiting")]
    internal static class CastleRecruiterScopePatch
    {
        static bool Prefix(MobileParty mobileParty, Settlement settlement)
        {
            return settlement == null || !settlement.IsCastle || CastleMarketTownsBehavior.RecruiterAllowed(mobileParty);
        }
    }

    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior), "AiHourlyTick")]
    internal static class CastleRecruitVisitPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int volunteers = codes.FindIndex(delegate(CodeInstruction code)
            {
                MethodInfo called = code.operand as MethodInfo;
                return called != null && called.Name == "GetApproximateVolunteersCanBeRecruitedDataFromSettlement";
            });

            for (int i = volunteers - 1; i >= 0; i--)
            {
                MethodInfo called = codes[i].operand as MethodInfo;
                if (called != null && called.Name == "get_IsCastle" && called.DeclaringType == typeof(Settlement))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.Method(typeof(CastleRecruitVisitPatch), "SkipsRecruitVisit");
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_1));
                    break;
                }
            }

            return codes;
        }

        public static bool SkipsRecruitVisit(Settlement settlement, MobileParty party)
        {
            if (settlement == null || !settlement.IsCastle)
            {
                return false;
            }

            return !CastleMarketTownsBehavior.RecruitsAvailable(settlement) || !CastleMarketTownsBehavior.RecruiterAllowed(party);
        }
    }

    [HarmonyPatch(typeof(ItemConsumptionBehavior), "MakeConsumption")]
    internal static class CastleConsumptionPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo target = AccessTools.Method(typeof(CastleMarketTownsBehavior), "CastleConsumptionBudget");
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == "CalculateDailySettlementBudgetForItemCategory")
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = target;
                }

                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(CaravansCampaignBehavior), "GetTradeScoreForTown")]
    internal static class CastleCaravanLowStockPatch
    {
        private const float LowStockRatio = 0.35f;

        static void Postfix(Town town, ref float __result)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            Settlement settlement = town == null ? null : town.Settlement;
            if (__result <= 0f || settings == null || !settings.CaravansFavourLowStock || settlement == null || !settlement.IsCastle || !CastleMarketTownsBehavior.AppliesTo(settlement))
            {
                return;
            }

            float ratio = CastleMarketTownsBehavior.StockRatio(settlement);
            if (ratio < LowStockRatio)
            {
                __result *= 1f + settings.CaravanBonusPercent / 100f * (1f - ratio / LowStockRatio);
            }
        }
    }

    [HarmonyPatch(typeof(DefaultVolunteerModel), "GetBasicVolunteer")]
    internal static class CastleNobleVolunteerPatch
    {
        static void Postfix(Hero sellerHero, ref CharacterObject __result)
        {
            if (sellerHero == null || sellerHero.IsRuralNotable || sellerHero.Culture == null || sellerHero.Culture.EliteBasicTroop == null)
            {
                return;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !CastleMarketTownsBehavior.RecruitsAvailable(sellerHero.CurrentSettlement))
            {
                return;
            }

            if (TaleWorlds.Core.MBRandom.RandomFloat * 100f < settings.NobleRecruitPercent)
            {
                __result = sellerHero.Culture.EliteBasicTroop;
            }
        }
    }

    [HarmonyPatch]
    internal static class CastleRebellionPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RebellionsCampaignBehavior), "DailyTickSettlement");
            yield return AccessTools.Method(typeof(RebellionsCampaignBehavior), "OnSiegeStarted");
            yield return AccessTools.Method(typeof(RebellionsCampaignBehavior), "OnGameLoaded");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SettlementGetter.Redirect(instructions, "get_IsTown", typeof(CastleRebellionPatch), "CountsAsRebelSettlement");
        }

        public static bool CountsAsRebelSettlement(Settlement settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            if (settlement.IsTown)
            {
                return true;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (!CastleMarketTownsBehavior.AppliesTo(settlement))
            {
                return false;
            }

            if (settings == null)
            {
                return true;
            }

            return settings.AllowRebellion;
        }
    }

    internal enum CastleQuest
    {
        EscortCaravan,
        Brigands,
        OverpricedMaterials,
        ArtisanCantSell,
        CaravanAmbush,
        ArmyOfPoachers,
        InnAndOut,
        DeliverHerd,
        FamilyFeud
    }

    internal static class CastleQuestRules
    {
        public static bool Enabled(CastleQuest quest)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null)
            {
                return true;
            }

            if (!settings.Enabled || !settings.AllowNotableQuests)
            {
                return false;
            }

            switch (quest)
            {
                case CastleQuest.EscortCaravan: return settings.QuestEscortCaravan;
                case CastleQuest.Brigands: return settings.QuestBrigands;
                case CastleQuest.OverpricedMaterials: return settings.QuestOverpricedMaterials;
                case CastleQuest.ArtisanCantSell: return settings.QuestArtisanCantSell;
                case CastleQuest.CaravanAmbush: return settings.QuestCaravanAmbush;
                case CastleQuest.ArmyOfPoachers: return settings.QuestArmyOfPoachers;
                case CastleQuest.InnAndOut: return settings.QuestInnAndOut;
                case CastleQuest.DeliverHerd: return settings.QuestDeliverHerd;
                case CastleQuest.FamilyFeud: return settings.QuestFamilyFeud;
            }

            return true;
        }

        public static bool Blocked(Hero issueGiver, CastleQuest quest)
        {
            if (issueGiver == null)
            {
                return false;
            }

            Settlement settlement = issueGiver.CurrentSettlement;
            return settlement != null && settlement.IsCastle && CastleMarketTownsBehavior.AppliesTo(settlement) && !Enabled(quest);
        }

        public static bool OverpricedTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (CastleMarketTownsBehavior.AppliesTo(settlement) && Enabled(CastleQuest.OverpricedMaterials)));
        }

        public static bool EscortTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (CastleMarketTownsBehavior.AppliesTo(settlement) && Enabled(CastleQuest.EscortCaravan)));
        }

        public static bool InnAndOutTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (CastleMarketTownsBehavior.TavernAvailable(settlement) && Enabled(CastleQuest.InnAndOut)));
        }

        public static bool HerdTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (CastleMarketTownsBehavior.AppliesTo(settlement) && Enabled(CastleQuest.DeliverHerd)));
        }

        public static bool HerdBlocksCastle(Settlement settlement)
        {
            return settlement != null && settlement.IsCastle && !(CastleMarketTownsBehavior.AppliesTo(settlement) && Enabled(CastleQuest.DeliverHerd));
        }

        public static bool FeudTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (CastleMarketTownsBehavior.AppliesTo(settlement) && Enabled(CastleQuest.FamilyFeud)));
        }

        public static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, string getter, string helper)
        {
            MethodInfo target = AccessTools.Method(typeof(CastleQuestRules), helper);
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == getter && called.DeclaringType == typeof(Settlement))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = target;
                }

                yield return instruction;
            }
        }

        public static bool ScopeTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || CastleMarketTownsBehavior.AppliesTo(settlement));
        }

        public static bool EffectTown(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || (settlement.IsCastle && settlement.Town != null));
        }

        public static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, string helper)
        {
            return Redirect(instructions, "get_IsTown", helper);
        }
    }

    [HarmonyPatch(typeof(ArtisanOverpricedGoodsIssueBehavior), "ConditionsHold")]
    internal static class CastleOverpricedQuestPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "OverpricedTown");
        }
    }

    [HarmonyPatch(typeof(EscortMerchantCaravanIssueBehavior), "ConditionsHold")]
    internal static class CastleEscortQuestPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "EscortTown");
        }
    }

    [HarmonyPatch(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "InitializeCache")]
    internal static class CastleBrigandCachePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "ScopeTown");
        }
    }

    [HarmonyPatch]
    internal static class CastleBrigandEffectPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            System.Type issue = AccessTools.Inner(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "MerchantNeedsHelpWithOutlawsIssue");
            System.Type quest = AccessTools.Inner(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "MerchantNeedsHelpWithOutlawsIssueQuest");
            yield return AccessTools.Method(issue, "AlternativeSolutionEndWithSuccessConsequence");
            yield return AccessTools.Method(issue, "AlternativeSolutionEndWithFailureConsequence");
            yield return AccessTools.Method(quest, "SuccessConsequences");
            yield return AccessTools.Method(quest, "OnTimedOut");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "EffectTown");
        }
    }

    [HarmonyPatch(typeof(SandBox.Issues.RuralNotableInnAndOutIssueBehavior), "ConditionsHold")]
    internal static class CastleInnAndOutQuestPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "get_IsTown", "InnAndOutTown");
        }
    }

    [HarmonyPatch(typeof(SandBox.Issues.FamilyFeudIssueBehavior), "ConditionsHold")]
    internal static class CastleFamilyFeudQuestPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "get_IsTown", "FeudTown");
        }
    }

    [HarmonyPatch(typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "IsVillageSuitableForIssue")]
    internal static class CastleHerdVillagePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "get_IsCastle", "HerdBlocksCastle");
        }
    }

    [HarmonyPatch]
    internal static class CastleHerdTargetPatch
    {
        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        private static List<MethodBase> Targets()
        {
            List<MethodBase> targets = new List<MethodBase>();
            System.Type issue = AccessTools.Inner(typeof(HeadmanNeedsToDeliverAHerdIssueBehavior), "HeadmanNeedsToDeliverAHerdIssue");
            if (issue == null)
            {
                return targets;
            }

            MethodInfo isTown = AccessTools.PropertyGetter(typeof(Settlement), "IsTown");
            foreach (System.Type nested in issue.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (MethodInfo method in nested.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.Name.StartsWith("<.ctor>", System.StringComparison.Ordinal) && Calls(method, isTown))
                    {
                        targets.Add(method);
                    }
                }
            }

            return targets;
        }

        private static bool Calls(MethodInfo method, MethodInfo target)
        {
            try
            {
                foreach (KeyValuePair<OpCode, object> instruction in PatchProcessor.ReadMethodBody(method))
                {
                    if (target.Equals(instruction.Value))
                    {
                        return true;
                    }
                }
            }
            catch (System.Exception)
            {
            }

            return false;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleQuestRules.Redirect(instructions, "get_IsTown", "HerdTown");
        }
    }

    [HarmonyPatch(typeof(ArtisanCantSellProductsAtAFairPriceIssueBehavior), "ConditionsHold")]
    internal static class CastleArtisanCantSellQuestPatch
    {
        static void Postfix(Hero issueGiver, ref bool __result)
        {
            if (__result && CastleQuestRules.Blocked(issueGiver, CastleQuest.ArtisanCantSell))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(CaravanAmbushIssueBehavior), "ConditionsHold")]
    internal static class CastleCaravanAmbushQuestPatch
    {
        static void Postfix(Hero issueGiver, ref bool __result)
        {
            if (__result && CastleQuestRules.Blocked(issueGiver, CastleQuest.CaravanAmbush))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(MerchantArmyOfPoachersIssueBehavior), "ConditionsHold")]
    internal static class CastleArmyOfPoachersQuestPatch
    {
        static void Postfix(Hero issueGiver, ref bool __result)
        {
            if (__result && CastleQuestRules.Blocked(issueGiver, CastleQuest.ArmyOfPoachers))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(DefaultBuildingModel), "CanAddBuildingTypeToTown")]
    internal static class CastleFestivalDailyPatch
    {
        static void Postfix(BuildingType buildingType, Town town, ref bool __result)
        {
            if (town == null || town.Settlement == null || buildingType == null || !town.Settlement.IsCastle)
            {
                return;
            }

            if (buildingType == DefaultBuildingTypes.SettlementMarketplace)
            {
                __result = CastleMarketTownsBehavior.CastleProjectsOverridden(town.Settlement);
                return;
            }

            if (!CastleMarketTownsBehavior.DailyDefaultsOverridden(town.Settlement))
            {
                return;
            }

            if (buildingType == DefaultBuildingTypes.CastleDailySlackenGarrison || buildingType == DefaultBuildingTypes.CastleDailyIrrigation)
            {
                __result = false;
            }

            if (buildingType == DefaultBuildingTypes.SettlementDailyFestivalAndGames || buildingType == DefaultBuildingTypes.SettlementDailyIrrigation)
            {
                __result = true;
            }
        }
    }

    internal static class CastleBuildingEffects
    {
        public static readonly float[] CastellanTax = new float[] { 0.03f, 0.06f, 0.12f };
        public static readonly float[] CastleTariff = new float[] { 0.04f, 0.08f, 0.12f };
        private static readonly FieldInfo EffectsField = AccessTools.Field(typeof(BuildingType), "_effects");
        private static readonly System.Type EffectInfoType = AccessTools.Inner(typeof(BuildingType), "EffectInfo");
        private static System.Array _castellanVariant;
        private static System.Array _marketplaceVariant;

        public static bool IsCastleMarketplace(Building building)
        {
            return building != null
                && building.Town != null
                && building.Town.Settlement != null
                && building.Town.Settlement.IsCastle
                && building.BuildingType == DefaultBuildingTypes.SettlementMarketplace
                && CastleMarketTownsBehavior.CastleProjectsOverridden(building.Town.Settlement)
                && CastleMarketTownsBehavior.OwnsCastleMarketplace(building);
        }

        public static bool HasCastellanTax(Building building)
        {
            return building != null
                && building.Town != null
                && building.BuildingType == DefaultBuildingTypes.CastleCastallansOffice
                && CastleMarketTownsBehavior.CastleProjectsOverridden(building.Town.Settlement);
        }

        public static float LevelValue(float[] values, int level)
        {
            if (level < 1 || level > values.Length)
            {
                return 0f;
            }

            return values[level - 1];
        }

        public static int MarketplaceCostPercent()
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null ? 60 : settings.MarketplaceCostPercent;
        }

        public static object Swap(Building building)
        {
            if (building == null || building.BuildingType == null || EffectsField == null || EffectInfoType == null)
            {
                return null;
            }

            System.Array variant = null;
            if (HasCastellanTax(building))
            {
                variant = CastellanVariant(building.BuildingType);
            }
            else if (IsCastleMarketplace(building))
            {
                variant = MarketplaceVariant(building.BuildingType);
            }

            if (variant == null)
            {
                return null;
            }

            object original = EffectsField.GetValue(building.BuildingType);
            EffectsField.SetValue(building.BuildingType, variant);
            return new object[] { building.BuildingType, original };
        }

        public static void Restore(object state)
        {
            object[] pair = state as object[];
            if (pair == null || EffectsField == null)
            {
                return;
            }

            EffectsField.SetValue(pair[0], pair[1]);
        }

        private static System.Array CastellanVariant(BuildingType type)
        {
            if (_castellanVariant != null)
            {
                return _castellanVariant;
            }

            System.Array original = EffectsField.GetValue(type) as System.Array;
            if (original == null)
            {
                return null;
            }

            bool hasTax = false;
            for (int i = 0; i < original.Length; i++)
            {
                if (EffectOf(original.GetValue(i)) == BuildingEffectEnum.TaxPerDay)
                {
                    hasTax = true;
                }
            }

            System.Array variant = System.Array.CreateInstance(EffectInfoType, original.Length + (hasTax ? 0 : 1));
            System.Array.Copy(original, variant, original.Length);
            if (!hasTax)
            {
                variant.SetValue(MakeEffect(BuildingEffectEnum.TaxPerDay, BuildingEffectIncrementType.AddFactor, CastellanTax), original.Length);
            }

            _castellanVariant = variant;
            return variant;
        }

        private static System.Array MarketplaceVariant(BuildingType type)
        {
            if (_marketplaceVariant != null)
            {
                return _marketplaceVariant;
            }

            System.Array original = EffectsField.GetValue(type) as System.Array;
            if (original == null)
            {
                return null;
            }

            System.Array variant = System.Array.CreateInstance(EffectInfoType, original.Length);
            for (int i = 0; i < original.Length; i++)
            {
                object effect = original.GetValue(i);
                if (EffectOf(effect) == BuildingEffectEnum.TariffIncome)
                {
                    BuildingEffectIncrementType increment = (BuildingEffectIncrementType)EffectInfoType.GetProperty("BuildingEffectIncrementType").GetValue(effect, null);
                    effect = MakeEffect(BuildingEffectEnum.TariffIncome, increment, CastleTariff);
                }

                variant.SetValue(effect, i);
            }

            _marketplaceVariant = variant;
            return variant;
        }

        private static BuildingEffectEnum EffectOf(object effect)
        {
            return (BuildingEffectEnum)EffectInfoType.GetProperty("BuildingEffect").GetValue(effect, null);
        }

        private static object MakeEffect(BuildingEffectEnum effect, BuildingEffectIncrementType increment, float[] values)
        {
            return System.Activator.CreateInstance(EffectInfoType, new object[] { effect, increment, values[0], values[1], values[2] });
        }
    }

    [HarmonyPatch(typeof(Building), "AddEffectOfBuilding")]
    internal static class CastleBuildingEffectPatch
    {
        static bool Prefix(Building __instance, BuildingEffectEnum buildingEffect, ref ExplainedNumber result)
        {
            if (buildingEffect != BuildingEffectEnum.TariffIncome || !CastleBuildingEffects.IsCastleMarketplace(__instance))
            {
                return true;
            }

            int level = __instance.CurrentLevel;
            if (level <= 0)
            {
                return false;
            }

            ExplainedNumber vanilla = Campaign.Current.Models.BuildingEffectModel.GetBuildingEffect(__instance, buildingEffect);
            float baseAmount = __instance.BuildingType.GetBaseBuildingEffectAmount(buildingEffect, level);
            float amount = vanilla.ResultNumber - baseAmount + CastleBuildingEffects.LevelValue(CastleBuildingEffects.CastleTariff, level);
            if (amount != 0f)
            {
                result.AddFactor(amount, __instance.Name);
            }

            return false;
        }

        static void Postfix(Building __instance, BuildingEffectEnum buildingEffect, ref ExplainedNumber result)
        {
            if (buildingEffect != BuildingEffectEnum.TaxPerDay || !CastleBuildingEffects.HasCastellanTax(__instance))
            {
                return;
            }

            float amount = CastleBuildingEffects.LevelValue(CastleBuildingEffects.CastellanTax, __instance.CurrentLevel);
            if (amount > 0f)
            {
                result.AddFactor(amount, __instance.Name);
            }
        }
    }

    [HarmonyPatch(typeof(Building), "GetBonusExplanations")]
    internal static class CastleBuildingTextPatch
    {
        static void Prefix(Building __instance, out object __state)
        {
            __state = CastleBuildingEffects.Swap(__instance);
        }

        static void Finalizer(object __state)
        {
            CastleBuildingEffects.Restore(__state);
        }
    }

    [HarmonyPatch]
    internal static class CastleProjectScreenTextPatch
    {
        private static System.Type ProjectType()
        {
            const string name = "TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement.SettlementProjectVM";
            System.Type type = System.Type.GetType(name + ", TaleWorlds.CampaignSystem.ViewModelCollection", false);
            return type ?? AccessTools.TypeByName(name);
        }

        static bool Prepare()
        {
            return ProjectType() != null;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            System.Type type = ProjectType();
            MethodInfo refresh = AccessTools.Method(type, "RefreshValues");
            MethodInfo level = AccessTools.Method(type, "GetBonusExplanationOfLevel");
            if (refresh != null)
            {
                yield return refresh;
            }

            if (level != null)
            {
                yield return level;
            }
        }

        static void Prefix(object __instance, out object __state)
        {
            Building building = Traverse.Create(__instance).Property("Building").GetValue<Building>();
            __state = CastleBuildingEffects.Swap(building);
        }

        static void Finalizer(object __state)
        {
            CastleBuildingEffects.Restore(__state);
        }
    }

    [HarmonyPatch(typeof(Building), "GetConstructionCost")]
    internal static class CastleMarketplaceCostPatch
    {
        static void Postfix(Building __instance, ref int __result)
        {
            if (__result <= 0 || !CastleBuildingEffects.IsCastleMarketplace(__instance))
            {
                return;
            }

            int scaled = __result * CastleBuildingEffects.MarketplaceCostPercent() / 100;
            __result = scaled < 1 ? 1 : scaled;
        }
    }

    [HarmonyPatch(typeof(MerchantNeedsHelpWithOutlawsIssueQuestBehavior), "ConditionsHold")]
    internal static class CastleBrigandQuestPatch
    {
        static void Postfix(MerchantNeedsHelpWithOutlawsIssueQuestBehavior __instance, Hero issueGiver, ref Hideout hideout, ref bool __result)
        {
            if (issueGiver == null || issueGiver.CurrentSettlement == null || !CastleMarketTownsBehavior.AppliesTo(issueGiver.CurrentSettlement))
            {
                return;
            }

            if (!issueGiver.IsMerchant)
            {
                return;
            }

            if (!CastleQuestRules.Enabled(CastleQuest.Brigands))
            {
                __result = false;
                hideout = null;
                return;
            }

            if (__result)
            {
                return;
            }

            Settlement settlement = issueGiver.CurrentSettlement;
            float limit = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 1.25f;
            foreach (Hideout candidate in Hideout.All)
            {
                if (candidate == null || candidate.Settlement == null || !candidate.IsInfested)
                {
                    continue;
                }

                if (candidate.Settlement.IsSettlementBusy(__instance))
                {
                    continue;
                }

                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(candidate.Settlement, settlement, false, false, MobileParty.NavigationType.Default);
                if (distance < limit)
                {
                    hideout = candidate;
                    __result = true;
                    return;
                }
            }
        }
    }

    internal static class CastleCaravanStops
    {
        private static readonly MBList<Town> _stops = new MBList<Town>();
        private static long _stamp = -1;
        private static Campaign _campaign;

        public static bool Enabled()
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || (settings.Enabled && settings.CaravansVisitCastles);
        }

        public static bool IsStop(Settlement settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            if (settlement.IsTown)
            {
                return true;
            }

            return Enabled()
                && CastleMarketTownsBehavior.AppliesTo(settlement)
                && settlement.Town != null
                && settlement.Town.MarketData != null;
        }

        public static MBReadOnlyList<Town> Stops()
        {
            if (!Enabled() || Campaign.Current == null)
            {
                return Town.AllTowns;
            }

            long hour = (long)CampaignTime.Now.ToHours;
            if (hour != _stamp || _campaign != Campaign.Current)
            {
                _stamp = hour;
                _campaign = Campaign.Current;
                _stops.Clear();
                foreach (Town town in Town.AllTowns)
                {
                    _stops.Add(town);
                }

                foreach (Town castle in Town.AllCastles)
                {
                    if (castle != null && IsStop(castle.Settlement))
                    {
                        _stops.Add(castle);
                    }
                }
            }

            return _stops;
        }

        public static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, string getter, string replacement)
        {
            MethodInfo target = AccessTools.Method(typeof(CastleCaravanStops), replacement);
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called.Name == getter && (called.DeclaringType == typeof(Town) || called.DeclaringType == typeof(Settlement)))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = target;
                }

                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(CaravansCampaignBehavior), "FindNextDestinationForCaravan")]
    internal static class CastleCaravanDestinationPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleCaravanStops.Redirect(instructions, "get_AllTowns", "Stops");
        }
    }

    [HarmonyPatch(typeof(CaravansCampaignBehavior), "HourlyTickParty")]
    internal static class CastleCaravanBuyPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleCaravanStops.Redirect(instructions, "get_IsTown", "IsStop");
        }
    }

    [HarmonyPatch(typeof(CaravansCampaignBehavior), "SpawnCaravan")]
    internal static class CastleCaravanHomePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CastleCaravanStops.Redirect(instructions, "get_IsTown", "IsStop");
        }
    }

    internal static class CastleBrothelGuard
    {
        public static void TryPatch(Harmony harmony)
        {
            MethodInfo entered = AccessTools.Method("CaptivityEvents.Brothel.CEBrothelBehavior:OnSettlementEntered");
            if (entered != null)
            {
                harmony.Patch(entered, prefix: new HarmonyMethod(typeof(CastleBrothelGuard), "SkipCastle"));
            }

            MethodInfo spawn = AccessTools.Method("CaptivityEvents.Brothel.CEBrothelBehavior:LocationCharactersAreReadyToSpawn");
            if (spawn != null)
            {
                harmony.Patch(spawn, prefix: new HarmonyMethod(typeof(CastleBrothelGuard), "SkipCastleSpawn"));
            }
        }

        public static bool SkipCastle(Settlement settlement)
        {
            return !Blocked(settlement);
        }

        public static bool SkipCastleSpawn()
        {
            return !Blocked(Settlement.CurrentSettlement);
        }

        private static bool Blocked(Settlement settlement)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settlement != null && settlement.IsCastle && (settings == null || settings.BlockBrothels);
        }
    }
}
