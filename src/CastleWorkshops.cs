using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // Castle workshops: the player's castle warehouse, selling the player's workshops when a castle stops being covered,
    // and the fallback owner and town list the patches below use.
    internal static class CastleWorkshops
    {
        private static readonly MethodInfo WarehouseGetter = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "GetWarehouseRoster", new[] { typeof(Settlement) });
        private static bool _warehouseErrorLogged;

        internal static void ReportMissingLookups()
        {
            if (WarehouseGetter == null)
            {
                Debug.Print("Castle Market Towns could not find the base game's workshop warehouse; Enter the warehouse is inactive in castles and selling castle workshops leaves their warehouse goods to the base game, likely because a game update changed it.");
            }
        }

        internal static ItemRoster WarehouseOf(Settlement settlement)
        {
            WorkshopsCampaignBehavior behavior = Campaign.Current == null ? null : Campaign.Current.GetCampaignBehavior<WorkshopsCampaignBehavior>();
            if (behavior == null || WarehouseGetter == null || settlement == null)
            {
                return null;
            }

            try
            {
                return WarehouseGetter.Invoke(behavior, new object[] { settlement }) as ItemRoster;
            }
            catch (Exception exception)
            {
                // Menu conditions run every frame the castle menu is open, so report a broken lookup only once.
                if (!_warehouseErrorLogged)
                {
                    _warehouseErrorLogged = true;
                    Debug.Print("Castle Market Towns could not read the workshop warehouse of " + settlement.Name + ": " + exception.Message);
                }

                return null;
            }
        }

        // The base game keeps a warehouse only where the player owns a workshop, so a roster here means the player owns one.
        internal static bool WarehouseCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Warehouse;
            Settlement settlement = Settlement.CurrentSettlement;
            return settlement != null && settlement.IsCastle && WarehouseOf(settlement) != null;
        }

        // Same screen and capacity as the base game's "Enter the warehouse" town option.
        internal static void WarehouseConsequence(MenuCallbackArgs args)
        {
            ItemRoster warehouse = WarehouseOf(Settlement.CurrentSettlement);
            if (warehouse == null)
            {
                return;
            }

            InventoryLogic.CapacityData capacity = new InventoryLogic.CapacityData(
                delegate { return Campaign.Current.Models.WorkshopModel.WarehouseCapacity; },
                delegate { return new TextObject("{=Drj5gIZu}Warehouse Capacity Exceeded"); },
                delegate { return new TextObject("{=Drj5gIZu}Warehouse Capacity Exceeded"); });
            InventoryScreenHelper.OpenScreenAsWarehouse(warehouse, capacity);
        }

        // Runs before the castle's notables move away. Only workshops this mod created are sold; another mod's stay with it.
        // Warehouse goods go to the party if the player is in the castle, to the castle stash if the player's clan owns it,
        // and are otherwise sold for their price.
        internal static void SellPlayerWorkshops(Settlement settlement)
        {
            if (settlement == null || settlement.Town == null || settlement.Town.Workshops == null || Hero.MainHero == null)
            {
                return;
            }

            List<Workshop> owned = new List<Workshop>();
            List<Hero> buyers = new List<Hero>();
            int kept = 0;
            foreach (Workshop workshop in settlement.Town.Workshops)
            {
                if (workshop == null || workshop.Owner != Hero.MainHero)
                {
                    continue;
                }

                if (!IsModWorkshop(workshop))
                {
                    kept++;
                    continue;
                }

                Hero buyer = BuyerFor(workshop);
                if (buyer == null)
                {
                    Debug.Print("Castle Market Towns found no notable to buy your " + workshop.Name + " in " + settlement.Name + "; it stays yours.");
                    kept++;
                    continue;
                }

                owned.Add(workshop);
                buyers.Add(buyer);
            }

            if (owned.Count == 0)
            {
                return;
            }

            // The base game keeps one warehouse per settlement for all of the player's workshops there.
            int goodsGold = kept == 0 ? EmptyWarehouse(settlement) : 0;
            int workshopGold = 0;
            for (int i = 0; i < owned.Count; i++)
            {
                workshopGold += Campaign.Current.Models.WorkshopModel.GetCostForNotable(owned[i]);
                ChangeOwnerOfWorkshopAction.ApplyByPlayerSelling(owned[i], buyers[i], owned[i].WorkshopType);
            }

            if (goodsGold > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goodsGold);
            }

            TextObject message = new TextObject("{=cmt_notice_workshops_sold}{SETTLEMENT} no longer has a castle market, so your {COUNT} {?(COUNT > 1)}workshops there were{?}workshop there was{\\?} sold for {GOLD}{GOLD_ICON}.");
            message.SetTextVariable("SETTLEMENT", settlement.Name);
            message.SetTextVariable("COUNT", owned.Count);
            message.SetTextVariable("GOLD", workshopGold + goodsGold);
            message.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            MBInformationManager.AddQuickInformation(message);
            Debug.Print("Castle Market Towns sold " + owned.Count + " of your workshops in " + settlement.Name + " for " + workshopGold + " gold, plus " + goodsGold + " gold for warehouse goods.");
        }

        private static bool IsModWorkshop(Workshop workshop)
        {
            return !string.IsNullOrEmpty(workshop.Tag) && workshop.Tag.StartsWith(CastleMarketTownsBehavior.WorkshopTagPrefix, StringComparison.Ordinal);
        }

        public static MBReadOnlyList<Town> TownsAndWorkshopCastles()
        {
            MBList<Town> towns = new MBList<Town>();
            foreach (Town town in Town.AllTowns)
            {
                towns.Add(town);
            }

            foreach (Town castle in Town.AllCastles)
            {
                if (castle != null && castle.Workshops != null && castle.Workshops.Length > 0)
                {
                    towns.Add(castle);
                }
            }

            return towns;
        }

        private static Hero BuyerFor(Workshop workshop)
        {
            Hero buyer = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
            return buyer != null ? buyer : FallbackOwner(workshop);
        }

        // The base game only hands a workshop to another notable of the same settlement and fails with no one to take it,
        // which a castle with a single notable reaches on bankruptcy, death or sale.
        internal static Hero FallbackOwner(Workshop workshop)
        {
            if (workshop == null || workshop.Settlement == null)
            {
                return null;
            }

            Hero owner = workshop.Owner;

            // The base game picks by power, so notables with no power at all are never chosen; any living notable will do here.
            Hero notable = AnyNotable(workshop.Settlement, owner);
            if (notable != null)
            {
                return notable;
            }

            // A bankrupt notable with nobody to sell to keeps the workshop with fresh capital, as a town notable's buyer would get.
            if (owner != null && owner.IsAlive && owner.IsNotable && owner != Hero.MainHero)
            {
                return owner;
            }

            return AnyNotable(CastleMarketTownsBehavior.NearestTown(workshop.Settlement), owner);
        }

        private static Hero AnyNotable(Settlement settlement, Hero owner)
        {
            if (settlement == null)
            {
                return null;
            }

            Hero fallback = null;
            foreach (Hero notable in settlement.Notables)
            {
                if (notable == null || !notable.IsAlive || notable == owner)
                {
                    continue;
                }

                if (notable.IsMerchant || notable.IsArtisan)
                {
                    return notable;
                }

                if (fallback == null)
                {
                    fallback = notable;
                }
            }

            return fallback;
        }

        private static int EmptyWarehouse(Settlement settlement)
        {
            ItemRoster warehouse = WarehouseOf(settlement);
            if (warehouse == null || warehouse.Count == 0)
            {
                return 0;
            }

            ItemRoster target = null;
            if (Settlement.CurrentSettlement == settlement && MobileParty.MainParty != null)
            {
                target = MobileParty.MainParty.ItemRoster;
            }
            else if (settlement.OwnerClan == Clan.PlayerClan && settlement.Stash != null)
            {
                target = settlement.Stash;
            }

            int gold = 0;
            for (int i = 0; i < warehouse.Count; i++)
            {
                ItemRosterElement element = warehouse.GetElementCopyAtIndex(i);
                if (element.EquipmentElement.Item == null || element.Amount <= 0)
                {
                    continue;
                }

                if (target != null)
                {
                    target.AddToCounts(element.EquipmentElement, element.Amount);
                }
                else
                {
                    gold += settlement.Town.GetItemPrice(element.EquipmentElement, null, true) * element.Amount;
                }
            }

            warehouse.Clear();
            return gold;
        }
    }

    // The clan screen's Other tab lists workshops of towns only; castle workshops were counted but never shown or managed.
    [HarmonyPatch]
    internal static class CastleClanWorkshopListPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(ClanIncomeVM), "RefreshList", Type.EmptyTypes));
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
            return CastleTownRules.Redirect(instructions, typeof(Settlement), "get_IsTown", "ListsWorkshopsAsTown");
        }
    }

    // A hero's tooltip lists the workshops they own in towns. Only that check changes; the alley check right after it stays.
    [HarmonyPatch]
    internal static class CastleHeroTooltipWorkshopsPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(TooltipRefresherCollection), "RefreshHeroTooltip"));
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo isTown = AccessTools.PropertyGetter(typeof(Settlement), "IsTown");
            FieldInfo town = AccessTools.Field(typeof(Settlement), "Town");
            int match = -1;
            int matches = 0;
            for (int i = 0; isTown != null && town != null && i + 3 < codes.Count; i++)
            {
                // "if (settlement.IsTown) { Town town = settlement.Town; ..." is the workshop block.
                if (codes[i].Calls(isTown) && codes[i + 1].opcode.FlowControl == FlowControl.Cond_Branch && codes[i + 2].IsLdloc() && codes[i + 3].LoadsField(town))
                {
                    match = i;
                    matches++;
                }
            }

            if (matches == 1)
            {
                codes[match].opcode = OpCodes.Call;
                codes[match].operand = AccessTools.Method(typeof(CastleTownRules), "ListsWorkshopsAsTown");
            }

            return codes;
        }
    }

    // The base game's limit, clan tier + 1, becomes the town limit plus Extra town workshop limit.
    // Castle workshops count against their own limit instead; see CastleWorkshopLimits.
    [HarmonyPatch(typeof(DefaultWorkshopModel), "GetMaxWorkshopCountForClanTier")]
    internal static class CastleWorkshopLimitPatch
    {
        static void Postfix(ref int __result)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings != null && settings.ExtraWorkshopLimit > 0)
            {
                __result += settings.ExtraWorkshopLimit;
            }
        }
    }

    internal static class CastleWorkshopLimits
    {
        public static int Limit(bool castle)
        {
            int limit = Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(Clan.PlayerClan.Tier);
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (!castle || settings == null)
            {
                return limit;
            }

            return limit - Math.Max(0, settings.ExtraWorkshopLimit) + Math.Max(0, settings.CastleWorkshopLimit);
        }

        public static int Owned(bool castle)
        {
            int count = 0;
            if (Hero.MainHero == null || Hero.MainHero.OwnedWorkshops == null)
            {
                return count;
            }

            foreach (Workshop workshop in Hero.MainHero.OwnedWorkshops)
            {
                if (workshop != null && workshop.Settlement != null && workshop.Settlement.IsCastle == castle)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool Full(bool castle)
        {
            return Limit(castle) <= Owned(castle);
        }
    }

    // Buying a workshop from a notable: the base game checks every owned workshop against one limit.
    [HarmonyPatch]
    internal static class CastleWorkshopBuyLimitPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(WorkshopsCharactersCampaignBehavior), "can_player_buy_workshop_clickable_condition"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static void Postfix(Workshop workshop, ref bool __result, ref TextObject explanation)
        {
            if (workshop == null || workshop.Settlement == null || Hero.MainHero == null || Clan.PlayerClan == null || Hero.MainHero.OwnedWorkshops == null)
            {
                return;
            }

            bool castle = workshop.Settlement.IsCastle;
            if (CastleWorkshopLimits.Full(castle))
            {
                __result = false;
                explanation = castle
                    ? new TextObject("{=cmt_workshop_castle_limit_reached}You have reached the maximum number of castle workshops you can have.")
                    : new TextObject("{=cmt_workshop_town_limit_reached}You have reached the maximum number of town workshops you can have.");
                return;
            }

            WorkshopModel model = Campaign.Current.Models.WorkshopModel;
            if (model.GetMaxWorkshopCountForClanTier(Clan.PlayerClan.Tier) > Hero.MainHero.OwnedWorkshops.Count)
            {
                return;
            }

            // Only the shared limit blocked this purchase; the gold check behind it still applies.
            if (Hero.MainHero.Gold < model.GetCostForPlayer(workshop))
            {
                __result = false;
                explanation = new TextObject("{=B2jpmFh6}You don't have enough money to buy this workshop.");
                return;
            }

            __result = true;
            explanation = null;
        }
    }

    // The clan screen shows owned workshops over the one limit; it shows the town and castle limits instead.
    [HarmonyPatch]
    internal static class CastleClanWorkshopCountPatch
    {
        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(AccessTools.Method(typeof(ClanIncomeVM), "RefreshList", Type.EmptyTypes));
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static void Postfix(ClanIncomeVM __instance)
        {
            if (Hero.MainHero == null || Clan.PlayerClan == null)
            {
                return;
            }

            TextObject text = new TextObject("{=cmt_clan_workshop_counts}{WORKSHOPS} (towns {TOWN_OWNED}/{TOWN_LIMIT}, castles {CASTLE_OWNED}/{CASTLE_LIMIT})");
            text.SetTextVariable("WORKSHOPS", GameTexts.FindText("str_clan_workshops"));
            text.SetTextVariable("TOWN_OWNED", CastleWorkshopLimits.Owned(false));
            text.SetTextVariable("TOWN_LIMIT", CastleWorkshopLimits.Limit(false));
            text.SetTextVariable("CASTLE_OWNED", CastleWorkshopLimits.Owned(true));
            text.SetTextVariable("CASTLE_LIMIT", CastleWorkshopLimits.Limit(true));
            __instance.WorkshopText = text.ToString();
        }
    }

    [HarmonyPatch(typeof(DefaultWorkshopModel), "GetNotableOwnerForWorkshop")]
    internal static class CastleWorkshopNewOwnerPatch
    {
        static void Postfix(Workshop workshop, ref Hero __result)
        {
            if (__result == null && workshop != null && workshop.Settlement != null && workshop.Settlement.IsCastle)
            {
                __result = CastleWorkshops.FallbackOwner(workshop);
            }
        }
    }

    // Two base game workshop incidents look for the player's workshops in towns only.
    [HarmonyPatch]
    internal static class CastleWorkshopIncidentPatch
    {
        static List<MethodBase> Targets()
        {
            List<MethodBase> found = new List<MethodBase>();
            Type behavior = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.IncidentsCampaignBehaviour");
            if (behavior == null)
            {
                return found;
            }

            List<Type> types = new List<Type> { behavior };
            types.AddRange(behavior.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic));
            foreach (Type type in types)
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.Name.Contains("GetHandInTheStrongboxWorkshop") || method.Name.Contains("GetSecessionOfThePlebsWorkshop"))
                    {
                        found.Add(method);
                    }
                }
            }

            return found;
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
            MethodInfo allTowns = AccessTools.PropertyGetter(typeof(Town), "AllTowns");
            MethodInfo withCastles = AccessTools.Method(typeof(CastleWorkshops), "TownsAndWorkshopCastles");
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(allTowns))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = withCastles;
                }

                yield return instruction;
            }
        }
    }

    // The base game sizes its per-workshop and warehouse arrays to the limit only when a save loads. A limit raised mid-game
    // would leave a new workshop without warehouse data, so grow the arrays before a workshop or warehouse is added.
    [HarmonyPatch]
    internal static class CastleWorkshopDataSizePatch
    {
        private static MethodInfo _ensureSize;

        static List<MethodBase> Targets()
        {
            return SettlementGetter.Existing(
                AccessTools.Method(typeof(WorkshopsCampaignBehavior), "AddNewWorkshopData"),
                AccessTools.Method(typeof(WorkshopsCampaignBehavior), "AddNewWarehouseDataIfNeeded"));
        }

        static bool Prepare()
        {
            _ensureSize = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "EnsureBehaviorDataSize", Type.EmptyTypes);
            return _ensureSize != null && Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static void Prefix(WorkshopsCampaignBehavior __instance)
        {
            try
            {
                _ensureSize.Invoke(__instance, null);
            }
            catch (Exception exception)
            {
                Exception cause = exception.InnerException ?? exception;
                Debug.Print("Castle Market Towns could not make room for another workshop: " + cause.Message);
            }
        }
    }
}
