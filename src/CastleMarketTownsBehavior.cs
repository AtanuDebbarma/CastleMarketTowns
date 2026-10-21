using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CastleMarketTowns
{
    public class CastleMarketTownsBehavior : CampaignBehaviorBase
    {
        private const string LegacyMarketplaceId = "castle_market_towns_marketplace";
        private const string WorkshopTagPrefix = "castle_market_town_workshop_";
        private const string ZeroSnapshotMarker = "*";
        private const string FestivalRole = "festival";
        private const string IrrigationRole = "irrigation";

        // Saves without this version only recorded which castles had their daily defaults swapped, not which buildings were added.
        private const string OwnershipVersion = "2";

        // Vanilla save entries over 32767 bytes corrupt the save; 8000 UTF-16 chars stay under 24000 UTF-8 bytes.
        private const int SaveChunkLength = 8000;

        // The distance caches store settlements that cannot be reached with a navigation type at about 1e30.
        private const float UnreachableDistance = 1e20f;

        private readonly HashSet<string> _prepared = new HashSet<string>();
        private readonly HashSet<string> _createdTaverns = new HashSet<string>();
        private readonly HashSet<string> _createdNotables = new HashSet<string>();
        private readonly HashSet<string> _createdMarketData = new HashSet<string>();
        private static readonly HashSet<string> OwnedMarketplaces = new HashSet<string>();
        private static readonly HashSet<string> OwnedDailyDefaults = new HashSet<string>();
        private static readonly HashSet<string> CreatedDailyBuildings = new HashSet<string>();
        private static readonly Dictionary<string, Building> MarketplaceBuildings = new Dictionary<string, Building>();
        private static readonly Dictionary<string, Building> DailyBuildings = new Dictionary<string, Building>();
        private readonly Dictionary<string, Location> _taverns = new Dictionary<string, Location>();
        private readonly Dictionary<string, TownMarketData> _markets = new Dictionary<string, TownMarketData>();
        private readonly Dictionary<string, float> _prosperityGrants = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _stockGrants = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _producedStock = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _originalMarketSupply = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _originalMarketDemand = new Dictionary<string, float>();
        private readonly HashSet<string> _zeroSnapshotCastles = new HashSet<string>();
        private string _preparedIds = "";
        private string _createdTavernIds = "";
        private string _createdNotableIds = "";
        private string _createdMarketDataIds = "";
        private string _ownedMarketplaceIds = "";
        private string _ownedDailyDefaultIds = "";
        private string _createdDailyBuildingIds = "";
        private string _ownershipVersion = "";
        private string _prosperityGrantData = "";
        private string _stockGrantData = "";
        private string _producedStockData = "";
        private string _marketSnapshotData = "";
        private bool _menusAdded;

        public CastleMarketTownsBehavior()
        {
            OwnedMarketplaces.Clear();
            OwnedDailyDefaults.Clear();
            CreatedDailyBuildings.Clear();
            MarketplaceBuildings.Clear();
            DailyBuildings.Clear();
            StockRatios.Clear();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlement);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                ValidateOwnership();
                _createdDailyBuildingIds = SerializeSet(CreatedDailyBuildings);
                _ownershipVersion = OwnershipVersion;
                _preparedIds = SerializeSet(_prepared);
                _createdTavernIds = SerializeSet(_createdTaverns);
                _createdNotableIds = SerializeSet(_createdNotables);
                _createdMarketDataIds = SerializeSet(_createdMarketData);
                _ownedMarketplaceIds = SerializeSet(OwnedMarketplaces);
                _ownedDailyDefaultIds = SerializeSet(OwnedDailyDefaults);
                _prosperityGrantData = SerializeFloatMap(_prosperityGrants);
                _stockGrantData = SerializeIntMap(_stockGrants);
                _producedStockData = SerializeIntMap(_producedStock);
                _marketSnapshotData = SerializeMarketSnapshots();
            }

            _preparedIds = SyncChunked(dataStore, "CastleMarketTowns_Prepared", _preparedIds);
            _createdTavernIds = SyncChunked(dataStore, "CastleMarketTowns_CreatedTaverns", _createdTavernIds);
            _createdNotableIds = SyncChunked(dataStore, "CastleMarketTowns_CreatedNotables", _createdNotableIds);
            _createdMarketDataIds = SyncChunked(dataStore, "CastleMarketTowns_CreatedMarketData", _createdMarketDataIds);
            _ownedMarketplaceIds = SyncChunked(dataStore, "CastleMarketTowns_OwnedMarketplaces", _ownedMarketplaceIds);
            _ownedDailyDefaultIds = SyncChunked(dataStore, "CastleMarketTowns_OwnedDailyDefaults", _ownedDailyDefaultIds);
            _prosperityGrantData = SyncChunked(dataStore, "CastleMarketTowns_ProsperityGrants", _prosperityGrantData);
            _stockGrantData = SyncChunked(dataStore, "CastleMarketTowns_StockGrants", _stockGrantData);
            _producedStockData = SyncChunked(dataStore, "CastleMarketTowns_ProducedStock", _producedStockData);
            _marketSnapshotData = SyncChunked(dataStore, "CastleMarketTowns_MarketSnapshots", _marketSnapshotData);
            _createdDailyBuildingIds = SyncChunked(dataStore, "CastleMarketTowns_CreatedDailyBuildings", _createdDailyBuildingIds);
            _ownershipVersion = SyncChunked(dataStore, "CastleMarketTowns_OwnershipVersion", _ownershipVersion);

            if (dataStore.IsLoading)
            {
                LoadSet(_preparedIds, _prepared);
                LoadSet(_createdTavernIds, _createdTaverns);
                LoadSet(_createdNotableIds, _createdNotables);
                LoadSet(_createdMarketDataIds, _createdMarketData);
                LoadSet(_ownedMarketplaceIds, OwnedMarketplaces);
                LoadSet(_ownedDailyDefaultIds, OwnedDailyDefaults);
                LoadFloatMap(_prosperityGrantData, _prosperityGrants);
                LoadIntMap(_stockGrantData, _stockGrants);
                LoadIntMap(_producedStockData, _producedStock);
                LoadMarketSnapshots(_marketSnapshotData);
                LoadSet(_createdDailyBuildingIds, CreatedDailyBuildings);
                if (_ownershipVersion != OwnershipVersion)
                {
                    foreach (string settlementId in OwnedDailyDefaults)
                    {
                        CreatedDailyBuildings.Add(MarketKey(settlementId, FestivalRole));
                        CreatedDailyBuildings.Add(MarketKey(settlementId, IrrigationRole));
                    }
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ExpandZeroSnapshots();
            ValidateOwnership();
            AddMenus(starter);
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsCastle && settlement.Town != null)
                {
                    MigrateLegacyMarketplace(settlement.Town);
                }
            }

            UnregisterLegacyMarketplace();

            foreach (Settlement settlement in Settlement.All)
            {
                if (!settlement.IsCastle || settlement.Town == null)
                {
                    continue;
                }

                if (AppliesTo(settlement))
                {
                    Prepare(settlement, true);
                    Settlement current = settlement;
                    Step("measure the stock of", settlement, delegate { UpdateStockRatio(current, MarketTown(current)); });
                }
                else
                {
                    Cleanup(settlement);
                }
            }
        }

        private void OnDailySettlement(Settlement settlement)
        {
            if (settlement == null || !settlement.IsCastle || settlement.Town == null)
            {
                return;
            }

            if (!AppliesTo(settlement))
            {
                Cleanup(settlement);
                return;
            }

            Prepare(settlement, true);
            RemoveBrothel(settlement);
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            Settlement marketTown = MarketTown(settlement);
            Dictionary<ItemObject, float> produced = new Dictionary<ItemObject, float>();
            Step("track the goods of", settlement, delegate { ClampLedgers(settlement); });
            if (settings != null)
            {
                Step("produce goods in", settlement, delegate { produced = DailyCastleProduction(settlement, settings); ProduceCastleGoods(settlement, marketTown, produced, settings); });
            }

            RunOfficialTownDailyTicks(settlement.Town);
            Step("track the goods of", settlement, delegate { ClampLedgers(settlement); });
            if (settings != null && settings.ExportCastleSurplus)
            {
                Step("export surplus from", settlement, delegate { ExportCastleSurplus(settlement, marketTown, produced, settings); });
            }

            Step("measure the stock of", settlement, delegate { UpdateStockRatio(settlement, marketTown); });
            RemoveBrothel(settlement);
        }

        private static Settlement MarketTown(Settlement castle)
        {
            Dictionary<Settlement, int> counts = new Dictionary<Settlement, int>();
            Settlement best = null;
            int bestCount = 0;
            foreach (Village village in castle.BoundVillages)
            {
                Settlement trade = village == null ? null : village.TradeBound;
                if (trade == null || !trade.IsTown)
                {
                    continue;
                }

                int count;
                counts.TryGetValue(trade, out count);
                count++;
                counts[trade] = count;
                if (count > bestCount)
                {
                    best = trade;
                    bestCount = count;
                }
            }

            return best ?? NearestTown(castle);
        }

        private static Dictionary<ItemObject, float> DailyCastleProduction(Settlement castle, CastleMarketTownsSettings settings)
        {
            Dictionary<ItemObject, float> produced = new Dictionary<ItemObject, float>();
            float share = settings.CastleProductionPercent / 100f;
            if (share <= 0f || castle.IsUnderSiege)
            {
                return produced;
            }

            float prosperityScale = MathF.Clamp(castle.Town.Prosperity / 1600f, 0.5f, 1.5f);
            foreach (Village village in castle.BoundVillages)
            {
                if (village == null || village.IsDeserted || village.VillageState != Village.VillageStates.Normal || village.VillageType == null)
                {
                    continue;
                }

                foreach (ItemObject item in ProductionItems(village.VillageType))
                {
                    float amount = Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, item).ResultNumber;
                    if (amount <= 0f)
                    {
                        continue;
                    }

                    float current;
                    produced.TryGetValue(item, out current);
                    produced[item] = current + amount * share * prosperityScale;
                }
            }

            return produced;
        }

        private static List<ItemObject> ProductionItems(VillageType villageType)
        {
            List<ItemObject> items = new List<ItemObject>();
            PropertyInfo property = typeof(VillageType).GetProperty("Productions");
            System.Collections.IEnumerable productions = property == null ? null : property.GetValue(villageType, null) as System.Collections.IEnumerable;
            if (productions == null)
            {
                return items;
            }

            foreach (object production in productions)
            {
                FieldInfo first = production == null ? null : production.GetType().GetField("Item1");
                ItemObject item = first == null ? null : first.GetValue(production) as ItemObject;
                if (item != null && !items.Contains(item))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private void ProduceCastleGoods(Settlement castle, Settlement marketTown, Dictionary<ItemObject, float> produced, CastleMarketTownsSettings settings)
        {
            foreach (KeyValuePair<ItemObject, float> pair in produced)
            {
                int amount = MBRandom.RoundRandomized(pair.Value);
                if (amount <= 0)
                {
                    continue;
                }

                int room = StockCeiling(castle, marketTown, pair.Key, pair.Value, settings) - castle.ItemRoster.GetItemNumber(pair.Key);
                if (room > 0)
                {
                    int added = Math.Min(amount, room);
                    castle.ItemRoster.AddToCounts(pair.Key, added);
                    ChangeProducedStock(castle, pair.Key, added);
                }
            }
        }

        private void ChangeProducedStock(Settlement castle, ItemObject item, int change)
        {
            string key = MarketKey(castle.StringId, item.StringId);
            int current;
            _producedStock.TryGetValue(key, out current);
            current = Math.Min(current + change, castle.ItemRoster == null ? 0 : castle.ItemRoster.GetItemNumber(item));
            if (current > 0)
            {
                _producedStock[key] = current;
            }
            else
            {
                _producedStock.Remove(key);
            }
        }

        // Goods the mod added are treated as sold or eaten first, oldest grant first, so goods bought in later stay out of cleanup.
        private void ClampLedgers(Settlement castle)
        {
            if (castle == null || castle.ItemRoster == null)
            {
                return;
            }

            string prefix = castle.StringId + "|";
            HashSet<string> keys = new HashSet<string>();
            CollectLedgerKeys(_stockGrants, prefix, keys);
            CollectLedgerKeys(_producedStock, prefix, keys);
            foreach (string key in keys)
            {
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(key.Substring(prefix.Length));
                int stock = item == null ? 0 : castle.ItemRoster.GetItemNumber(item);
                int granted;
                int produced;
                _stockGrants.TryGetValue(key, out granted);
                _producedStock.TryGetValue(key, out produced);
                int excess = granted + produced - stock;
                if (excess <= 0)
                {
                    continue;
                }

                int fromGrant = Math.Min(excess, granted);
                SetLedger(_stockGrants, key, granted - fromGrant);
                SetLedger(_producedStock, key, produced - (excess - fromGrant));
            }
        }

        private static void CollectLedgerKeys(Dictionary<string, int> ledger, string prefix, HashSet<string> keys)
        {
            foreach (string key in ledger.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }
        }

        private static void SetLedger(Dictionary<string, int> ledger, string key, int value)
        {
            if (value > 0)
            {
                ledger[key] = value;
            }
            else
            {
                ledger.Remove(key);
            }
        }

        private static bool CanTradeWith(Settlement castle, Settlement marketTown)
        {
            if (marketTown == null || marketTown.Town == null || marketTown.ItemRoster == null || castle.Town == null || castle.IsUnderSiege || marketTown.IsUnderSiege)
            {
                return false;
            }

            return castle.MapFaction == null || marketTown.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(castle.MapFaction, marketTown.MapFaction);
        }

        private static int SellToTown(Settlement castle, Settlement marketTown, ItemObject item, int count)
        {
            int price = marketTown.Town.GetItemPrice(item, null, true);
            if (count <= 0 || price <= 0)
            {
                return 0;
            }

            int sold = Math.Min(count, marketTown.Town.Gold / price);
            if (sold <= 0)
            {
                return 0;
            }

            castle.ItemRoster.AddToCounts(item, -sold);
            marketTown.ItemRoster.AddToCounts(item, sold);
            marketTown.Town.ChangeGold(-sold * price);
            castle.Town.ChangeGold(sold * price);
            return sold;
        }

        private void SellProducedLeftovers(Settlement castle)
        {
            Settlement marketTown = MarketTown(castle);
            if (marketTown != null && (castle.IsUnderSiege || marketTown.IsUnderSiege))
            {
                return;
            }

            bool canSell = CanTradeWith(castle, marketTown);
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, int> pair in _producedStock)
            {
                string settlementId;
                string itemId;
                if (SplitMarketKey(pair.Key, out settlementId, out itemId) && settlementId == castle.StringId)
                {
                    keys.Add(pair.Key);
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string settlementId;
                string itemId;
                SplitMarketKey(keys[i], out settlementId, out itemId);
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                int leftover = item == null || castle.ItemRoster == null ? 0 : Math.Min(_producedStock[keys[i]], castle.ItemRoster.GetItemNumber(item));
                if (leftover > 0)
                {
                    int sold = canSell ? SellToTown(castle, marketTown, item, leftover) : 0;
                    if (leftover > sold)
                    {
                        castle.ItemRoster.AddToCounts(item, -(leftover - sold));
                    }
                }

                _producedStock.Remove(keys[i]);
            }
        }

        private static int KeepLine(Settlement castle, ItemObject item, float dailyProduction, CastleMarketTownsSettings settings)
        {
            float daily = Math.Max(dailyProduction, DailyConsumptionUnits(castle.Town, item));
            return (int)Math.Ceiling(daily * Math.Max(1, settings.CastleStockDays));
        }

        internal static int Reserve(Settlement castle, ItemObject item)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || castle == null || castle.Town == null ? 0 : KeepLine(castle, item, 0f, settings);
        }

        private static int StockCeiling(Settlement castle, Settlement marketTown, ItemObject item, float dailyProduction, CastleMarketTownsSettings settings)
        {
            int keep = KeepLine(castle, item, dailyProduction, settings);
            if (marketTown == null || marketTown.ItemRoster == null)
            {
                return keep;
            }

            int share = (int)(marketTown.ItemRoster.GetItemNumber(item) * (settings.CastleStockCeilingPercent / 100f));
            return Math.Max(keep, share);
        }

        private static float DailyConsumptionUnits(Town town, ItemObject item)
        {
            if (town == null || item == null || item.ItemCategory == null)
            {
                return 0f;
            }

            SettlementEconomyModel model = Campaign.Current.Models.SettlementEconomyModel;
            float demand = model.GetDailyDemandForCategory(town, item.ItemCategory, 0);
            float budget = CastleConsumptionBudget(model, town, demand, item.ItemCategory);
            int price = town.GetItemPrice(item, null, false);
            return price <= 0 ? 0f : budget / price;
        }

        internal static float CastleConsumptionBudget(SettlementEconomyModel model, Town town, float demand, ItemCategory category)
        {
            float budget = model.CalculateDailySettlementBudgetForItemCategory(town, demand, category);
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings != null && town != null && town.Settlement != null && town.Settlement.IsCastle && AppliesTo(town.Settlement))
            {
                budget *= settings.CastleConsumptionPercent / 100f;
            }

            return budget;
        }

        private void ExportCastleSurplus(Settlement castle, Settlement marketTown, Dictionary<ItemObject, float> produced, CastleMarketTownsSettings settings)
        {
            if (!CanTradeWith(castle, marketTown))
            {
                return;
            }

            foreach (KeyValuePair<ItemObject, float> pair in produced)
            {
                int surplus = castle.ItemRoster.GetItemNumber(pair.Key) - KeepLine(castle, pair.Key, pair.Value, settings);
                int sold = SellToTown(castle, marketTown, pair.Key, surplus);
                if (sold > 0)
                {
                    ChangeProducedStock(castle, pair.Key, -sold);
                }
            }
        }

        private static readonly Dictionary<string, float> StockRatios = new Dictionary<string, float>();

        internal static float StockRatio(Settlement castle)
        {
            float ratio;
            return castle != null && StockRatios.TryGetValue(castle.StringId, out ratio) ? ratio : 1f;
        }

        private static void UpdateStockRatio(Settlement castle, Settlement marketTown)
        {
            int townGoods = TradeGoodCount(marketTown);
            StockRatios[castle.StringId] = townGoods <= 0 ? 1f : TradeGoodCount(castle) / (float)townGoods;
        }

        private static int TradeGoodCount(Settlement settlement)
        {
            if (settlement == null || settlement.ItemRoster == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < settlement.ItemRoster.Count; i++)
            {
                ItemRosterElement element = settlement.ItemRoster.GetElementCopyAtIndex(i);
                if (element.EquipmentElement.Item != null && element.EquipmentElement.Item.IsTradeGood)
                {
                    count += element.Amount;
                }
            }

            return count;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (settlement == null || !settlement.IsCastle || settlement.Town == null)
            {
                return;
            }

            if (!AppliesTo(settlement))
            {
                SyncCastleDailyDefault(settlement);
                SyncCastleProjects(settlement);
                RemoveBrothel(settlement);
                return;
            }

            Prepare(settlement, false);
            RemoveBrothel(settlement);
        }

        private void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (settlement == null || !settlement.IsCastle)
            {
                return;
            }

            RemoveBrothel(settlement);
        }

        private void AddMenus(CampaignGameStarter starter)
        {
            if (_menusAdded)
            {
                return;
            }

            _menusAdded = true;
            starter.AddGameMenuOption("castle", "castle_market_towns_tavern_district", "{=cmtTavern01}Visit the castle tavern", TavernDistrictCondition, TavernDistrictConsequence, false, 4, false, null);
            starter.AddGameMenuOption("castle", "castle_market_towns_trade", "{=GmcgoiGy}Trade", TradeCondition, TradeConsequence, false, 5, false, null);
            starter.AddGameMenuOption("castle", "castle_market_towns_recruit", "{=E31IJyqs}Recruit troops", RecruitCondition, RecruitConsequence, false, 6, false, null);

            starter.AddGameMenu(TavernMenuId, "{=cmtTavern02}You are in the castle tavern. Off-duty soldiers, traders and travellers share the benches.", TavernMenuInit, GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(TavernMenuId, "castle_market_towns_tavern", "{=qcl3YTPh}Visit the tavern", TavernCondition, TavernConsequence, false, -1, false, null);
            AddHotButterOption(starter, "castle_market_towns_sell_body", "{=pompasellyourbody}Sell your body for {SELL_BODY_GOLD_AMOUNT}{GOLD_ICON}", "kendinisat");
            AddHotButterOption(starter, "castle_market_towns_fine_prostitute", "{=pompafinep}Get a fine prostitute ({GOLD_AMOUNT_EH}{GOLD_ICON})", "kendinekadar_eh");
            AddHotButterOption(starter, "castle_market_towns_cheap_prostitute", "{=pompacheapp}Get a cheap prostitute ({GOLD_AMOUNT_CHEAP}{GOLD_ICON})", "kendinekadar_ucuz");
            AddHotButterOption(starter, "castle_market_towns_troop_prostitutes", "{=pompagrup}Get prostitutes for your troops ({GOLD_AMOUNT_GRUP}{GOLD_ICON})", "grup");
            starter.AddGameMenuOption(TavernMenuId, "castle_market_towns_ransom_all", "{=xZIBKK0v}Ransom your prisoners ({RANSOM_AMOUNT}{GOLD_ICON})", args => RansomCondition(args, "SellPrisonersCondition"), RansomAllConsequence, false, -1, false, null);
            starter.AddGameMenuOption(TavernMenuId, "castle_market_towns_ransom_some", "{=Q8VN9UCq}Choose the prisoners to be ransomed", args => RansomCondition(args, "SellPrisonerOneStackCondition"), RansomSomeConsequence, false, -1, false, null);
            starter.AddGameMenuOption(TavernMenuId, "castle_market_towns_tavern_back", "{=cmtTavern03}Back to the castle", TavernBackCondition, TavernBackConsequence, true, -1, false, null);
        }

        private const string TavernMenuId = "castle_market_towns_tavern_menu";

        private static void AddHotButterOption(CampaignGameStarter starter, string id, string text, string handler)
        {
            starter.AddGameMenuOption(TavernMenuId, id, text,
                args => HotButterCondition(args, "pompa_on_condition_" + handler),
                args => HotButterConsequence(args, "pompa_on_consequence_" + handler),
                false, -1, false, null);
        }

        private static bool TavernDistrictCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return TavernAvailable(Settlement.CurrentSettlement);
        }

        private static void TavernDistrictConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu(TavernMenuId);
        }

        private static void TavernMenuInit(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            args.MenuTitle = new TextObject("{=DO7Vk4iw}Tavern");
            if (settlement == null)
            {
                return;
            }

            if (settlement.Culture != null)
            {
                args.MenuContext.SetBackgroundMeshName(settlement.Culture.StringId + "_tavern");
            }

            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/tavern");
            args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_tavern");

            GameMenuManager menuManager = Campaign.Current.GameMenuManager;
            Location tavern = settlement.LocationComplex == null ? null : settlement.LocationComplex.GetLocationWithId("tavern");
            menuManager.MenuLocations.Clear();
            if (tavern != null)
            {
                menuManager.MenuLocations.Add(tavern);
            }

            if (menuManager.NextLocation != null && PlayerEncounter.LocationEncounter != null)
            {
                string next = menuManager.NextLocation.StringId;
                PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(menuManager.NextLocation, menuManager.PreviousLocation, null, null);
                if (next != "tavern")
                {
                    menuManager.SetNextMenu("castle");
                }

                menuManager.NextLocation = null;
                menuManager.PreviousLocation = null;
            }
        }

        private static bool RansomCondition(MenuCallbackArgs args, string vanillaCondition)
        {
            if (!TavernAvailable(Settlement.CurrentSettlement))
            {
                return false;
            }

            try
            {
                MethodInfo condition = AccessTools.Method(typeof(PlayerTownVisitCampaignBehavior), vanillaCondition);
                object result = condition == null ? null : condition.Invoke(null, new object[] { args });
                return result is bool && (bool)result;
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not check prisoner ransom: " + exception.Message);
                return false;
            }
        }

        private static void RansomAllConsequence(MenuCallbackArgs args)
        {
            try
            {
                SellPrisonersAction.ApplyForSelectedPrisoners(PartyBase.MainParty, null, MobilePartyHelper.GetPlayerPrisonersPlayerCanSell());
            }
            catch (Exception exception)
            {
                InformationManager.DisplayMessage(new InformationMessage("Prisoners could not be ransomed. " + exception.Message));
            }

            GameMenu.SwitchToMenu(TavernMenuId);
        }

        private static void RansomSomeConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu(TavernMenuId);
            PartyScreenHelper.OpenScreenAsRansom();
        }

        private static bool TavernBackCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private static void TavernBackConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("castle");
        }

        internal static bool TavernAvailable(Settlement settlement)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !settings.Enabled || !settings.EnableTavern || settlement == null || !AppliesTo(settlement))
            {
                return false;
            }

            return settlement.LocationComplex != null && settlement.LocationComplex.GetLocationWithId("tavern") != null;
        }

        private static object HotButterBehavior()
        {
            if (Campaign.Current == null)
            {
                return null;
            }

            Type type = AccessTools.TypeByName("HotButter.PompaCampaignBehavior");
            if (type == null)
            {
                return null;
            }

            MethodInfo getter = typeof(Campaign).GetMethod("GetCampaignBehavior", Type.EmptyTypes);
            return getter == null ? null : getter.MakeGenericMethod(type).Invoke(Campaign.Current, null);
        }

        private static object CallHotButter(string method, MenuCallbackArgs args)
        {
            object behavior = HotButterBehavior();
            if (behavior == null)
            {
                return null;
            }

            MethodInfo target = behavior.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return target == null ? null : target.Invoke(behavior, new object[] { args });
        }

        private static bool HotButterCondition(MenuCallbackArgs args, string method)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !settings.HotButterTavernOptions || !TavernAvailable(Settlement.CurrentSettlement))
            {
                return false;
            }

            try
            {
                object result = CallHotButter(method, args);
                return result is bool && (bool)result;
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not ask Hot Butter about the tavern: " + exception.Message);
                return false;
            }
        }

        private static void HotButterConsequence(MenuCallbackArgs args, string method)
        {
            try
            {
                CallHotButter(method, args);
                if (TavernAvailable(Settlement.CurrentSettlement))
                {
                    GameMenu.SwitchToMenu(TavernMenuId);
                }
            }
            catch (Exception exception)
            {
                InformationManager.DisplayMessage(new InformationMessage("Hot Butter could not run in the castle tavern. " + exception.Message));
            }
        }

        private static bool TavernCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return TavernAvailable(Settlement.CurrentSettlement);
        }

        private static void TavernConsequence(MenuCallbackArgs args)
        {
            GameMenuManager menuManager = null;
            try
            {
                Settlement settlement = Settlement.CurrentSettlement;
                LocationEncounter encounter = PlayerEncounter.LocationEncounter;
                LocationComplex complex = settlement == null ? null : settlement.LocationComplex;
                Location center = complex == null ? null : complex.GetLocationWithId("center");
                Location tavern = complex == null ? null : complex.GetLocationWithId("tavern");
                if (encounter == null || center == null || tavern == null)
                {
                    throw new InvalidOperationException("The castle tavern location is not ready.");
                }

                menuManager = Campaign.Current.GameMenuManager;
                menuManager.PreviousLocation = center;
                menuManager.NextLocation = tavern;
                encounter.CreateAndOpenMissionController(tavern, null, null, null);
            }
            catch (Exception exception)
            {
                InformationManager.DisplayMessage(new InformationMessage("The castle tavern could not be opened. " + exception.Message));
            }
            finally
            {
                if (menuManager != null)
                {
                    menuManager.PreviousLocation = null;
                    menuManager.NextLocation = null;
                }
            }
        }

        private static bool TradeCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            Settlement settlement = Settlement.CurrentSettlement;
            if (settings == null || !settings.Enabled || settlement == null || !AppliesTo(settlement))
            {
                return false;
            }

            bool disabled = false;
            TextObject reason = TextObject.GetEmpty();
            bool canTrade = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(settlement, SettlementAccessModel.SettlementAction.Trade, out disabled, out reason);
            args.IsEnabled = canTrade && !disabled;
            if (!args.IsEnabled)
            {
                args.Tooltip = reason;
            }

            return true;
        }

        private static void TradeConsequence(MenuCallbackArgs args)
        {
            InventoryScreenHelper.ActivateTradeWithCurrentSettlement();
        }

        private static bool RecruitCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            Settlement settlement = Settlement.CurrentSettlement;
            if (!RecruitsAvailable(settlement))
            {
                return false;
            }

            bool disabled = false;
            TextObject reason = TextObject.GetEmpty();
            bool canRecruit = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(settlement, SettlementAccessModel.SettlementAction.RecruitTroops, out disabled, out reason);
            return MenuHelper.SetOptionProperties(args, canRecruit, disabled, reason);
        }

        private static void RecruitConsequence(MenuCallbackArgs args)
        {
            args.MenuContext.OpenRecruitVolunteers();
            args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_recruit");
        }

        internal static bool RecruitsAvailable(Settlement settlement)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings != null && settings.Enabled && settings.CastleRecruits && settlement != null && AppliesTo(settlement);
        }

        internal static bool RecruiterAllowed(MobileParty party)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null)
            {
                return true;
            }

            Clan clan = party == null ? null : party.ActualClan;
            Clan player = Clan.PlayerClan;
            if (clan != null && clan == player)
            {
                return true;
            }

            Kingdom playerKingdom = player != null ? player.Kingdom : null;
            if (clan != null && playerKingdom != null && clan.Kingdom == playerKingdom)
            {
                return settings.ApplyToKingdom;
            }

            return settings.ApplyToNpc;
        }

        private void Prepare(Settlement settlement, bool canMoveHeroes)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || settlement == null || settlement.Town == null)
            {
                return;
            }

            if (settings.EnableTavern)
            {
                Step("add the tavern to", settlement, delegate { EnsureTavern(settlement); });
            }
            else if (canMoveHeroes)
            {
                Step("remove the tavern from", settlement, delegate { RemoveTavern(settlement); });
            }

            if (canMoveHeroes)
            {
                Step("move notables out of", settlement, delegate { RelocateNotables(settlement, settings.MerchantNotables, settings.ArtisanNotables); });
                Step("add notables to", settlement, delegate { EnsureNotables(settlement, settings); });
                Step("resize the workshops of", settlement, delegate { ResizeWorkshops(settlement, HasWorkshopOwners(settlement) ? Clamp(settings.WorkshopCount, 0, 4) : 0); });
                Step("assign the workshops of", settlement, delegate { AssignWorkshops(settlement); });
            }

            Step("open the market of", settlement, delegate { EnsureMarket(settlement, settings); });
            Step("raise the prosperity of", settlement, delegate { EnsureProsperity(settlement, settings); });
            Step("set the daily defaults of", settlement, delegate { SyncCastleDailyDefault(settlement); });
            Step("set the projects of", settlement, delegate { SyncCastleProjects(settlement); });
            Step("remove the brothel from", settlement, delegate { RemoveBrothel(settlement); });
        }

        private void Cleanup(Settlement settlement)
        {
            Step("track the goods of", settlement, delegate { ClampLedgers(settlement); });
            Step("restore the daily defaults of", settlement, delegate { SyncCastleDailyDefault(settlement); });
            Step("restore the projects of", settlement, delegate { SyncCastleProjects(settlement); });
            Step("remove the brothel from", settlement, delegate { RemoveBrothel(settlement); });
            Step("remove the tavern from", settlement, delegate { RemoveTavern(settlement); });
            Step("remove the workshops of", settlement, delegate { ResizeWorkshops(settlement, 0); });
            Step("move notables out of", settlement, delegate { RelocateNotables(settlement, 0, 0); });
            Step("sell the leftover goods of", settlement, delegate { SellProducedLeftovers(settlement); });
            Step("restore the market of", settlement, delegate { RestoreMarket(settlement); });
            Step("restore the prosperity of", settlement, delegate { RestoreProsperity(settlement); });
            _prepared.Remove(settlement.StringId);
            _prepared.Remove("prosperity:" + settlement.StringId);
            StockRatios.Remove(settlement.StringId);
        }

        private static void Step(string action, Settlement settlement, Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not " + action + " " + settlement.Name + ": " + exception);
            }
        }

        private void EnsureTavern(Settlement settlement)
        {
            LocationComplex complex = settlement.LocationComplex;
            Location center = complex == null ? null : complex.GetLocationWithId("center");
            if (center == null)
            {
                return;
            }

            Location existing = complex.GetLocationWithId("tavern");
            if (existing != null)
            {
                if (TavernIsOurs(settlement))
                {
                    LinkLocations(center, existing);
                    LinkLocations(existing, center);
                }

                return;
            }

            string scene = TavernScene(settlement);
            TextObject name = new TextObject("{=DO7Vk4iw}Tavern");
            string[] scenes = new string[] { scene, scene, scene, scene };
            Location tavern = new Location("tavern", name, name, 30, true, false, "CanAlways", "CanAlways", "CanAlways", "CanIfGrownUpMaleOrHero", scenes, complex);
            Dictionary<string, Location> locations = (Dictionary<string, Location>)typeof(LocationComplex).GetField("_locations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(complex);
            locations["tavern"] = tavern;
            LinkLocations(center, tavern);
            LinkLocations(tavern, center);
            _createdTaverns.Add(settlement.StringId);
            _taverns[settlement.StringId] = tavern;
        }

        private bool TavernIsOurs(Settlement settlement)
        {
            string id = settlement.StringId;
            if (!_createdTaverns.Contains(id))
            {
                return false;
            }

            Location current = settlement.LocationComplex == null ? null : settlement.LocationComplex.GetLocationWithId("tavern");
            Location owned;
            if (!_taverns.TryGetValue(id, out owned))
            {
                owned = current;
                if (owned != null)
                {
                    _taverns[id] = owned;
                }
            }

            if (owned != null && owned == current)
            {
                return true;
            }

            _createdTaverns.Remove(id);
            _taverns.Remove(id);
            return false;
        }

        private static void LinkLocations(Location from, Location to)
        {
            if (from.LocationsOfPassages == null || !from.LocationsOfPassages.Contains(to))
            {
                from.AddPassageToLocation(to);
            }
        }

        private void RemoveTavern(Settlement settlement)
        {
            if (!_createdTaverns.Contains(settlement.StringId))
            {
                return;
            }

            LocationComplex complex = settlement.LocationComplex;
            if (complex == null)
            {
                return;
            }

            if (TaleWorlds.MountAndBlade.Mission.Current != null && Settlement.CurrentSettlement == settlement)
            {
                return;
            }

            Dictionary<string, Location> locations = typeof(LocationComplex).GetField("_locations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(complex) as Dictionary<string, Location>;
            Location tavern;
            if (locations == null || !locations.TryGetValue("tavern", out tavern) || !TavernIsOurs(settlement))
            {
                return;
            }

            DetachAndRemoveLocation(complex, locations, "tavern", tavern);
            _createdTaverns.Remove(settlement.StringId);
            _taverns.Remove(settlement.StringId);
        }

        private static string TavernScene(Settlement settlement)
        {
            string culture = settlement.Culture == null ? "" : settlement.Culture.StringId;
            if (culture == "vlandia") return "vlandia_tavern_interior_a";
            if (culture == "sturgia") return "sturgia_house_b_interior_tavern";
            if (culture == "aserai") return "aserai_tavern_interior";
            if (culture == "battania") return "battania_town_house_a_interior_a_tavern";
            if (culture == "khuzait") return "khuzait_tavern_a";
            if (culture == "nord") return "nord_tavern";
            return "empire_house_c_tavern_a";
        }

        private void EnsureNotables(Settlement settlement, CastleMarketTownsSettings settings)
        {
            int merchants = CountNotables(settlement, Occupation.Merchant);
            int artisans = CountNotables(settlement, Occupation.Artisan);
            int guard = 0;
            while (merchants < settings.MerchantNotables && guard < 8)
            {
                guard++;
                Hero hero = HeroCreator.CreateNotable(Occupation.Merchant, settlement);
                int updated = CountNotables(settlement, Occupation.Merchant);
                if (hero == null || updated <= merchants)
                {
                    break;
                }

                if (hero.Gold < settings.MerchantGold)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, hero, settings.MerchantGold - hero.Gold, true);
                }

                _createdNotables.Add(hero.StringId);
                merchants = updated;
            }

            guard = 0;
            while (artisans < settings.ArtisanNotables && guard < 8)
            {
                guard++;
                Hero hero = HeroCreator.CreateNotable(Occupation.Artisan, settlement);
                int updated = CountNotables(settlement, Occupation.Artisan);
                if (hero == null || updated <= artisans)
                {
                    break;
                }

                _createdNotables.Add(hero.StringId);
                artisans = updated;
            }
        }

        private static int CountNotables(Settlement settlement, Occupation occupation)
        {
            int count = 0;
            foreach (Hero hero in settlement.Notables)
            {
                if (hero != null && hero.IsAlive && hero.Occupation == occupation)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasWorkshopOwners(Settlement settlement)
        {
            foreach (Hero hero in settlement.Notables)
            {
                if (hero != null && hero.IsAlive && (hero.CurrentSettlement == settlement || hero.HomeSettlement == settlement) && (hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.Merchant))
                {
                    return true;
                }
            }

            return false;
        }

        private void RelocateNotables(Settlement settlement, int keepMerchants, int keepArtisans)
        {
            List<Hero> merchants = new List<Hero>();
            List<Hero> artisans = new List<Hero>();
            foreach (Hero hero in settlement.Notables)
            {
                if (hero == null || !hero.IsAlive || hero.CurrentSettlement != settlement || !_createdNotables.Contains(hero.StringId))
                {
                    continue;
                }

                if (hero.Occupation == Occupation.Merchant)
                {
                    merchants.Add(hero);
                }
                else if (hero.Occupation == Occupation.Artisan)
                {
                    artisans.Add(hero);
                }
            }

            merchants = WorkshopOwnersFirst(merchants, settlement);
            artisans = WorkshopOwnersFirst(artisans, settlement);
            List<Hero> moving = new List<Hero>();
            int merchantExcess = Math.Max(0, CountNotables(settlement, Occupation.Merchant) - Math.Max(0, keepMerchants));
            for (int i = merchants.Count - 1; i >= 0 && merchantExcess > 0; i--)
            {
                moving.Add(merchants[i]);
                merchantExcess--;
            }

            int artisanExcess = Math.Max(0, CountNotables(settlement, Occupation.Artisan) - Math.Max(0, keepArtisans));
            for (int i = artisans.Count - 1; i >= 0 && artisanExcess > 0; i--)
            {
                moving.Add(artisans[i]);
                artisanExcess--;
            }

            moving.RemoveAll(delegate(Hero hero)
            {
                return (hero.Issue != null && (hero.Issue.IsSolvingWithQuest || hero.Issue.IsOngoingWithoutQuest)) || hero.PartyBelongedTo != null || hero.IsPrisoner;
            });

            if (moving.Count == 0)
            {
                return;
            }

            Settlement target = NearestTown(settlement);
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < moving.Count; i++)
            {
                Hero hero = moving[i];
                HandOverWorkshops(hero, settlement, moving);
                TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, target);
                hero.UpdateHomeSettlement();
                _createdNotables.Remove(hero.StringId);
            }
        }

        private static List<Workshop> WorkshopsIn(Hero hero, Settlement settlement)
        {
            List<Workshop> found = new List<Workshop>();
            if (hero == null || hero.OwnedWorkshops == null)
            {
                return found;
            }

            foreach (Workshop workshop in hero.OwnedWorkshops)
            {
                if (workshop != null && workshop.Settlement == settlement)
                {
                    found.Add(workshop);
                }
            }

            return found;
        }

        private static List<Hero> WorkshopOwnersFirst(List<Hero> heroes, Settlement settlement)
        {
            List<Hero> ordered = new List<Hero>();
            List<Hero> free = new List<Hero>();
            foreach (Hero hero in heroes)
            {
                if (WorkshopsIn(hero, settlement).Count > 0)
                {
                    ordered.Add(hero);
                }
                else
                {
                    free.Add(hero);
                }
            }

            ordered.AddRange(free);
            return ordered;
        }

        private static void HandOverWorkshops(Hero hero, Settlement settlement, List<Hero> moving)
        {
            List<Workshop> workshops = WorkshopsIn(hero, settlement);
            for (int i = 0; i < workshops.Count; i++)
            {
                Hero heir = null;
                foreach (Hero notable in settlement.Notables)
                {
                    if (notable == null || !notable.IsAlive || notable == hero || moving.Contains(notable) || notable.CurrentSettlement != settlement)
                    {
                        continue;
                    }

                    if (heir == null || notable.OwnedWorkshops.Count < heir.OwnedWorkshops.Count)
                    {
                        heir = notable;
                    }
                }

                if (heir == null)
                {
                    return;
                }

                ChangeOwnerOfWorkshopAction.ApplyByDeath(workshops[i], heir);
            }
        }

        private static Settlement NearestTown(Settlement from)
        {
            return NearestTown(from, MobileParty.NavigationType.Default) ?? NearestTown(from, MobileParty.NavigationType.All);
        }

        private static Settlement NearestTown(Settlement from, MobileParty.NavigationType navigation)
        {
            Settlement best = null;
            Settlement bestSameCulture = null;
            float bestDistance = float.MaxValue;
            float bestSameCultureDistance = float.MaxValue;
            foreach (Town town in Town.AllTowns)
            {
                if (town == null || town.Settlement == null)
                {
                    continue;
                }

                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(from, town.Settlement, false, false, navigation);
                if (!(distance < UnreachableDistance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = town.Settlement;
                }

                if (from.Culture != null && town.Settlement.Culture == from.Culture && distance < bestSameCultureDistance)
                {
                    bestSameCultureDistance = distance;
                    bestSameCulture = town.Settlement;
                }
            }

            return bestSameCulture ?? best;
        }

        private static bool CanRemoveWorkshop(Workshop workshop)
        {
            if (workshop == null || string.IsNullOrEmpty(workshop.Tag) || !workshop.Tag.StartsWith(WorkshopTagPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (workshop.Owner == null)
            {
                return true;
            }

            return workshop.Owner.IsNotable && workshop.Owner.Clan != Clan.PlayerClan;
        }

        private static void DetachWorkshop(Workshop workshop)
        {
            if (workshop == null || workshop.Owner == null)
            {
                return;
            }

            FieldInfo field = typeof(Hero).GetField("_ownedWorkshops", BindingFlags.Instance | BindingFlags.NonPublic);
            MBList<Workshop> owned = field.GetValue(workshop.Owner) as MBList<Workshop>;
            if (owned != null)
            {
                owned.Remove(workshop);
            }
        }

        private static void ResizeWorkshops(Settlement settlement, int count)
        {
            Town town = settlement.Town;
            Workshop[] current = town.Workshops ?? new Workshop[0];
            List<Workshop> kept = new List<Workshop>(current);

            for (int i = kept.Count - 1; i >= 0 && kept.Count > count; i--)
            {
                if (CanRemoveWorkshop(kept[i]))
                {
                    DetachWorkshop(kept[i]);
                    kept.RemoveAt(i);
                }
            }

            int index = 0;
            while (kept.Count < count)
            {
                string tag = WorkshopTagPrefix + index;
                index++;
                bool used = false;
                for (int i = 0; i < kept.Count; i++)
                {
                    if (kept[i] != null && kept[i].Tag == tag)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    kept.Add(new Workshop(settlement, tag));
                }
            }

            if (kept.Count == current.Length)
            {
                return;
            }

            MethodInfo setter = typeof(Town).GetProperty("Workshops", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true);
            setter.Invoke(town, new object[] { kept.ToArray() });
        }

        private static void AssignWorkshops(Settlement settlement)
        {
            Town town = settlement.Town;
            if (town.Workshops == null || town.Workshops.Length == 0)
            {
                return;
            }

            List<WorkshopType> types = new List<WorkshopType>();
            foreach (WorkshopType type in WorkshopType.All)
            {
                if (type != null && !type.IsHidden)
                {
                    types.Add(type);
                }
            }

            types.Sort(delegate(WorkshopType left, WorkshopType right)
            {
                return right.Frequency.CompareTo(left.Frequency);
            });

            List<Hero> owners = new List<Hero>();
            foreach (Hero hero in settlement.Notables)
            {
                if (hero != null && hero.IsAlive && (hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.Merchant))
                {
                    owners.Add(hero);
                }
            }

            if (owners.Count == 0 || types.Count == 0)
            {
                return;
            }

            for (int i = 0; i < town.Workshops.Length && i < types.Count; i++)
            {
                Workshop workshop = town.Workshops[i];
                if (workshop == null || workshop.WorkshopType != null)
                {
                    continue;
                }

                workshop.InitializeWorkshop(owners[i % owners.Count], types[i]);
            }
        }

        private void EnsureMarket(Settlement settlement, CastleMarketTownsSettings settings)
        {
            if (_prepared.Contains(settlement.StringId))
            {
                return;
            }

            Town town = settlement.Town;
            TownMarketData market = town.MarketData;
            if (market == null)
            {
                market = new TownMarketData(town);
                MarketDataField.SetValue(town, market);
                _createdMarketData.Add(settlement.StringId);
                _markets[settlement.StringId] = market;
            }

            float scale = settings.MarketPercent / 100f;
            if (scale < 0.1f)
            {
                scale = 0.1f;
            }

            HashSet<ItemCategory> seen = new HashSet<ItemCategory>();
            foreach (ItemObject item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (item == null || !item.IsTradeGood || item.ItemCategory == null || !seen.Add(item.ItemCategory))
                {
                    continue;
                }

                string snapshotKey = MarketKey(settlement.StringId, item.ItemCategory.StringId);
                if (!_originalMarketSupply.ContainsKey(snapshotKey))
                {
                    _originalMarketSupply[snapshotKey] = market.GetSupply(item.ItemCategory);
                    _originalMarketDemand[snapshotKey] = market.GetDemand(item.ItemCategory);
                }

                market.AddDemand(item.ItemCategory, 3f * scale);
                market.AddSupply(item.ItemCategory, 2f * scale);
            }

            SeedMarketStock(settlement, scale);
            market.UpdateStores();
            _prepared.Add(settlement.StringId);
        }

        private void EnsureProsperity(Settlement settlement, CastleMarketTownsSettings settings)
        {
            if (settings.ProsperityFloor <= 0 || _prepared.Contains("prosperity:" + settlement.StringId))
            {
                return;
            }

            if (settlement.Town.Prosperity < settings.ProsperityFloor)
            {
                _prosperityGrants[settlement.StringId] = settings.ProsperityFloor - settlement.Town.Prosperity;
                settlement.Town.Prosperity = settings.ProsperityFloor;
            }

            _prepared.Add("prosperity:" + settlement.StringId);
        }

        private static void RemoveBrothel(Settlement settlement)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if ((settings != null && !settings.BlockBrothels) || settlement.LocationComplex == null)
            {
                return;
            }

            FieldInfo field = typeof(LocationComplex).GetField("_locations", BindingFlags.Instance | BindingFlags.NonPublic);
            Dictionary<string, Location> locations = field.GetValue(settlement.LocationComplex) as Dictionary<string, Location>;
            Location brothel;
            if (locations != null && locations.TryGetValue("brothel", out brothel))
            {
                DetachAndRemoveLocation(settlement.LocationComplex, locations, "brothel", brothel);
            }
        }

        private void SeedMarketStock(Settlement settlement, float scale)
        {
            Settlement source = NearestTown(settlement);
            if (source == null || source.ItemRoster == null || settlement.ItemRoster == null)
            {
                return;
            }

            for (int i = 0; i < source.ItemRoster.Count; i++)
            {
                ItemRosterElement element = source.ItemRoster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || !item.IsTradeGood || element.Amount <= 0)
                {
                    continue;
                }

                int target = (int)Math.Round(element.Amount * scale);
                if (target <= 0)
                {
                    continue;
                }

                int current = settlement.ItemRoster.GetItemNumber(item);
                int add = target - current;
                if (add <= 0)
                {
                    continue;
                }

                settlement.ItemRoster.AddToCounts(item, add);
                _stockGrants[MarketKey(settlement.StringId, item.StringId)] = add;
            }
        }

        private void RestoreMarket(Settlement settlement)
        {
            List<string> remove = new List<string>();
            foreach (KeyValuePair<string, int> pair in _stockGrants)
            {
                string settlementId;
                string itemId;
                if (!SplitMarketKey(pair.Key, out settlementId, out itemId) || settlementId != settlement.StringId)
                {
                    continue;
                }

                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (item != null && settlement.ItemRoster != null)
                {
                    int available = settlement.ItemRoster.GetItemNumber(item);
                    settlement.ItemRoster.AddToCounts(item, -Math.Min(available, pair.Value));
                }

                remove.Add(pair.Key);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                _stockGrants.Remove(remove[i]);
            }

            bool claimedMarket = _createdMarketData.Contains(settlement.StringId);
            bool ownMarket = MarketIsOurs(settlement);
            TownMarketData market = claimedMarket && !ownMarket ? null : settlement.Town.MarketData;
            remove.Clear();
            foreach (KeyValuePair<string, float> pair in _originalMarketSupply)
            {
                string settlementId;
                string categoryId;
                if (!SplitMarketKey(pair.Key, out settlementId, out categoryId) || settlementId != settlement.StringId)
                {
                    continue;
                }

                ItemCategory category = MBObjectManager.Instance.GetObject<ItemCategory>(categoryId);
                float demand;
                if (market != null && category != null && _originalMarketDemand.TryGetValue(pair.Key, out demand))
                {
                    market.SetSupplyDemand(category, pair.Value, demand);
                }

                remove.Add(pair.Key);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                _originalMarketSupply.Remove(remove[i]);
                _originalMarketDemand.Remove(remove[i]);
            }

            if (ownMarket)
            {
                _createdMarketData.Remove(settlement.StringId);
                _markets.Remove(settlement.StringId);
                MarketDataField.SetValue(settlement.Town, null);
            }
            else if (market != null)
            {
                market.UpdateStores();
            }
        }

        private static readonly FieldInfo MarketDataField = typeof(Town).GetField("_marketData", BindingFlags.Instance | BindingFlags.NonPublic);

        private bool MarketIsOurs(Settlement settlement)
        {
            string id = settlement.StringId;
            if (!_createdMarketData.Contains(id))
            {
                return false;
            }

            TownMarketData current = MarketDataField.GetValue(settlement.Town) as TownMarketData;
            TownMarketData owned;
            if (!_markets.TryGetValue(id, out owned))
            {
                owned = current;
                if (owned != null)
                {
                    _markets[id] = owned;
                }
            }

            if (owned != null && owned == current)
            {
                return true;
            }

            _createdMarketData.Remove(id);
            _markets.Remove(id);
            return false;
        }

        private void RestoreProsperity(Settlement settlement)
        {
            float grant;
            if (!_prosperityGrants.TryGetValue(settlement.StringId, out grant))
            {
                return;
            }

            settlement.Town.Prosperity = Math.Max(0f, settlement.Town.Prosperity - grant);
            _prosperityGrants.Remove(settlement.StringId);
        }

        private static void DetachAndRemoveLocation(LocationComplex complex, Dictionary<string, Location> locations, string id, Location target)
        {
            if (complex == null || locations == null || target == null)
            {
                return;
            }

            List<Location> existing = new List<Location>(locations.Values);
            for (int i = 0; i < existing.Count; i++)
            {
                Location location = existing[i];
                object passages = location == null ? null : location.LocationsOfPassages;
                if (passages == null)
                {
                    continue;
                }

                MethodInfo remove = passages.GetType().GetMethod("Remove", new Type[] { typeof(Location) });
                if (remove != null)
                {
                    remove.Invoke(passages, new object[] { target });
                }
            }

            locations.Remove(id);
        }

        private static void RunOfficialTownDailyTicks(Town town)
        {
            RunOfficialTownDailyTick(typeof(ItemConsumptionBehavior), town);
            RunOfficialTownDailyTick(typeof(RecruitmentCampaignBehavior), town);
            RunOfficialTownDailyTick(typeof(TradeCampaignBehavior), town);
            RunOfficialTownDailyTick(typeof(WorkshopsCampaignBehavior), town);
        }

        private static void RunOfficialTownDailyTick(Type behaviorType, Town town)
        {
            try
            {
                MethodInfo getter = typeof(Campaign).GetMethod("GetCampaignBehavior", Type.EmptyTypes);
                object behavior = getter == null ? null : getter.MakeGenericMethod(behaviorType).Invoke(Campaign.Current, null);
                MethodInfo tick = behaviorType.GetMethod("DailyTickTown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (behavior != null && tick != null)
                {
                    tick.Invoke(behavior, new object[] { town });
                }
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not run " + behaviorType.Name + " for " + town.Name + ": " + exception.Message);
            }
        }

        internal static bool AppliesTo(Settlement settlement)
        {
            if (settlement == null || !settlement.IsCastle)
            {
                return false;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings != null && !settings.Enabled)
            {
                return false;
            }

            Clan owner = settlement.OwnerClan;
            Clan player = Clan.PlayerClan;
            if (settings == null || (owner != null && owner == player))
            {
                return true;
            }

            Kingdom playerKingdom = player != null ? player.Kingdom : null;
            if (playerKingdom != null && owner != null && owner.Kingdom == playerKingdom)
            {
                return settings.ApplyToKingdom;
            }

            return settings.ApplyToNpc;
        }

        internal static bool CastleProjectsOverridden(Settlement settlement)
        {
            if (!AppliesTo(settlement))
            {
                return false;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || settings.OverrideCastleProjects;
        }

        private static void MigrateLegacyMarketplace(Town town)
        {
            if (town.Buildings == null)
            {
                return;
            }

            BuildingType marketplace = DefaultBuildingTypes.SettlementMarketplace;
            if (marketplace == null)
            {
                return;
            }

            FieldInfo typeField = typeof(Building).GetField("_buildingType", BindingFlags.Instance | BindingFlags.NonPublic);
            bool hasVanilla = false;
            foreach (Building building in town.Buildings)
            {
                if (building != null && building.BuildingType == marketplace)
                {
                    hasVanilla = true;
                    break;
                }
            }

            List<Building> legacy = new List<Building>();
            foreach (Building building in town.Buildings)
            {
                if (building != null && building.BuildingType != null && building.BuildingType.StringId == LegacyMarketplaceId)
                {
                    legacy.Add(building);
                }
            }

            for (int i = 0; i < legacy.Count; i++)
            {
                if (!hasVanilla && typeField != null)
                {
                    typeField.SetValue(legacy[i], marketplace);
                    hasVanilla = true;
                    OwnedMarketplaces.Add(town.Settlement.StringId);
                    MarketplaceBuildings[town.Settlement.StringId] = legacy[i];
                }
                else
                {
                    town.Buildings.Remove(legacy[i]);
                    RemoveFromQueue(town, legacy[i]);
                }
            }
        }

        private static void UnregisterLegacyMarketplace()
        {
            try
            {
                if (Game.Current == null || Game.Current.ObjectManager == null)
                {
                    return;
                }

                BuildingType legacy = Game.Current.ObjectManager.GetObject<BuildingType>(LegacyMarketplaceId);
                if (legacy != null)
                {
                    Game.Current.ObjectManager.UnregisterObject(legacy);
                }
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not remove the old castle marketplace type: " + exception.Message);
            }
        }

        internal static void SyncCastleProjects(Settlement settlement)
        {
            if (settlement == null || !settlement.IsCastle || settlement.Town == null || settlement.Town.Buildings == null)
            {
                return;
            }

            BuildingType marketplace = DefaultBuildingTypes.SettlementMarketplace;
            if (marketplace == null)
            {
                return;
            }

            if (CastleProjectsOverridden(settlement))
            {
                EnsureProject(settlement, marketplace);
            }
            else
            {
                RemoveProject(settlement, marketplace);
            }
        }

        internal static bool OwnsCastleMarketplace(Building building)
        {
            return building != null && building.Town != null && building.Town.Settlement != null && OwnedMarketplace(building.Town.Settlement) == building;
        }

        private static Building OwnedMarketplace(Settlement settlement)
        {
            string id = settlement.StringId;
            if (!OwnedMarketplaces.Contains(id) || settlement.Town == null || settlement.Town.Buildings == null)
            {
                return null;
            }

            BuildingType marketplace = DefaultBuildingTypes.SettlementMarketplace;
            Building owned;
            if (!MarketplaceBuildings.TryGetValue(id, out owned))
            {
                owned = FirstBuilding(settlement.Town, marketplace);
                if (owned != null)
                {
                    MarketplaceBuildings[id] = owned;
                }
            }

            if (owned != null && owned.BuildingType == marketplace && settlement.Town.Buildings.Contains(owned))
            {
                return owned;
            }

            OwnedMarketplaces.Remove(id);
            MarketplaceBuildings.Remove(id);
            return null;
        }

        private static Building FirstBuilding(Town town, BuildingType buildingType)
        {
            if (town == null || town.Buildings == null || buildingType == null)
            {
                return null;
            }

            foreach (Building building in town.Buildings)
            {
                if (building != null && building.BuildingType == buildingType)
                {
                    return building;
                }
            }

            return null;
        }

        private static void EnsureProject(Settlement settlement, BuildingType buildingType)
        {
            if (OwnedMarketplace(settlement) != null || FirstBuilding(settlement.Town, buildingType) != null)
            {
                return;
            }

            Town town = settlement.Town;
            Building building = new Building(buildingType, town, 0f, buildingType.StartLevel);
            town.Buildings.Add(building);
            OwnedMarketplaces.Add(settlement.StringId);
            MarketplaceBuildings[settlement.StringId] = building;
        }

        private static void RemoveProject(Settlement settlement, BuildingType buildingType)
        {
            Building owned = OwnedMarketplace(settlement);
            if (owned == null)
            {
                return;
            }

            Town town = settlement.Town;
            town.Buildings.Remove(owned);
            RemoveFromQueue(town, owned);
            OwnedMarketplaces.Remove(settlement.StringId);
            MarketplaceBuildings.Remove(settlement.StringId);
        }

        private static void RemoveFromQueue(Town town, Building building)
        {
            Queue<Building> queue = town.BuildingsInProgress as Queue<Building>;
            if (queue == null || !queue.Contains(building))
            {
                return;
            }

            Building[] pending = queue.ToArray();
            queue.Clear();
            for (int i = 0; i < pending.Length; i++)
            {
                if (pending[i] != null && pending[i] != building)
                {
                    queue.Enqueue(pending[i]);
                }
            }
        }

        internal static bool DailyDefaultsOverridden(Settlement settlement)
        {
            if (!AppliesTo(settlement))
            {
                return false;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings == null || settings.ReplaceSlackenWithFestival;
        }

        internal static void SyncCastleDailyDefault(Settlement settlement)
        {
            if (settlement == null || !settlement.IsCastle || settlement.Town == null || settlement.Town.Buildings == null)
            {
                return;
            }

            bool overridden = DailyDefaultsOverridden(settlement);
            if (overridden)
            {
                OwnedDailyDefaults.Add(settlement.StringId);
            }
            else if (!OwnedDailyDefaults.Remove(settlement.StringId))
            {
                return;
            }

            SwapDaily(settlement, FestivalRole, overridden, DefaultBuildingTypes.CastleDailySlackenGarrison, DefaultBuildingTypes.SettlementDailyFestivalAndGames);
            SwapDaily(settlement, IrrigationRole, overridden, DefaultBuildingTypes.CastleDailyIrrigation, DefaultBuildingTypes.SettlementDailyIrrigation);
        }

        private static void SwapDaily(Settlement settlement, string role, bool overridden, BuildingType castleType, BuildingType townType)
        {
            string key = MarketKey(settlement.StringId, role);
            if (overridden)
            {
                OwnedDailyBuilding(settlement, key, townType);
                Building created = SwapDailyDefault(settlement.Town, castleType, townType, null, true);
                if (created != null)
                {
                    CreatedDailyBuildings.Add(key);
                    DailyBuildings[key] = created;
                }

                return;
            }

            SwapDailyDefault(settlement.Town, townType, castleType, OwnedDailyBuilding(settlement, key, townType), false);
            CreatedDailyBuildings.Remove(key);
            DailyBuildings.Remove(key);
        }

        private static Building OwnedDailyBuilding(Settlement settlement, string key, BuildingType buildingType)
        {
            if (!CreatedDailyBuildings.Contains(key))
            {
                return null;
            }

            Building owned;
            if (!DailyBuildings.TryGetValue(key, out owned))
            {
                owned = FirstBuilding(settlement.Town, buildingType);
                if (owned != null)
                {
                    DailyBuildings[key] = owned;
                }
            }

            if (owned != null && owned.BuildingType == buildingType && settlement.Town.Buildings.Contains(owned))
            {
                return owned;
            }

            CreatedDailyBuildings.Remove(key);
            DailyBuildings.Remove(key);
            return null;
        }

        private static Building SwapDailyDefault(Town town, BuildingType removeType, BuildingType keepType, Building ownedRemove, bool removeAny)
        {
            if (town == null || removeType == null || keepType == null)
            {
                return null;
            }

            Building remove = null;
            Building keep = null;
            foreach (Building building in town.Buildings)
            {
                if (building == null || building.BuildingType == null)
                {
                    continue;
                }

                if (building.BuildingType == removeType && (removeAny || building == ownedRemove))
                {
                    remove = building;
                }

                if (building.BuildingType == keepType)
                {
                    keep = building;
                }
            }

            Building created = null;
            if (keep == null)
            {
                keep = new Building(keepType, town, 0f, keepType.StartLevel);
                town.Buildings.Add(keep);
                created = keep;
            }

            if (remove == null)
            {
                return created;
            }

            if (remove.IsCurrentlyDefault)
            {
                remove.IsCurrentlyDefault = false;
                keep.IsCurrentlyDefault = true;
            }

            town.Buildings.Remove(remove);
            return created;
        }

        private void ValidateOwnership()
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !settlement.IsCastle || settlement.Town == null)
                {
                    continue;
                }

                TavernIsOurs(settlement);
                MarketIsOurs(settlement);
                OwnedMarketplace(settlement);
                if (settlement.Town.Buildings != null)
                {
                    OwnedDailyBuilding(settlement, MarketKey(settlement.StringId, FestivalRole), DefaultBuildingTypes.SettlementDailyFestivalAndGames);
                    OwnedDailyBuilding(settlement, MarketKey(settlement.StringId, IrrigationRole), DefaultBuildingTypes.SettlementDailyIrrigation);
                }
            }
        }

        private string SerializeMarketSnapshots()
        {
            List<string> values = new List<string>();
            HashSet<string> zeroCastles = new HashSet<string>(_zeroSnapshotCastles);
            foreach (KeyValuePair<string, float> pair in _originalMarketSupply)
            {
                float demand;
                if (!_originalMarketDemand.TryGetValue(pair.Key, out demand))
                {
                    continue;
                }

                string settlementId;
                string categoryId;
                if (pair.Value == 0f && demand == 0f && SplitMarketKey(pair.Key, out settlementId, out categoryId))
                {
                    zeroCastles.Add(settlementId);
                    continue;
                }

                values.Add(pair.Key + "|" + pair.Value.ToString("R", CultureInfo.InvariantCulture) + "|" + demand.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (string settlementId in zeroCastles)
            {
                values.Add(MarketKey(settlementId, ZeroSnapshotMarker) + "|0|0");
            }

            return string.Join(";", values.ToArray());
        }

        private void LoadMarketSnapshots(string data)
        {
            _originalMarketSupply.Clear();
            _originalMarketDemand.Clear();
            _zeroSnapshotCastles.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split('|');
                float supply;
                float demand;
                if (parts.Length == 4
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out supply)
                    && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out demand))
                {
                    if (parts[1] == ZeroSnapshotMarker)
                    {
                        _zeroSnapshotCastles.Add(parts[0]);
                        continue;
                    }

                    string key = MarketKey(parts[0], parts[1]);
                    _originalMarketSupply[key] = supply;
                    _originalMarketDemand[key] = demand;
                }
            }
        }

        private void ExpandZeroSnapshots()
        {
            if (_zeroSnapshotCastles.Count == 0 || MBObjectManager.Instance == null)
            {
                return;
            }

            HashSet<ItemCategory> seen = new HashSet<ItemCategory>();
            foreach (ItemObject item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (item == null || !item.IsTradeGood || item.ItemCategory == null || !seen.Add(item.ItemCategory))
                {
                    continue;
                }

                foreach (string settlementId in _zeroSnapshotCastles)
                {
                    string key = MarketKey(settlementId, item.ItemCategory.StringId);
                    if (!_originalMarketSupply.ContainsKey(key))
                    {
                        _originalMarketSupply[key] = 0f;
                        _originalMarketDemand[key] = 0f;
                    }
                }
            }

            _zeroSnapshotCastles.Clear();
        }

        private static string SyncChunked(IDataStore dataStore, string key, string value)
        {
            if (dataStore.IsSaving)
            {
                List<string> chunks = SplitForSave(value ?? "");
                string first = chunks[0];
                dataStore.SyncData(key, ref first);
                if (chunks.Count > 1)
                {
                    string count = chunks.Count.ToString(CultureInfo.InvariantCulture);
                    dataStore.SyncData(key + "_Parts", ref count);
                    for (int i = 1; i < chunks.Count; i++)
                    {
                        string chunk = chunks[i];
                        dataStore.SyncData(key + "_Part" + i.ToString(CultureInfo.InvariantCulture), ref chunk);
                    }
                }

                return value;
            }

            string head = null;
            dataStore.SyncData(key, ref head);
            string partCount = null;
            dataStore.SyncData(key + "_Parts", ref partCount);
            int total;
            if (string.IsNullOrEmpty(partCount) || !int.TryParse(partCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out total) || total <= 1)
            {
                return head ?? "";
            }

            StringBuilder builder = new StringBuilder(head ?? "");
            for (int i = 1; i < total; i++)
            {
                string chunk = null;
                dataStore.SyncData(key + "_Part" + i.ToString(CultureInfo.InvariantCulture), ref chunk);
                builder.Append(chunk);
            }

            return builder.ToString();
        }

        private static List<string> SplitForSave(string value)
        {
            List<string> chunks = new List<string>();
            int start = 0;
            while (start < value.Length)
            {
                int length = Math.Min(SaveChunkLength, value.Length - start);
                if (start + length < value.Length && char.IsHighSurrogate(value[start + length - 1]))
                {
                    length--;
                }

                chunks.Add(value.Substring(start, length));
                start += length;
            }

            if (chunks.Count == 0)
            {
                chunks.Add("");
            }

            return chunks;
        }

        private static string SerializeSet(HashSet<string> values)
        {
            return string.Join(",", ToArray(values));
        }

        private static void LoadSet(string data, HashSet<string> values)
        {
            values.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] parts = data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                values.Add(parts[i]);
            }
        }

        private static string SerializeFloatMap(Dictionary<string, float> values)
        {
            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, float> pair in values)
            {
                entries.Add(pair.Key + "=" + pair.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            return string.Join(";", entries.ToArray());
        }

        private static void LoadFloatMap(string data, Dictionary<string, float> values)
        {
            values.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                int separator = entries[i].LastIndexOf('=');
                float value;
                if (separator > 0 && float.TryParse(entries[i].Substring(separator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    values[entries[i].Substring(0, separator)] = value;
                }
            }
        }

        private static string SerializeIntMap(Dictionary<string, int> values)
        {
            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, int> pair in values)
            {
                entries.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join(";", entries.ToArray());
        }

        private static void LoadIntMap(string data, Dictionary<string, int> values)
        {
            values.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                int separator = entries[i].LastIndexOf('=');
                int value;
                if (separator > 0 && int.TryParse(entries[i].Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    values[entries[i].Substring(0, separator)] = value;
                }
            }
        }

        private static string MarketKey(string settlementId, string objectId)
        {
            return settlementId + "|" + objectId;
        }

        private static bool SplitMarketKey(string key, out string settlementId, out string objectId)
        {
            int separator = string.IsNullOrEmpty(key) ? -1 : key.IndexOf('|');
            if (separator <= 0 || separator >= key.Length - 1)
            {
                settlementId = null;
                objectId = null;
                return false;
            }

            settlementId = key.Substring(0, separator);
            objectId = key.Substring(separator + 1);
            return true;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static string[] ToArray(HashSet<string> values)
        {
            string[] array = new string[values.Count];
            values.CopyTo(array);
            return array;
        }
    }
}
