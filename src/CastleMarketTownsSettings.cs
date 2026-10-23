using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace CastleMarketTowns
{
    public sealed class CastleMarketTownsSettings : AttributeGlobalSettings<CastleMarketTownsSettings>
    {
        internal const int MaxCompanionStayDays = 30;

        public override string Id { get { return "CastleMarketTowns"; } }
        public override string DisplayName { get { return "Castle Market Towns"; } }
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
            CastleCompanions = true;
            CompanionArrivalPercent = 100;
            CompanionDeparturePercent = 10;
            CompanionMinimumStayDays = 7;
            ApplyToKingdom = true;
            ApplyToNpc = true;
            MerchantNotables = 2;
            ArtisanNotables = 2;
            WorkshopCount = 1;
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

        [SettingPropertyBool("Enable castle markets", Order = 0, RequireRestart = false, HintText = "Turns every castle into a small trading settlement. The castle stays a castle: same walls and the same noble recruits from its villages. Rebellion is a separate switch.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public bool Enabled { get; set; }

        [SettingPropertyBool("Add a tavern", Order = 1, RequireRestart = false, HintText = "Adds a tavern room to the castle and a menu option to visit it. Tavern staff use the town tavern scenes.")]
        [SettingPropertyGroup("General")]
        public bool EnableTavern { get; set; }

        [SettingPropertyBool("Keep brothels out", Order = 2, RequireRestart = false, HintText = "Independent of Enable castle markets and on by default. Captivity Events adds its brothel to the town menu and to a location named brothel. Castles keep the castle menu, and any location named brothel is removed from every castle. Turn this off to let another mod keep a brothel in castles.")]
        [SettingPropertyGroup("General")]
        public bool BlockBrothels { get; set; }

        [SettingPropertyBool("Castles can rebel", Order = 3, RequireRestart = false, HintText = "On by default. A castle uses the same loyalty rebellion check as a town. Turn this off and castles go back to never rebelling.")]
        [SettingPropertyGroup("General")]
        public bool AllowRebellion { get; set; }

        [SettingPropertyBool("Castle notable quests", Order = 4, RequireRestart = false, HintText = "On by default. Master switch for the quests castle merchants and artisans can offer, and for the village quests that point to a castle. Pick single quests in the Castle quests group. Each quest still needs the same situation as in a town. Turn this off and castles offer none of them.")]
        [SettingPropertyGroup("General")]
        public bool AllowNotableQuests { get; set; }

        [SettingPropertyBool("Override castle daily defaults", Order = 5, RequireRestart = false, HintText = "Daily defaults only. Castle projects have their own switch. On by default. Slacken Garrison is replaced by Festival and Games, which adds 3 loyalty each day. Irrigation adds 1 bound-village hearth each day instead of the vanilla 0.5. Turn this off and both daily defaults go back to vanilla.")]
        [SettingPropertyGroup("General")]
        public bool ReplaceSlackenWithFestival { get; set; }

        [SettingPropertyBool("Override castle projects", Order = 6, RequireRestart = false, HintText = "Construction projects only. Daily defaults have their own switch. On by default. Castellan's Office keeps the vanilla wage cut and auto-recruit, and also adds tax of 3%, 6%, and 12%. Castles gain a Marketplace with tariffs of 4%, 8%, and 12%, and caravan access of 1.02, 1.04, and 1.06. Turn this off and castles go back to vanilla: no extra tax, and no marketplace.")]
        [SettingPropertyGroup("General")]
        public bool OverrideCastleProjects { get; set; }

        [SettingPropertyBool("Hot Butter in castle taverns", Order = 10, RequireRestart = false, HintText = "Only matters if Hot Butter is installed. On by default. Castle taverns get Hot Butter's Sell your body, fine prostitute, cheap prostitute and troop prostitutes options, with the same rules as in a town. Renting a room already exists in every castle through Hot Butter's own Stay at the castle with your partner option. The Captivity Events brothel stays out of castles. Without Hot Butter this does nothing.")]
        [SettingPropertyGroup("General")]
        public bool HotButterTavernOptions { get; set; }

        [SettingPropertyBool("Companions in castle taverns", IsToggle = true, Order = 0, RequireRestart = false, HintText = "On by default. On the first eligible day, creates a small, bounded group of companions in different castle taverns using the active companion system's unused templates and creation rules: the base game's, or a replacement such as Women in Calradia's. A save marker prevents reseeding. If no compatible companion system is found, the seed is skipped once and castle taverns fill only as companions move from towns. Afterward, unhired companions move between towns and nearby castles; the settings in this group control how often. Supports War Sails without requiring it. Turn this off and tracked unhired castle companions move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists, once they are free to move: not giving a quest, imprisoned, in a party or in the castle you are in. Turning it back on does not seed again. If another mod overrides companion behavior and causes problems in castles, leave this off. Needs Add a tavern.")]
        [SettingPropertyGroup("General/Castle companions")]
        public bool CastleCompanions { get; set; }

        [SettingPropertyInteger("Arrival chance percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "Each day, the chance that one unhired companion in a town moves to the tavern of one of the 3 castles closest to that town that has no unhired companion yet. Default 100. Arrivals and the one-time seed stop once castles hold a third of the free companions; if hiring later shrinks the free pool, castles can briefly hold more until companions leave. 0 stops new arrivals.")]
        [SettingPropertyGroup("General/Castle companions")]
        public int CompanionArrivalPercent { get; set; }

        [SettingPropertyInteger("Departure chance percent", 0, 100, "0", Order = 2, RequireRestart = false, HintText = "Each day, the chance that one companion this mod moved into a castle goes back to one of the 3 towns closest to that castle, preferring a town without an unhired companion. Default 10. 0 keeps them in castles until they are hired, the base game removes them, or the castle stops hosting companions because this group, the tavern or the castle's coverage is switched off.")]
        [SettingPropertyGroup("General/Castle companions")]
        public int CompanionDeparturePercent { get; set; }

        [SettingPropertyInteger("Minimum stay days", 1, MaxCompanionStayDays, "0", Order = 3, RequireRestart = false, HintText = "After this mod moves a companion, it does not move them again for this many days, into a castle or back to a town. Default 7. When a castle stops hosting companions because this group, the tavern or the castle's coverage is switched off, they move out regardless of this. The base game's own town-to-town companion swaps are not affected.")]
        [SettingPropertyGroup("General/Castle companions")]
        public int CompanionMinimumStayDays { get; set; }

        [SettingPropertyBool("Caravans trade at castles", Order = 9, RequireRestart = false, HintText = "On by default. Every caravan, including yours, NPC caravans, and caravans formed in a castle, can pick a castle as a trade stop, then buy and sell there like in a town. Only castles the mod is active on count, so the two clan switches decide which castles caravans visit. A caravan formed in a castle starts from that castle. Turn this off and caravans only trade in towns, as in vanilla.")]
        [SettingPropertyGroup("General")]
        public bool CaravansVisitCastles { get; set; }

        [SettingPropertyBool("Apply to your kingdom's clans", Order = 7, RequireRestart = false, HintText = "On by default. Covers castles owned by other clans in the kingdom your clan belongs to, and lets lords of those clans recruit at any castle the mod covers, as they do in towns and villages. Your own clan's castles are always changed. Turn this off and those castles go back to vanilla: the tavern, the mod workshops and the castle Marketplace are removed, the prosperity and goods the mod added are taken back, and the merchants and artisans the mod created move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Castles outside your kingdom use the next switch.")]
        [SettingPropertyGroup("General")]
        public bool ApplyToKingdom { get; set; }

        [SettingPropertyBool("Apply to other kingdoms and clans", Order = 8, RequireRestart = false, HintText = "On by default. Covers castles owned by any clan outside your kingdom: other kingdoms and independent clans. If your clan has no kingdom, this covers every castle you do not own. It also lets lords and caravans of those clans recruit at any castle the mod covers, as they do in towns and villages. Turn it off and they cannot recruit at castles. Turn this off and those castles go back to vanilla: the tavern, the mod workshops and the castle Marketplace are removed, the prosperity and goods the mod added are taken back, and the merchants and artisans the mod created move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists.")]
        [SettingPropertyGroup("General")]
        public bool ApplyToNpc { get; set; }

        [SettingPropertyInteger("Merchants", 0, 4, "0", Order = 0, RequireRestart = false, HintText = "A town keeps 2 merchants. They are the notables who can send caravans. Default 2. Every notable also gives up to 6 recruits. Lowering it moves extra merchants the mod created to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Merchants from the base game or other mods stay.")]
        [SettingPropertyGroup("People and workshops", GroupOrder = 1)]
        public int MerchantNotables { get; set; }

        [SettingPropertyInteger("Artisans", 0, 4, "0", Order = 1, RequireRestart = false, HintText = "A town keeps 1 artisan. Artisans own workshops. Default 2, so a castle has 4 notables, between a village's 3 and a town's 5. Every notable also gives up to 6 recruits. Lowering it moves extra artisans the mod created to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists. Artisans from the base game or other mods stay.")]
        [SettingPropertyGroup("People and workshops")]
        public int ArtisanNotables { get; set; }

        [SettingPropertyInteger("Workshops", 0, 4, "0", Order = 2, RequireRestart = false, HintText = "Towns start with 4 workshop slots. Castles start with 1. Change this at any time. Castles gain or lose slots to match, from 0 to 4. Only slots the mod added are removed, and never one owned by you or a lord.")]
        [SettingPropertyGroup("People and workshops")]
        public int WorkshopCount { get; set; }

        [SettingPropertyInteger("Merchant purse", 0, 50000, "0", Order = 3, RequireRestart = false, HintText = "Gold given to a merchant this mod creates, if they have less. Vanilla caravans need a large purse before a notable will form one.")]
        [SettingPropertyGroup("People and workshops")]
        public int MerchantGold { get; set; }

        [SettingPropertyInteger("Prosperity floor", 0, 4000, "0", Order = 0, RequireRestart = false, HintText = "Applied once per castle. If prosperity is below this, it is raised to it. Towns in the base game sit around 1700 to 5600, castles around 420 to 1100. Default 1000, the top of the castle range, so a new castle market starts modest and grows through trade. Prosperity also drives castle tax, food use and militia. 0 leaves prosperity alone.")]
        [SettingPropertyGroup("Scale", GroupOrder = 2)]
        public int ProsperityFloor { get; set; }

        [SettingPropertyInteger("Market stock percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "Applied once per castle. The castle market starts with this share of the trade goods held by the nearest reachable town of its culture, or the nearest reachable town if none exists. 100 matches that town. Default 25, a quarter, so a new castle never rivals a town. Later stock comes from workshops, caravans and normal trade. If the mod stops covering a castle, the goods it added are taken back.")]
        [SettingPropertyGroup("Scale")]
        public int MarketPercent { get; set; }

        [SettingPropertyInteger("Castle production percent", 0, 100, "0", Order = 0, RequireRestart = false, HintText = "Each day the castle market gains this share of what its own villages produce, using the base game village production. The villages still produce their full amount and send it to their town, so towns lose nothing. Scaled by castle prosperity, from half at 800 to one and a half at 2400. Default 20: a castle at 1600 prosperity that consumes half as much is about a fifth of an average town. 0 turns production off.")]
        [SettingPropertyGroup("Castle market", GroupOrder = 5)]
        public int CastleProductionPercent { get; set; }

        [SettingPropertyInteger("Castle consumption percent", 10, 100, "0", Order = 1, RequireRestart = false, HintText = "How much a castle's people use up each day, compared with a town of the same prosperity. Villages use nothing from a market and towns use 100. Default 50 sits between them. Prices still follow supply and demand as in a town.")]
        [SettingPropertyGroup("Castle market")]
        public int CastleConsumptionPercent { get; set; }

        [SettingPropertyInteger("Castle stock ceiling percent", 10, 100, "0", Order = 2, RequireRestart = false, HintText = "Safety cap for extreme settings. Castle production stops once the castle holds this share of its town's stock of that good, unless that is below the castle's own reserve. Default 60, so a castle stays below its town unless that town is in trouble. Goods brought by caravans or traders are not capped.")]
        [SettingPropertyGroup("Castle market")]
        public int CastleStockCeilingPercent { get; set; }

        [SettingPropertyBool("Sell castle surplus to its town", Order = 3, RequireRestart = false, HintText = "On by default. Like villagers, the castle sells goods it produces beyond its reserve to the town its villages trade with, and the castle market keeps the gold. Towns gain stock instead of losing it. Only goods the castle's villages make are sold, and only as much as the town can pay for.")]
        [SettingPropertyGroup("Castle market")]
        public bool ExportCastleSurplus { get; set; }

        [SettingPropertyInteger("Castle reserve days", 1, 30, "0", Order = 4, RequireRestart = false, HintText = "How many days of its own production or consumption, whichever is larger, the castle keeps of each good it produces before selling the rest to its town. Default 10.")]
        [SettingPropertyGroup("Castle market")]
        public int CastleStockDays { get; set; }

        [SettingPropertyBool("Caravans favour low-stock castles", Order = 5, RequireRestart = false, HintText = "On by default. When a castle holds less than 35 percent of its town's trade goods, caravans rate it higher as a trade stop. The emptier the castle, the larger the boost. Well-stocked castles get no boost, so caravans keep favouring towns. Needs Caravans trade at castles.")]
        [SettingPropertyGroup("Castle market")]
        public bool CaravansFavourLowStock { get; set; }

        [SettingPropertyInteger("Caravan boost percent", 0, 100, "0", Order = 6, RequireRestart = false, HintText = "Largest boost to a caravan's rating of a nearly empty castle. Default 25. It shrinks to 0 as the castle reaches 35 percent of its town's trade goods.")]
        [SettingPropertyGroup("Castle market")]
        public int CaravanBonusPercent { get; set; }

        [SettingPropertyBool("Siege food from the castle market", Order = 7, RequireRestart = false, HintText = "On by default. Like a town, a besieged castle that runs out of food takes food from its own market. Usually adds a few days to a siege. Turn this off and the market food is not used during sieges, as in a vanilla castle.")]
        [SettingPropertyGroup("Castle market")]
        public bool SiegeFoodFromMarket { get; set; }

        [SettingPropertyBool("Lords buy food at castles", Order = 8, RequireRestart = false, HintText = "On by default. Lords and their armies can buy food in a castle market, as in a town, but only food above the castle's reserve, so the castle keeps its siege food. Turn this off and lords only buy food in towns and villages.")]
        [SettingPropertyGroup("Castle market")]
        public bool LordsBuyCastleFood { get; set; }

        [SettingPropertyInteger("Castle tax percent", 10, 150, "0", Order = 0, RequireRestart = false, HintText = "Prosperity tax of a castle, compared with a town of the same prosperity. Default 80. With the 1000 prosperity floor a new castle pays about 280 a day, above the vanilla castle average of about 245, and more as trade raises prosperity. Town tax policies, the Khuzait tax penalty, Road Tolls, State Monopolies and the Tollgates perk also apply to castles.")]
        [SettingPropertyGroup("Castle income", GroupOrder = 6)]
        public int CastleTaxPercent { get; set; }

        [SettingPropertyInteger("Trade prosperity percent", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "In towns, goods sold in the market raise prosperity. Castles get this share of that effect. Default 50. Prosperity in turn raises tax. 0 keeps it town-only, as in vanilla.")]
        [SettingPropertyGroup("Castle income")]
        public int TradeProsperityPercent { get; set; }

        [SettingPropertyBool("Town healing in castles", Order = 0, RequireRestart = false, HintText = "On by default. Needs the castle tavern. Pristine Streets, A Good Day's Rest and Good Lodgings work while resting in a castle, and the governor's Medicine skill speeds up garrison healing, as in a town. Castles already give the base game's walled-settlement healing.")]
        [SettingPropertyGroup("Castle services", GroupOrder = 7)]
        public bool CastleHealing { get; set; }

        [SettingPropertyBool("Castles repair damage", Order = 1, RequireRestart = false, HintText = "On by default. Like towns and villages, a castle slowly restores its settlement health after raids or sieges, with Clean Infrastructure and In Best Light working as in a town.")]
        [SettingPropertyGroup("Castle services")]
        public bool CastleRepair { get; set; }

        [SettingPropertyBool("Castle smithy", Order = 2, RequireRestart = false, HintText = "On by default. Adds Enter smithy to the castle menu, the same smithy screen as in towns. Smith, smelt and refine with your own materials, and smithing stamina recovers in castles as in towns. Smithing orders stay in towns, so the castle smithy shows none. Turn this off and the option is hidden.")]
        [SettingPropertyGroup("Castle services")]
        public bool CastleSmithy { get; set; }

        [SettingPropertyBool("Recruit troops in castles", Order = 0, RequireRestart = false, HintText = "On by default. Adds Recruit troops to the castle menu, the same screen as in towns and villages. The castle's merchants and artisans offer volunteers of the castle's culture, which grow in tier over time like any notable's. Lords recruit there too and travel to castles for recruits, as in towns and villages; the two Apply to switches decide which clans may. Turn this off and castle notables stop producing volunteers.")]
        [SettingPropertyGroup("Castle recruits", GroupOrder = 4)]
        public bool CastleRecruits { get; set; }

        [SettingPropertyInteger("Noble recruit chance", 0, 100, "0", Order = 1, RequireRestart = false, HintText = "Chance that a new castle volunteer starts on the culture's noble troop line instead of the basic one. Towns are 0 and villages owned by a castle are 100. Default 50 sits between them. Only new volunteers are affected.")]
        [SettingPropertyGroup("Castle recruits")]
        public int NobleRecruitPercent { get; set; }

        [SettingPropertyInteger("Recruit speed percent", 10, 100, "0", Order = 2, RequireRestart = false, HintText = "How fast the castle's merchants and artisans produce volunteers, compared with a town notable. Village recruits are unchanged, including the noble troops from castle villages.")]
        [SettingPropertyGroup("Castle recruits")]
        public int RecruitPercent { get; set; }

        [SettingPropertyInteger("Castle Marketplace cost percent", 25, 100, "0", Order = 3, RequireRestart = false, HintText = "Construction cost of the castle Marketplace, compared with a town Marketplace. Default 60. Only changes construction that has not been done yet.")]
        [SettingPropertyGroup("Scale")]
        public int MarketplaceCostPercent { get; set; }

        [SettingPropertyBool("Escort Merchant Caravan", Order = 0, RequireRestart = false, HintText = "Merchant quest. On by default. Needs low security at the castle, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests", GroupOrder = 3)]
        public bool QuestEscortCaravan { get; set; }

        [SettingPropertyBool("Needs Help With Brigands", Order = 1, RequireRestart = false, HintText = "Merchant quest. On by default. Needs a bandit hideout near the castle, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestBrigands { get; set; }

        [SettingPropertyBool("Overpriced Raw Materials", Order = 2, RequireRestart = false, HintText = "Artisan quest. On by default. Needs a raw material to be expensive in the castle market, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestOverpricedMaterials { get; set; }

        [SettingPropertyBool("Artisan Can't Sell Products", Order = 3, RequireRestart = false, HintText = "Artisan quest from the base game. On by default. Needs a merchant in the same castle and a town nearby to deliver to, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestArtisanCantSell { get; set; }

        [SettingPropertyBool("Caravan Ambush", Order = 4, RequireRestart = false, HintText = "Merchant or artisan quest from the base game. On by default. The notable must own at least one caravan, as in a town. Castles have no port, so it is always the land version. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestCaravanAmbush { get; set; }

        [SettingPropertyBool("Army of Poachers", Order = 5, RequireRestart = false, HintText = "Merchant quest from the base game. On by default. Needs a castle village, low castle security, a nearby hideout and a merchant with low mercy or honor, as in a town. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestArmyOfPoachers { get; set; }

        [SettingPropertyBool("Inn and Out (village)", Order = 6, RequireRestart = false, HintText = "Village quest from the base game. On by default. A headman or landowner of a village owned by a castle can ask you to win back a land deed lost at a board game in the castle tavern. Needs the castle tavern. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestInnAndOut { get; set; }

        [SettingPropertyBool("Deliver a Herd (village)", Order = 7, RequireRestart = false, HintText = "Village quest from the base game. On by default. A headman of a horse, cattle or sheep village owned by a castle can offer it, and castle merchants and artisans can be the buyer you deliver the herd to. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestDeliverHerd { get; set; }

        [SettingPropertyBool("Family Feud (village)", Order = 8, RequireRestart = false, HintText = "Village quest from the base game. On by default. A landowner of a village owned by a castle can ask for help with a feud against another village of the same castle. Quests already running are not affected.")]
        [SettingPropertyGroup("Castle quests")]
        public bool QuestFamilyFeud { get; set; }

        public static CastleMarketTownsSettings Current
        {
            get { return Instance; }
        }
    }
}
