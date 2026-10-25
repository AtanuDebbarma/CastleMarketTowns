using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    public sealed class CastleMarketTownsSettings : AttributeGlobalSettings<CastleMarketTownsSettings>
    {
        internal const int MaxCompanionStayDays = 30;
        internal const int MaxCastleWorkshopSlots = 8;
        internal const int BaseTownWorkshopSlots = 4;
        internal const int MaxTownWorkshopSlots = 10;

        // Translation IDs are public: translation modules depend on them, so keep each ID once released.
        private const string GroupGeneral = "{=cmt_mcm_group_general}General";
        private const string GroupCastleCompanions = GroupGeneral + "/{=cmt_mcm_group_castle_companions}Castle companions";
        private const string GroupPeopleAndWorkshops = "{=cmt_mcm_group_people_and_workshops}People and workshops";
        private const string GroupScale = "{=cmt_mcm_group_scale}Scale";
        private const string GroupCastleMarket = "{=cmt_mcm_group_castle_market}Castle market";
        private const string GroupCastleIncome = "{=cmt_mcm_group_castle_income}Castle income";
        private const string GroupCastleServices = "{=cmt_mcm_group_castle_services}Castle services";
        private const string GroupCastleRecruits = "{=cmt_mcm_group_castle_recruits}Castle recruits";
        private const string GroupCastleQuests = "{=cmt_mcm_group_castle_quests}Castle quests";

        public override string Id { get { return "CastleMarketTowns"; } }
        public override string DisplayName { get { return new TextObject("{=cmt_mcm_title}Castle Market Towns").ToString(); } }
        public override string FolderName { get { return "CastleMarketTowns"; } }
        public override string FormatType { get { return "json2"; } }

        public CastleMarketTownsSettings()
        {
            Enabled = true;
            EnableTavern = true;
            BlockBrothels = true;
            AllowRebellion = true;
            AllowNotableQuests = true;
            QuestEscortCaravan = true;
            QuestBrigands = true;
            QuestOverpricedMaterials = true;
            QuestArtisanCantSell = true;
            QuestCaravanAmbush = true;
            QuestArmyOfPoachers = true;
            QuestInnAndOut = true;
            QuestDeliverHerd = true;
            QuestFamilyFeud = true;
            ReplaceSlackenWithFestival = true;
            OverrideCastleProjects = true;
            CaravansVisitCastles = true;
            HotButterTavernOptions = true;
            ImprovedGarrisonsSupport = true;
            CastleCompanions = true;
            CompanionArrivalPercent = 100;
            CompanionDeparturePercent = 10;
            CompanionMinimumStayDays = 7;
            ApplyToKingdom = true;
            ApplyToNpc = true;
            MerchantNotables = 2;
            ArtisanNotables = 2;
            WorkshopCount = 2;
            TownWorkshopCount = BaseTownWorkshopSlots;
            ExtraWorkshopLimit = 0;
            CastleWorkshopLimit = 0;
            ProsperityFloor = 1000;
            MarketPercent = 25;
            CastleProductionPercent = 20;
            CastleConsumptionPercent = 50;
            CastleStockCeilingPercent = 60;
            ExportCastleSurplus = true;
            CastleStockDays = 10;
            CaravansFavourLowStock = true;
            CaravanBonusPercent = 25;
            CastleRecruits = true;
            NobleRecruitPercent = 50;
            RecruitPercent = 65;
            MerchantGold = 12000;
            MarketplaceCostPercent = 60;
            SiegeFoodFromMarket = true;
            LordsBuyCastleFood = true;
            CastleTaxPercent = 80;
            TradeProsperityPercent = 50;
            CastleHealing = true;
            CastleRepair = true;
            CastleSmithy = true;
        }

        [SettingPropertyBool("{=cmt_mcm_enabled_name}Enable castle markets", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_enabled_hint}Turns every castle into a small trading settlement. The castle stays a castle: same walls and the same noble recruits from its villages. Rebellion is a separate switch.")]
        [SettingPropertyGroup(GroupGeneral, GroupOrder = 0)]
        public bool Enabled { get; set; }

        [SettingPropertyBool("{=cmt_mcm_enable_tavern_name}Add a tavern", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_enable_tavern_hint}Adds a tavern room to the castle and a menu option to visit it. Tavern staff use the town tavern scenes.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool EnableTavern { get; set; }

        [SettingPropertyBool("{=cmt_mcm_block_brothels_name}Keep brothels out", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_block_brothels_hint}Independent of Enable castle markets and on by default. Captivity Events adds its brothel to the town menu and to a location named brothel. Castles keep the castle menu, and any location named brothel is removed from every castle. Turn this off to let another mod keep a brothel in castles.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool BlockBrothels { get; set; }

        [SettingPropertyBool("{=cmt_mcm_allow_rebellion_name}Castles can rebel", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_allow_rebellion_hint}On by default. A castle uses the same loyalty rebellion check as a town. Turn this off and castles go back to never rebelling.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool AllowRebellion { get; set; }

        [SettingPropertyBool("{=cmt_mcm_allow_notable_quests_name}Castle notable quests", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_allow_notable_quests_hint}On by default. Master switch for the quests castle merchants and artisans can offer, and for the village quests that point to a castle. Pick single quests in the Castle quests group. Each quest still needs the same situation as in a town. Turn this off and castles offer none of them.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool AllowNotableQuests { get; set; }

        [SettingPropertyBool("{=cmt_mcm_replace_slacken_with_festival_name}Override castle daily defaults", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_replace_slacken_with_festival_hint}Daily defaults only. Castle projects have their own switch. On by default. Slacken Garrison is replaced by Festival and Games, which adds 3 loyalty each day. Irrigation adds 1 bound-village hearth each day instead of the vanilla 0.5. Turn this off and both daily defaults go back to vanilla.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ReplaceSlackenWithFestival { get; set; }

        [SettingPropertyBool("{=cmt_mcm_override_castle_projects_name}Override castle projects", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_override_castle_projects_hint}Construction projects only. Daily defaults have their own switch. On by default. Castellan's Office keeps the vanilla wage cut and auto-recruit, and also adds tax of 3%, 6%, and 12%. Castles gain a Marketplace with tariffs of 4%, 8%, and 12%, and caravan access of 1.02, 1.04, and 1.06. Turn this off and castles go back to vanilla: no extra tax, and no marketplace.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool OverrideCastleProjects { get; set; }

        [SettingPropertyBool("{=cmt_mcm_hot_butter_tavern_options_name}Hot Butter in castle taverns", Order = 10, RequireRestart = false, HintText = "{=cmt_mcm_hot_butter_tavern_options_hint}Only matters if Hot Butter is installed. On by default. Castle taverns get Hot Butter's Sell your body, fine prostitute, cheap prostitute and troop prostitutes options, with the same rules as in a town. Renting a room already exists in every castle through Hot Butter's own Stay at the castle with your partner option. The Captivity Events brothel stays out of castles. Without Hot Butter this does nothing.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool HotButterTavernOptions { get; set; }

        [SettingPropertyBool("{=cmt_mcm_improved_garrisons_support_name}Improved Garrisons support", Order = 11, RequireRestart = false, HintText = "{=cmt_mcm_improved_garrisons_support_hint}Only matters if Improved Garrisons is installed. On by default. In castles the mod covers, its menu button moves into Go to the castle, as it sits in Go to the keep in towns; its project tooltips show the castle Marketplace tariff and the Castellan's Office tax; and its recruiter, patrol and mobile garrison parties treat those castles like towns, so they recruit from castle merchants and artisans, sell loot and buy horses in the castle market. Improved Garrisons' own files are never changed. Turn this off and those three changes stop. Its garrisons still recruit from the castle's own merchants and artisans, as in towns. Without Improved Garrisons this does nothing.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ImprovedGarrisonsSupport { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_companions_name}Companions in castle taverns", IsToggle = true, Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_companions_hint}On by default. On the first eligible day, creates a small, bounded group of companions in different castle taverns using the active companion system's unused templates and creation rules: the base game's, or a replacement such as Women in Calradia's. A save marker prevents reseeding. If no compatible companion system is found, the seed is skipped once and castle taverns fill only as companions move from towns. Afterward, unhired companions move between towns and nearby castles; the settings in this group control how often. Supports War Sails without requiring it. Turn this off and tracked unhired castle companions move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists, once they are free to move: not giving a quest, imprisoned, in a party or in the castle you are in. Turning it back on does not seed again. If another mod overrides companion behavior and causes problems in castles, leave this off. Needs Add a tavern.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public bool CastleCompanions { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_arrival_percent_name}Arrival chance percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_companion_arrival_percent_hint}Each day, the chance that one unhired companion in a town moves to the tavern of one of the 3 castles closest to that town that has no unhired companion yet. Default 100. Arrivals and the one-time seed stop once castles hold a third of the free companions; if hiring later shrinks the free pool, castles can briefly hold more until companions leave. 0 stops new arrivals.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionArrivalPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_departure_percent_name}Departure chance percent", 0, 100, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_companion_departure_percent_hint}Each day, the chance that one companion this mod moved into a castle goes back to one of the 3 towns closest to that castle, preferring a town without an unhired companion. Default 10. 0 keeps them in castles until they are hired, the base game removes them, or the castle stops hosting companions because this group, the tavern or the castle's coverage is switched off.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionDeparturePercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_minimum_stay_days_name}Minimum stay days", 1, MaxCompanionStayDays, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_companion_minimum_stay_days_hint}After this mod moves a companion, it does not move them again for this many days, into a castle or back to a town. Default 7. When a castle stops hosting companions because this group, the tavern or the castle's coverage is switched off, they move out regardless of this. The base game's own town-to-town companion swaps are not affected.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionMinimumStayDays { get; set; }

        [SettingPropertyBool("{=cmt_mcm_caravans_visit_castles_name}Caravans trade at castles", Order = 9, RequireRestart = false, HintText = "{=cmt_mcm_caravans_visit_castles_hint}On by default. Every caravan, including yours, NPC caravans, and caravans formed in a castle, can pick a castle as a trade stop, then buy and sell there like in a town; visits count toward trade agreements. Only castles the mod is active on count, so the two clan switches decide which castles caravans visit. A caravan formed in a castle starts from that castle. Turn this off and caravans only trade in towns, as in vanilla.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool CaravansVisitCastles { get; set; }

        [SettingPropertyBool("{=cmt_mcm_apply_to_kingdom_name}Apply to your kingdom's clans", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_apply_to_kingdom_hint}On by default. Covers castles owned by other clans in the kingdom your clan belongs to, and lets lords of those clans recruit at any castle the mod covers, as they do in towns and villages. Your own clan's castles are always changed. Turn this off and those castles go back to vanilla: workshops you own there are sold at the base game's price to a castle notable (or one from the nearest town if none is left), the tavern, the mod workshops and the castle Marketplace are removed, the prosperity and goods the mod added are taken back, and the merchants and artisans the mod created move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Castles outside your kingdom use the next switch.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ApplyToKingdom { get; set; }

        [SettingPropertyBool("{=cmt_mcm_apply_to_npc_name}Apply to other kingdoms and clans", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_apply_to_npc_hint}On by default. Covers castles owned by any clan outside your kingdom: other kingdoms and independent clans. If your clan has no kingdom, this covers every castle you do not own. It also lets lords and caravans of those clans recruit at any castle the mod covers, as they do in towns and villages. Turn it off and they cannot recruit at castles. Turn this off and those castles go back to vanilla: workshops you own there are sold at the base game's price to a castle notable (or one from the nearest town if none is left), the tavern, the mod workshops and the castle Marketplace are removed, the prosperity and goods the mod added are taken back, and the merchants and artisans the mod created move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ApplyToNpc { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_merchant_notables_name}Merchants", 0, 4, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_merchant_notables_hint}A town keeps 2 merchants. They are the notables who can send caravans. Default 2. Every notable also gives up to 6 recruits. Lowering it moves extra merchants the mod created to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Merchants from the base game or other mods stay.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops, GroupOrder = 1)]
        public int MerchantNotables { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_artisan_notables_name}Artisans", 0, 4, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_artisan_notables_hint}A town keeps 1 artisan. Artisans own workshops. Default 2, so a castle has 4 notables, between a village's 3 and a town's 5. Every notable also gives up to 6 recruits. Lowering it moves extra artisans the mod created to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Artisans from the base game or other mods stay.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int ArtisanNotables { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_workshop_slots_name}Castle workshop slots", 0, MaxCastleWorkshopSlots, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_workshop_slots_hint}How many workshops each castle has. Default 2. Towns have their own slider. Change this at any time. Castles gain or lose slots to match, from 0 to 8. Only slots the mod added are removed, and never one owned by you or a lord.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int WorkshopCount { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_town_workshop_slots_name}Town workshop slots", BaseTownWorkshopSlots, MaxTownWorkshopSlots, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_town_workshop_slots_hint}How many workshops each town has. Default 4, as in the base game, where one of the 4 is the town artisans' own shop that cannot be bought. Each slot above 4 adds a workshop that the town's notables run and you can buy and manage like any other, but it has no building or sign in the town scene. Change this at any time. Towns gain or lose added slots to match, from 4 to 10. The base game's 4 are never removed, and an added one owned by you or a lord is kept.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int TownWorkshopCount { get; set; }

        // Keeps its 1.3.0 property name so a saved value carries over; it now raises the town limit only.
        [SettingPropertyInteger("{=cmt_mcm_town_workshop_limit_name}Extra town workshop limit", 0, 10, "0", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_town_workshop_limit_hint}Raises how many workshops you may own in towns. The base game allows your clan tier plus 1, so 1 at tier 0 up to 7 at tier 6. Default 0 keeps that limit. Only town workshops count toward it; castle workshops have their own limit below. Lowering it never takes a workshop away; you just cannot buy another town workshop until you are under the limit again.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int ExtraWorkshopLimit { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_workshop_limit_name}Extra castle workshop limit", 0, 10, "0", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_castle_workshop_limit_hint}Raises how many workshops you may own in castles. Castles have their own limit, separate from towns, of your clan tier plus 1, so 1 at tier 0 up to 7 at tier 6. Default 0 keeps that limit. Only castle workshops count toward it; town workshops have their own limit above. Lowering it never takes a workshop away; you just cannot buy another castle workshop until you are under the limit again.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int CastleWorkshopLimit { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_merchant_gold_name}Merchant purse", 0, 50000, "0", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_merchant_gold_hint}Gold given to a merchant this mod creates, if they have less. Vanilla caravans need a large purse before a notable will form one.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int MerchantGold { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_prosperity_floor_name}Prosperity floor", 0, 4000, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_prosperity_floor_hint}Applied once per castle. If prosperity is below this, it is raised to it. Towns in the base game sit around 1700 to 5600, castles around 420 to 1100. Default 1000, the top of the castle range, so a new castle market starts modest and grows through trade. Prosperity also drives castle tax, food use and militia. 0 leaves prosperity alone.")]
        [SettingPropertyGroup(GroupScale, GroupOrder = 2)]
        public int ProsperityFloor { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_market_percent_name}Market stock percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_market_percent_hint}Applied once per castle. The castle market starts with this share of the trade goods held by the nearest reachable town of its culture, or the nearest reachable town if none exists. 100 matches that town. Default 25, a quarter, so a new castle never rivals a town. Later stock comes from workshops, caravans and normal trade. If the mod stops covering a castle, the goods it added are taken back.")]
        [SettingPropertyGroup(GroupScale)]
        public int MarketPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_production_percent_name}Castle production percent", 0, 100, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_production_percent_hint}Each day the castle market gains this share of what its own villages produce, using the base game village production. The villages still produce their full amount and send it to their town, so towns lose nothing. Scaled by castle prosperity, from half at 800 to one and a half at 2400. Default 20: a castle at 1600 prosperity that consumes half as much is about a fifth of an average town. 0 turns production off.")]
        [SettingPropertyGroup(GroupCastleMarket, GroupOrder = 5)]
        public int CastleProductionPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_consumption_percent_name}Castle consumption percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_castle_consumption_percent_hint}How much a castle's people use up each day, compared with a town of the same prosperity. Villages use nothing from a market and towns use 100. Default 50 sits between them. Prices still follow supply and demand as in a town.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleConsumptionPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_stock_ceiling_percent_name}Castle stock ceiling percent", 10, 100, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_stock_ceiling_percent_hint}Safety cap for extreme settings. Castle production stops once the castle holds this share of its town's stock of that good, unless that is below the castle's own reserve. Default 60, so a castle stays below its town unless that town is in trouble. Goods brought by caravans or traders are not capped.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleStockCeilingPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_export_castle_surplus_name}Sell castle surplus to its town", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_export_castle_surplus_hint}On by default. Like villagers, the castle sells goods it produces beyond its reserve to the town its villages trade with, and the castle market keeps the gold. Towns gain stock instead of losing it. Only goods the castle's villages make are sold, and only as much as the town can pay for.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool ExportCastleSurplus { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_stock_days_name}Castle reserve days", 1, 30, "0", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_castle_stock_days_hint}How many days of its own production or consumption, whichever is larger, the castle keeps of each good it produces before selling the rest to its town. Default 10.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleStockDays { get; set; }

        [SettingPropertyBool("{=cmt_mcm_caravans_favour_low_stock_name}Caravans favour low-stock castles", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_caravans_favour_low_stock_hint}On by default. When a castle holds less than 35 percent of its town's trade goods, caravans rate it higher as a trade stop. The emptier the castle, the larger the boost. Well-stocked castles get no boost, so caravans keep favouring towns. Needs Caravans trade at castles.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool CaravansFavourLowStock { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_caravan_bonus_percent_name}Caravan boost percent", 0, 100, "0", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_caravan_bonus_percent_hint}Largest boost to a caravan's rating of a nearly empty castle. Default 25. It shrinks to 0 as the castle reaches 35 percent of its town's trade goods.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CaravanBonusPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_siege_food_from_market_name}Siege food from the castle market", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_siege_food_from_market_hint}On by default. Like a town, a besieged castle that runs out of food takes food from its own market. Usually adds a few days to a siege. Turn this off and the market food is not used during sieges, as in a vanilla castle.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool SiegeFoodFromMarket { get; set; }

        [SettingPropertyBool("{=cmt_mcm_lords_buy_castle_food_name}Lords buy food at castles", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_lords_buy_castle_food_hint}On by default. Lords and their armies can buy food in a castle market, as in a town, but only food above the castle's reserve, so the castle keeps its siege food. Lords short on food also travel to castles that have some to spare. Turn this off and lords only buy food in towns and villages.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool LordsBuyCastleFood { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_tax_percent_name}Castle tax percent", 10, 150, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_tax_percent_hint}Prosperity tax of a castle, compared with a town of the same prosperity. Default 80. With the 1000 prosperity floor a new castle pays about 280 a day, above the vanilla castle average of about 245, and more as trade raises prosperity. Town tax policies, the Khuzait tax penalty, Road Tolls, State Monopolies and the Tollgates perk also apply to castles.")]
        [SettingPropertyGroup(GroupCastleIncome, GroupOrder = 6)]
        public int CastleTaxPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_trade_prosperity_percent_name}Trade prosperity percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_trade_prosperity_percent_hint}In towns, goods sold in the market raise prosperity. Castles get this share of that effect. Default 50. Prosperity in turn raises tax. 0 keeps it town-only, as in vanilla.")]
        [SettingPropertyGroup(GroupCastleIncome)]
        public int TradeProsperityPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_healing_name}Town healing in castles", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_healing_hint}On by default. Needs the castle tavern. Pristine Streets, A Good Day's Rest and Good Lodgings work while resting in a castle, and the governor's Medicine skill speeds up garrison healing, as in a town. Castles already give the base game's walled-settlement healing.")]
        [SettingPropertyGroup(GroupCastleServices, GroupOrder = 7)]
        public bool CastleHealing { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_repair_name}Castles repair damage", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_castle_repair_hint}On by default. Like towns and villages, a castle slowly restores its settlement health after raids or sieges, with Clean Infrastructure and In Best Light working as in a town.")]
        [SettingPropertyGroup(GroupCastleServices)]
        public bool CastleRepair { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_smithy_name}Castle smithy", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_smithy_hint}On by default. Adds Enter smithy to the castle menu, the same smithy screen as in towns. Smith, smelt and refine with your own materials, and smithing stamina recovers in castles as in towns. Smithing orders stay in towns, so the castle smithy shows none. Turn this off and the option is hidden.")]
        [SettingPropertyGroup(GroupCastleServices)]
        public bool CastleSmithy { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_recruits_name}Recruit troops in castles", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_recruits_hint}On by default. Adds Recruit troops to the castle menu, the same screen as in towns and villages. The castle's merchants and artisans offer volunteers of the castle's culture, which grow in tier over time like any notable's. Lords recruit there too and travel to castles for recruits, as in towns and villages; the two Apply to switches decide which clans may. Turn this off and castle notables stop producing volunteers.")]
        [SettingPropertyGroup(GroupCastleRecruits, GroupOrder = 4)]
        public bool CastleRecruits { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_noble_recruit_percent_name}Noble recruit chance", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_noble_recruit_percent_hint}Chance that a new castle volunteer starts on the culture's noble troop line instead of the basic one. Towns are 0 and villages owned by a castle are 100. Default 50 sits between them. Only new volunteers are affected.")]
        [SettingPropertyGroup(GroupCastleRecruits)]
        public int NobleRecruitPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_recruit_percent_name}Recruit speed percent", 10, 100, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_recruit_percent_hint}How fast the castle's merchants and artisans produce volunteers, compared with a town notable. Village recruits are unchanged, including the noble troops from castle villages.")]
        [SettingPropertyGroup(GroupCastleRecruits)]
        public int RecruitPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_marketplace_cost_percent_name}Castle Marketplace cost percent", 25, 100, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_marketplace_cost_percent_hint}Construction cost of the castle Marketplace, compared with a town Marketplace. Default 60. Only changes construction that has not been done yet.")]
        [SettingPropertyGroup(GroupScale)]
        public int MarketplaceCostPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_escort_caravan_name}Escort Merchant Caravan", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_quest_escort_caravan_hint}Merchant quest. On by default. Needs low security at the castle, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests, GroupOrder = 3)]
        public bool QuestEscortCaravan { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_brigands_name}Needs Help With Brigands", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_quest_brigands_hint}Merchant quest. On by default. Needs a bandit hideout near the castle, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestBrigands { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_overpriced_materials_name}Overpriced Raw Materials", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_quest_overpriced_materials_hint}Artisan quest. On by default. Needs a raw material to be expensive in the castle market, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestOverpricedMaterials { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_artisan_cant_sell_name}Artisan Can't Sell Products", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_quest_artisan_cant_sell_hint}Artisan quest from the base game. On by default. Needs a merchant in the same castle and a town nearby to deliver to, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestArtisanCantSell { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_caravan_ambush_name}Caravan Ambush", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_quest_caravan_ambush_hint}Merchant or artisan quest from the base game. On by default. The notable must own at least one caravan, as in a town. Castles have no port, so it is always the land version. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestCaravanAmbush { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_army_of_poachers_name}Army of Poachers", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_quest_army_of_poachers_hint}Merchant quest from the base game. On by default. Needs a castle village, low castle security, a nearby hideout and a merchant with low mercy or honor, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestArmyOfPoachers { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_inn_and_out_name}Inn and Out (village)", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_quest_inn_and_out_hint}Village quest from the base game. On by default. A headman or landowner of a village owned by a castle can ask you to win back a land deed lost at a board game in the castle tavern. Needs the castle tavern. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestInnAndOut { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_deliver_herd_name}Deliver a Herd (village)", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_quest_deliver_herd_hint}Village quest from the base game. On by default. A headman of a horse, cattle or sheep village owned by a castle can offer it, and castle merchants and artisans can be the buyer you deliver the herd to. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestDeliverHerd { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_family_feud_name}Family Feud (village)", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_quest_family_feud_hint}Village quest from the base game. On by default. A landowner of a village owned by a castle can ask for help with a feud against another village of the same castle. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestFamilyFeud { get; set; }

        public static CastleMarketTownsSettings Current
        {
            get { return Instance; }
        }
    }
}
