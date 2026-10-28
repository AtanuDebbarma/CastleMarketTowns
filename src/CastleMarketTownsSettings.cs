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
        internal const int MaxFixedCastleWorkshops = 100;
        internal const int MaxSkillCooldownDays = 15;
        internal const int MaxAttributeCooldownDays = 20;

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
        private const string GroupCastleTraining = "{=cmt_mcm_group_castle_training}Castle training";

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
            AutoTraderSupport = true;
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
            CastleWorkshopMax = 10;
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
            CastleTraining = true;
            TrainPlayer = true;
            TrainCompanions = true;
            TrainingGoldCost = 1000;
            TrainingInfluenceCost = 10;
            SkillsPerSession = 3;
            SkillGainMin = 2;
            SkillGainMax = 10;
            MaxTrainableSkill = 150;
            SkillCooldownDays = 7;
            AttributeTraining = true;
            AttributeGoldCost = 5000;
            AttributeInfluenceCost = 25;
            AttributeCooldownDays = 7;
        }

        [SettingPropertyBool("{=cmt_mcm_enabled_name}Enable castle markets", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_enabled_hint}Turns every castle into a small trading settlement. The castle stays a castle: same walls and the same noble recruits from its villages. Rebellion is a separate switch.")]
        [SettingPropertyGroup(GroupGeneral, GroupOrder = 0)]
        public bool Enabled { get; set; }

        [SettingPropertyBool("{=cmt_mcm_enable_tavern_name}Add a tavern", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_enable_tavern_hint}Adds a tavern room to the castle and a menu option to visit it. Tavern staff use the town tavern scenes.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool EnableTavern { get; set; }

        [SettingPropertyBool("{=cmt_mcm_block_brothels_name}Keep brothels out", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_block_brothels_hint}On by default, even with Enable castle markets off. Removes any brothel location, such as Captivity Events', from castles. Turn this off to let another mod keep a brothel in castles.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool BlockBrothels { get; set; }

        [SettingPropertyBool("{=cmt_mcm_allow_rebellion_name}Castles can rebel", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_allow_rebellion_hint}On by default. A castle uses the same loyalty rebellion check as a town. Turn this off and castles go back to never rebelling.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool AllowRebellion { get; set; }

        [SettingPropertyBool("{=cmt_mcm_allow_notable_quests_name}Castle notable quests", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_allow_notable_quests_hint}On by default. Master switch for quests from castle merchants and artisans, and for village quests that point to a castle. Pick single quests in the Castle quests group.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool AllowNotableQuests { get; set; }

        [SettingPropertyBool("{=cmt_mcm_replace_slacken_with_festival_name}Override castle daily defaults", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_replace_slacken_with_festival_hint}On by default. Daily defaults only. Slacken Garrison becomes Festival and Games (+3 loyalty a day), and Irrigation gives 1 village hearth a day instead of 0.5. Off restores vanilla.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ReplaceSlackenWithFestival { get; set; }

        [SettingPropertyBool("{=cmt_mcm_override_castle_projects_name}Override castle projects", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_override_castle_projects_hint}On by default. Construction projects only. Castellan's Office also adds 3%, 6% and 12% tax, and castles gain a Marketplace with 4%, 8% and 12% tariffs and better caravan access. Off restores vanilla.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool OverrideCastleProjects { get; set; }

        [SettingPropertyBool("{=cmt_mcm_hot_butter_tavern_options_name}Hot Butter in castle taverns", Order = 10, RequireRestart = false, HintText = "{=cmt_mcm_hot_butter_tavern_options_hint}Only matters with Hot Butter installed. On by default. Castle taverns get Hot Butter's tavern options, with the same rules as in towns.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool HotButterTavernOptions { get; set; }

        [SettingPropertyBool("{=cmt_mcm_improved_garrisons_support_name}Improved Garrisons support", Order = 11, RequireRestart = false, HintText = "{=cmt_mcm_improved_garrisons_support_hint}Only matters with Improved Garrisons installed. On by default. Its button moves into Go to the castle, its tooltips show castle tariffs and tax, and its parties recruit and trade at castles as in towns. Its files are never changed.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ImprovedGarrisonsSupport { get; set; }

        [SettingPropertyBool("{=cmt_mcm_autotrader_support_name}AutoTrader support", Order = 12, RequireRestart = false, HintText = "{=cmt_mcm_autotrader_support_hint}Only matters with AutoTrader installed. On by default. Covered castle menus get Automatically trade wares next to Trade; AutoTrader uses the castle market and its own settings (Alt+A). Towns, villages and caravans are unchanged. If AutoTrader is missing or its API changes, this does nothing and the game log says so. Its files are never changed.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool AutoTraderSupport { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_companions_name}Companions in castle taverns", IsToggle = true, Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_companions_hint}On by default. Unhired companions also appear in castle taverns: a small group once, then companions move between towns and nearby castles over time. Works with the base game and most companion mods. Turn this off to send them back to towns, or if a companion mod misbehaves in castles. Needs Add a tavern.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public bool CastleCompanions { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_arrival_percent_name}Arrival chance percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_companion_arrival_percent_hint}Daily chance that an unhired companion in a town moves to a nearby castle that has none. Default 100. Arrivals stop once castles hold a third of the free companions. 0 stops arrivals.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionArrivalPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_departure_percent_name}Departure chance percent", 0, 100, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_companion_departure_percent_hint}Daily chance that a companion in a castle goes back to a nearby town. Default 10. 0 keeps them in castles until they are hired.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionDeparturePercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_companion_minimum_stay_days_name}Minimum stay days", 1, MaxCompanionStayDays, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_companion_minimum_stay_days_hint}Days before a companion moved by this mod can move again. Default 7. Ignored when a castle stops hosting companions.")]
        [SettingPropertyGroup(GroupCastleCompanions)]
        public int CompanionMinimumStayDays { get; set; }

        [SettingPropertyBool("{=cmt_mcm_caravans_visit_castles_name}Caravans trade at castles", Order = 9, RequireRestart = false, HintText = "{=cmt_mcm_caravans_visit_castles_hint}On by default. Caravans, including yours, can trade at castles the mod covers as they do in towns, and visits count toward trade agreements. Off: caravans trade only in towns.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool CaravansVisitCastles { get; set; }

        [SettingPropertyBool("{=cmt_mcm_apply_to_kingdom_name}Apply to your kingdom's clans", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_apply_to_kingdom_hint}On by default. Covers castles of other clans in your kingdom, and lets their lords recruit at castles. Your own clan's castles are always covered. Off returns those castles to vanilla: what the mod added is removed and workshops you own there are sold.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ApplyToKingdom { get; set; }

        [SettingPropertyBool("{=cmt_mcm_apply_to_npc_name}Apply to other kingdoms and clans", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_apply_to_npc_hint}On by default. Covers castles of clans outside your kingdom, or every castle you do not own if you have no kingdom, and lets their lords and caravans recruit at castles. Off returns those castles to vanilla: what the mod added is removed and workshops you own there are sold.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool ApplyToNpc { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_merchant_notables_name}Merchants", 0, 4, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_merchant_notables_hint}Merchants per castle. Default 2, as in a town. Merchants can form caravans and give recruits. Lowering it moves extra merchants the mod created to a town.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops, GroupOrder = 1)]
        public int MerchantNotables { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_artisan_notables_name}Artisans", 0, 4, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_artisan_notables_hint}Artisans per castle. Default 2; a town has 1. Artisans own workshops and give recruits. Lowering it moves extra artisans the mod created to a town.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int ArtisanNotables { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_workshop_slots_name}Castle workshop slots", 0, MaxCastleWorkshopSlots, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_workshop_slots_hint}Workshops per castle, from 0 to 8. Default 2. Lowering it removes only slots the mod added, never one owned by you or a lord.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int WorkshopCount { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_town_workshop_slots_name}Town workshop slots", BaseTownWorkshopSlots, MaxTownWorkshopSlots, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_town_workshop_slots_hint}Workshops per town, from 4 to 10. Default 4, as in the base game, where one is the artisans' own and cannot be bought. Slots above 4 work like any workshop but have no building in the town scene. The base game's 4 are never removed.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int TownWorkshopCount { get; set; }

        // Keeps its 1.2.0 property name so a saved value carries over; it now raises the town limit only.
        [SettingPropertyInteger("{=cmt_mcm_town_workshop_limit_name}Extra town workshop limit", 0, 10, "0", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_town_workshop_limit_hint}Extra town workshops you may own on top of the base game's limit of clan tier + 1. Default 0. Lowering it never takes a workshop away.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int ExtraWorkshopLimit { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_workshop_limit_name}Extra castle workshop limit", 0, 10, "0", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_castle_workshop_limit_hint}Extra castle workshops you may own on top of the castle limit of clan tier + 1. Default 0. The castle limit is separate from towns, so mods that change the town limit do not change it. Lowering it never takes a workshop away.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int CastleWorkshopLimit { get; set; }

        [SettingPropertyBool("{=cmt_mcm_fixed_castle_workshop_limit_name}Fixed castle workshop limit", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_fixed_castle_workshop_limit_hint}Off by default. On uses Castle workshop limit below instead of clan tier + 1 and Extra castle workshop limit, whatever your clan tier.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public bool FixedCastleWorkshopLimit { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_workshop_max_name}Castle workshop limit", 1, MaxFixedCastleWorkshops, "0", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_castle_workshop_max_hint}How many castle workshops you may own while Fixed castle workshop limit is on. Default 10. Lowering it never takes a workshop away.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int CastleWorkshopMax { get; set; }

        [SettingPropertyBool("{=cmt_mcm_unlimited_castle_workshops_name}Unlimited castle workshops", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_unlimited_castle_workshops_hint}Off by default. Removes the castle workshop limit, whatever the settings above say.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public bool UnlimitedCastleWorkshops { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_merchant_gold_name}Merchant purse", 0, 50000, "0", Order = 9, RequireRestart = false, HintText = "{=cmt_mcm_merchant_gold_hint}Gold given to a merchant this mod creates, if they have less. Vanilla caravans need a large purse before a notable will form one.")]
        [SettingPropertyGroup(GroupPeopleAndWorkshops)]
        public int MerchantGold { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_prosperity_floor_name}Prosperity floor", 0, 4000, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_prosperity_floor_hint}Applied once per castle: lower prosperity is raised to this. Default 1000, the top of the vanilla castle range; towns are about 1700 to 5600. 0 leaves prosperity alone.")]
        [SettingPropertyGroup(GroupScale, GroupOrder = 2)]
        public int ProsperityFloor { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_market_percent_name}Market stock percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_market_percent_hint}Applied once per castle. The market starts with this share of the nearest town's trade goods. Default 25. Later stock comes from production, caravans and trade.")]
        [SettingPropertyGroup(GroupScale)]
        public int MarketPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_production_percent_name}Castle production percent", 0, 100, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_production_percent_hint}Each day the castle market gains this share of its villages' production, scaled by prosperity. The villages still send their full output to their town. Default 20. 0 turns it off.")]
        [SettingPropertyGroup(GroupCastleMarket, GroupOrder = 5)]
        public int CastleProductionPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_consumption_percent_name}Castle consumption percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_castle_consumption_percent_hint}How much a castle's people use up each day, compared with a town of the same prosperity. Default 50.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleConsumptionPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_stock_ceiling_percent_name}Castle stock ceiling percent", 10, 100, "0", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_stock_ceiling_percent_hint}Safety cap. Castle production stops once the castle holds this share of its town's stock of a good. Default 60. Goods brought by caravans are not capped.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleStockCeilingPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_export_castle_surplus_name}Sell castle surplus to its town", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_export_castle_surplus_hint}On by default. Like villagers, the castle sells goods its villages make beyond its reserve to their town and keeps the gold.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool ExportCastleSurplus { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_stock_days_name}Castle reserve days", 1, 30, "0", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_castle_stock_days_hint}How many days of its own production or consumption, whichever is larger, the castle keeps of each good it produces before selling the rest to its town. Default 10.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CastleStockDays { get; set; }

        [SettingPropertyBool("{=cmt_mcm_caravans_favour_low_stock_name}Caravans favour low-stock castles", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_caravans_favour_low_stock_hint}On by default. Caravans rate castles low on goods higher as trade stops, so empty castles fill up. Needs Caravans trade at castles.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool CaravansFavourLowStock { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_caravan_bonus_percent_name}Caravan boost percent", 0, 100, "0", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_caravan_bonus_percent_hint}Largest boost to a caravan's rating of a nearly empty castle. Default 25. It shrinks to 0 as the castle reaches 35 percent of its town's trade goods.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public int CaravanBonusPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_siege_food_from_market_name}Siege food from the castle market", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_siege_food_from_market_hint}On by default. A besieged castle that runs out of food uses its market's food, as a town does.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool SiegeFoodFromMarket { get; set; }

        [SettingPropertyBool("{=cmt_mcm_lords_buy_castle_food_name}Lords buy food at castles", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_lords_buy_castle_food_hint}On by default. Lords can buy castle food above the castle's siege reserve, and travel to castles with food to spare. Off: they buy food only in towns and villages.")]
        [SettingPropertyGroup(GroupCastleMarket)]
        public bool LordsBuyCastleFood { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_castle_tax_percent_name}Castle tax percent", 10, 150, "0", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_tax_percent_hint}Castle prosperity tax, compared with a town of the same prosperity. Default 80. Town tax policies and perks also apply to castles.")]
        [SettingPropertyGroup(GroupCastleIncome, GroupOrder = 6)]
        public int CastleTaxPercent { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_trade_prosperity_percent_name}Trade prosperity percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_trade_prosperity_percent_hint}In towns, goods sold in the market raise prosperity. Castles get this share of that effect. Default 50. Prosperity in turn raises tax. 0 keeps it town-only, as in vanilla.")]
        [SettingPropertyGroup(GroupCastleIncome)]
        public int TradeProsperityPercent { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_healing_name}Town healing in castles", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_healing_hint}On by default. Needs the castle tavern. Town healing perks work while resting in a castle, and the governor's Medicine speeds garrison healing, as in a town.")]
        [SettingPropertyGroup(GroupCastleServices, GroupOrder = 7)]
        public bool CastleHealing { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_repair_name}Castles repair damage", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_castle_repair_hint}On by default. Like towns and villages, a castle slowly restores its settlement health after raids or sieges, with Clean Infrastructure and In Best Light working as in a town.")]
        [SettingPropertyGroup(GroupCastleServices)]
        public bool CastleRepair { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_smithy_name}Castle smithy", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_castle_smithy_hint}On by default. Adds Enter smithy to the castle menu, as in towns but without smithing orders. Off hides it.")]
        [SettingPropertyGroup(GroupCastleServices)]
        public bool CastleSmithy { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_recruits_name}Recruit troops in castles", Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_recruits_hint}On by default. Adds Recruit troops to the castle menu. Castle merchants and artisans offer volunteers, and lords recruit there too; the Apply to switches decide which clans may. Off stops castle volunteers.")]
        [SettingPropertyGroup(GroupCastleRecruits, GroupOrder = 4)]
        public bool CastleRecruits { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_noble_recruit_percent_name}Noble recruit chance", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_noble_recruit_percent_hint}Chance that a new castle volunteer starts on the noble troop line. Default 50; towns are 0 and castle villages 100.")]
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

        [SettingPropertyBool("{=cmt_mcm_quest_artisan_cant_sell_name}Artisan Can't Sell Products", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_quest_artisan_cant_sell_hint}Artisan quest from the base game. On by default. Needs a merchant in the same castle and a nearby town or covered castle to deliver to. If the destination is a covered castle whose market gate is blocked while the quest is active (refused entry, disguise, or face charges), you can pay the guards to take the goods in for half the reward. Quests already running are not affected.")]
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

        [SettingPropertyBool("{=cmt_mcm_quest_deliver_herd_name}Deliver a Herd (village)", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_quest_deliver_herd_hint}Village quest from the base game. On by default. A headman of a horse, cattle or sheep village owned by a castle can offer it, and castle merchants and artisans can be the buyer you deliver the herd to. If that castle's market gate is blocked while the quest is active (refused entry, disguise, or face charges), you can pay the guards to take the herd in for half the reward. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestDeliverHerd { get; set; }

        [SettingPropertyBool("{=cmt_mcm_quest_family_feud_name}Family Feud (village)", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_quest_family_feud_hint}Village quest from the base game. On by default. A landowner of a village owned by a castle can ask for help with a feud against another village of the same castle. Quests already running are not affected.")]
        [SettingPropertyGroup(GroupCastleQuests)]
        public bool QuestFamilyFeud { get; set; }

        [SettingPropertyBool("{=cmt_mcm_castle_training_name}Castle training", IsToggle = true, Order = 0, RequireRestart = false, HintText = "{=cmt_mcm_castle_training_hint}On by default. Adds Visit the castle trainers to Go to the castle. You and the companions and adult family members in your party can pay to raise skills or an attribute, with a cooldown. Gains are permanent and stay if the mod is removed.")]
        [SettingPropertyGroup(GroupCastleTraining, GroupOrder = 8)]
        public bool CastleTraining { get; set; }

        [SettingPropertyBool("{=cmt_mcm_train_player_name}Train yourself", Order = 1, RequireRestart = false, HintText = "{=cmt_mcm_train_player_hint}On by default. You can train at the castle.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public bool TrainPlayer { get; set; }

        [SettingPropertyBool("{=cmt_mcm_train_companions_name}Train companions", Order = 2, RequireRestart = false, HintText = "{=cmt_mcm_train_companions_hint}On by default. Adult companions and family members of your clan who are in your party can train. Heroes elsewhere, prisoners and visiting lords cannot.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public bool TrainCompanions { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_training_gold_cost_name}Skill training gold cost", 0, 10000, "0", Order = 3, RequireRestart = false, HintText = "{=cmt_mcm_training_gold_cost_hint}Gold for one skill training session, however many heroes train in it. Default 1000.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int TrainingGoldCost { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_training_influence_cost_name}Skill training influence cost", 0, 100, "0", Order = 4, RequireRestart = false, HintText = "{=cmt_mcm_training_influence_cost_hint}Your clan's influence for one skill training session. Default 10.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int TrainingInfluenceCost { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_skills_per_session_name}Skills per session", 1, 8, "0", Order = 5, RequireRestart = false, HintText = "{=cmt_mcm_skills_per_session_hint}Most random skills of the chosen group each hero improves in one session. Default 3. Only skills below the maximum trainable level are picked, so a hero with fewer of them improves fewer.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int SkillsPerSession { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_skill_gain_min_name}Skill gain minimum", 1, 20, "0", Order = 6, RequireRestart = false, HintText = "{=cmt_mcm_skill_gain_min_hint}Fewest levels a trained skill gains. Default 2. If it is above the maximum, the two are swapped.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int SkillGainMin { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_skill_gain_max_name}Skill gain maximum", 1, 20, "0", Order = 7, RequireRestart = false, HintText = "{=cmt_mcm_skill_gain_max_hint}Most levels a trained skill gains. Default 10. Levels are added directly: learning limits and focus points do not reduce them, and they give no character experience, so heroes do not level up from training.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int SkillGainMax { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_max_trainable_skill_name}Maximum trainable skill level", 50, 300, "0", Order = 8, RequireRestart = false, HintText = "{=cmt_mcm_max_trainable_skill_hint}Training never raises a skill above this level; beyond it, heroes must learn by doing. Default 150.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int MaxTrainableSkill { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_skill_cooldown_days_name}Skill training cooldown days", 2, MaxSkillCooldownDays, "0", Order = 9, RequireRestart = false, HintText = "{=cmt_mcm_skill_cooldown_days_hint}Days a hero waits after a skill training session before the next one, at any castle. Default 7.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int SkillCooldownDays { get; set; }

        [SettingPropertyBool("{=cmt_mcm_attribute_training_name}Attribute training", Order = 10, RequireRestart = false, HintText = "{=cmt_mcm_attribute_training_hint}On by default. Adds Train an attribute to the castle trainers: pick one hero and one attribute to raise by 1, never above the game's maximum. It has its own cost and cooldown, separate from skill training.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public bool AttributeTraining { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_attribute_gold_cost_name}Attribute training gold cost", 0, 20000, "0", Order = 11, RequireRestart = false, HintText = "{=cmt_mcm_attribute_gold_cost_hint}Gold for raising one attribute by 1. Default 5000.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int AttributeGoldCost { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_attribute_influence_cost_name}Attribute training influence cost", 0, 100, "0", Order = 12, RequireRestart = false, HintText = "{=cmt_mcm_attribute_influence_cost_hint}Your clan's influence for raising one attribute by 1. Default 25.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int AttributeInfluenceCost { get; set; }

        [SettingPropertyInteger("{=cmt_mcm_attribute_cooldown_days_name}Attribute training cooldown days", 5, MaxAttributeCooldownDays, "0", Order = 13, RequireRestart = false, HintText = "{=cmt_mcm_attribute_cooldown_days_hint}Days a hero waits after raising an attribute before raising another, at any castle. Default 7.")]
        [SettingPropertyGroup(GroupCastleTraining)]
        public int AttributeCooldownDays { get; set; }

        public static CastleMarketTownsSettings Current
        {
            get { return Instance; }
        }
    }
}
