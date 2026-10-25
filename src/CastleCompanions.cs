using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    // The base game creates and swaps unhired companions only in towns. Apart from a one-time seed, companions are only sent to
    // castle taverns and back with the same leave and enter actions the base game's companion swap uses.
    internal static class CastleCompanions
    {
        private const float CastleShare = 1f / 3f;
        private const int NearbyChoices = 3;
        private const float UnreachableDistance = 1e20f;

        // Only companions the mod sent to a castle are ever sent back, so companions other mods place in castles stay where they are.
        internal static readonly HashSet<string> Sent = new HashSet<string>();

        // Campaign day of the last move this mod made for a companion, for the minimum stay.
        internal static readonly Dictionary<string, float> LastMoved = new Dictionary<string, float>();

        internal static bool Hosts(Settlement settlement)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            return settings != null && settings.CastleCompanions && settlement != null && settlement.IsCastle && CastleMarketTownsBehavior.TavernAvailable(settlement);
        }

        // Seed a bounded castle population once per campaign. The active companion system still chooses the unused templates and
        // creates the heroes; the mod only supplies distinct eligible castles as their first settlements.
        internal static bool TryInitialSeed()
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !settings.Enabled || !settings.CastleCompanions || Campaign.Current == null)
            {
                return false;
            }

            List<Settlement> emptyCastles = new List<Settlement>();
            int occupiedCastles = 0;
            int eligibleCastles = 0;
            Settlement playerSettlement = Hero.MainHero == null ? null : Hero.MainHero.CurrentSettlement;
            foreach (Town castle in Town.AllCastles)
            {
                Settlement settlement = castle == null ? null : castle.Settlement;
                if (settlement == null || settlement == playerSettlement || settlement.IsUnderSiege || !Hosts(settlement))
                {
                    continue;
                }

                eligibleCastles++;
                if (HasFreeCompanion(settlement))
                {
                    occupiedCastles++;
                }
                else
                {
                    emptyCastles.Add(settlement);
                }
            }

            if (emptyCastles.Count == 0)
            {
                return eligibleCastles > 0;
            }

            // Counted the same way as the daily move cap, so seeding k companions keeps castles within a third of the free pool:
            // (inCastles + k) <= (free + k) / 3.
            int free = 0;
            int inCastles = 0;
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (!IsFree(hero))
                {
                    continue;
                }

                if (hero.CurrentSettlement.IsTown)
                {
                    if (CanJoinCastles(hero))
                    {
                        free++;
                    }
                }
                else if (hero.CurrentSettlement.IsCastle && Sent.Contains(hero.StringId))
                {
                    free++;
                    inCastles++;
                }
            }

            int target = MathF.Floor(Town.AllTowns.Count * 0.6f * CastleShare);
            int poolLimit = Math.Max(0, (free - 3 * inCastles) / 2);
            int remaining = Math.Min(Math.Min(Math.Max(0, target - occupiedCastles), poolLimit), emptyCastles.Count);
            if (remaining == 0)
            {
                return true;
            }

            object creator;
            MethodInfo create;
            if (!FindCompanionCreator(out creator, out create))
            {
                Debug.Print("Castle Market Towns found no active companion system that can create companions; castle taverns are not seeded and fill only as companions move from towns.");
                return true;
            }

            int createdCount = 0;
            while (remaining > 0 && emptyCastles.Count > 0)
            {
                int castleIndex = MBRandom.RandomInt(emptyCastles.Count);
                Settlement destination = emptyCastles[castleIndex];
                emptyCastles.RemoveAt(castleIndex);

                HashSet<Hero> before = new HashSet<Hero>(Hero.AllAliveHeroes);
                create.Invoke(creator, new object[] { destination });
                Hero created = null;
                foreach (Hero hero in Hero.AllAliveHeroes)
                {
                    if (!before.Contains(hero) && hero.IsWanderer && hero.CompanionOf == null)
                    {
                        created = hero;
                        break;
                    }
                }

                // No unused template remains. Mark seeding complete instead of retrying and polluting saves.
                if (created == null)
                {
                    break;
                }

                // A replacement companion system made a child or a clan member. It goes to a town instead, and seeding stops
                // rather than create more of them.
                if (!CanJoinCastles(created))
                {
                    if (created.CurrentSettlement == destination)
                    {
                        Settlement town = NearbyTown(destination, playerSettlement) ?? CastleMarketTownsBehavior.NearestTown(destination);
                        if (town != null)
                        {
                            Move(created, town);
                        }
                    }

                    Debug.Print("Castle Market Towns stopped seeding castle taverns because " + creator.GetType().Name + " created " + created.Name + ", who is a child or belongs to a clan.");
                    break;
                }

                createdCount++;
                remaining--;
                if (created.CurrentSettlement == destination)
                {
                    Sent.Add(created.StringId);
                    LastMoved[created.StringId] = Today();
                }
            }

            Debug.Print("Castle Market Towns initially seeded " + createdCount + " companion(s) in castle taverns using " + creator.GetType().Name + ".");
            return true;
        }

        // Companion overhaul mods may remove the base game's companion behavior and register a copy with the same creation
        // method, so the creator is matched by method rather than by type. The base game's behavior wins when it is still active.
        private static bool FindCompanionCreator(out object creator, out MethodInfo create)
        {
            creator = null;
            create = null;
            foreach (CampaignBehaviorBase behavior in Campaign.Current.GetCampaignBehaviors<CampaignBehaviorBase>())
            {
                if (behavior == null)
                {
                    continue;
                }

                MethodInfo method = AccessTools.Method(behavior.GetType(), "CreateCompanionAndAddToSettlement", new Type[] { typeof(Settlement) });
                if (method == null || method.IsStatic)
                {
                    continue;
                }

                if (behavior is CompanionsCampaignBehavior)
                {
                    creator = behavior;
                    create = method;
                    return true;
                }

                if (creator == null)
                {
                    creator = behavior;
                    create = method;
                }
            }

            return creator != null;
        }

        internal static void DailyTick()
        {
            Settlement playerSettlement = Hero.MainHero == null ? null : Hero.MainHero.CurrentSettlement;
            float today = Today();
            List<Hero> inTowns = new List<Hero>();
            List<Hero> inCastles = new List<Hero>();
            HashSet<string> stillSent = new HashSet<string>();
            HashSet<string> unhired = new HashSet<string>();
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero != null && hero.IsWanderer && hero.CompanionOf == null)
                {
                    unhired.Add(hero.StringId);
                }

                bool sentHere = hero != null && hero.IsWanderer && hero.CompanionOf == null && hero.CurrentSettlement != null && hero.CurrentSettlement.IsCastle && Sent.Contains(hero.StringId);
                if (sentHere)
                {
                    stillSent.Add(hero.StringId);
                }

                if (!IsFree(hero))
                {
                    continue;
                }

                if (hero.CurrentSettlement.IsTown)
                {
                    if (CanJoinCastles(hero))
                    {
                        inTowns.Add(hero);
                    }
                }
                else if (sentHere)
                {
                    inCastles.Add(hero);
                }
            }

            Sent.IntersectWith(stillSent);
            PruneMoves(unhired, today);
            for (int i = inCastles.Count - 1; i >= 0; i--)
            {
                Hero hero = inCastles[i];
                if (Hosts(hero.CurrentSettlement) || hero.CurrentSettlement == playerSettlement)
                {
                    continue;
                }

                SendToTown(hero, CastleMarketTownsBehavior.NearestTown(hero.CurrentSettlement));
                inCastles.RemoveAt(i);
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !settings.Enabled || !settings.CastleCompanions)
            {
                return;
            }

            int minimumStay = Math.Max(1, settings.CompanionMinimumStayDays);
            if (inCastles.Count > 0 && MBRandom.RandomFloat * 100f < settings.CompanionDeparturePercent)
            {
                List<Hero> leavers = new List<Hero>();
                foreach (Hero hero in inCastles)
                {
                    if (hero.CurrentSettlement != playerSettlement && HasStayed(hero, today, minimumStay))
                    {
                        leavers.Add(hero);
                    }
                }

                if (leavers.Count > 0)
                {
                    Hero leaving = leavers[MBRandom.RandomInt(leavers.Count)];
                    Settlement town = NearbyTown(leaving.CurrentSettlement, playerSettlement);
                    if (town != null)
                    {
                        SendToTown(leaving, town);
                        inCastles.Remove(leaving);
                    }
                }
            }

            int cap = MathF.Floor((inTowns.Count + inCastles.Count) * CastleShare);
            if (inCastles.Count >= cap || MBRandom.RandomFloat * 100f >= settings.CompanionArrivalPercent)
            {
                return;
            }

            List<Hero> travellers = new List<Hero>();
            foreach (Hero hero in inTowns)
            {
                if (hero.CurrentSettlement != playerSettlement && !hero.CurrentSettlement.IsUnderSiege && HasStayed(hero, today, minimumStay))
                {
                    travellers.Add(hero);
                }
            }

            List<Settlement> castles = new List<Settlement>();
            foreach (Town castle in Town.AllCastles)
            {
                Settlement settlement = castle == null ? null : castle.Settlement;
                if (settlement != null && settlement != playerSettlement && !settlement.IsUnderSiege && Hosts(settlement))
                {
                    castles.Add(settlement);
                }
            }

            // A companion only moves to one of the castles closest to their town; if those already have one, another companion is tried.
            while (travellers.Count > 0 && castles.Count > 0)
            {
                int index = MBRandom.RandomInt(travellers.Count);
                Hero traveller = travellers[index];
                travellers.RemoveAt(index);

                List<Settlement> open = new List<Settlement>();
                foreach (Settlement castle in Nearest(traveller.CurrentSettlement, castles, NearbyChoices))
                {
                    if (!HasFreeCompanion(castle))
                    {
                        open.Add(castle);
                    }
                }

                if (open.Count > 0)
                {
                    Move(traveller, open[MBRandom.RandomInt(open.Count)]);
                    Sent.Add(traveller.StringId);
                    return;
                }
            }
        }

        private static float Today()
        {
            return (float)CampaignTime.Now.ToDays;
        }

        private static bool HasStayed(Hero hero, float today, int minimumStay)
        {
            float movedDay;
            return !LastMoved.TryGetValue(hero.StringId, out movedDay) || today - movedDay >= minimumStay;
        }

        private static void PruneMoves(HashSet<string> unhired, float today)
        {
            List<string> stale = new List<string>();
            foreach (KeyValuePair<string, float> pair in LastMoved)
            {
                if (!unhired.Contains(pair.Key) || today - pair.Value >= CastleMarketTownsSettings.MaxCompanionStayDays)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (string heroId in stale)
            {
                LastMoved.Remove(heroId);
            }
        }

        private static bool IsFree(Hero hero)
        {
            return hero != null && hero.IsAlive && hero.IsActive && hero.IsWanderer && hero.CompanionOf == null && !hero.IsPrisoner
                && hero.PartyBelongedTo == null && hero.GovernorOf == null && hero.CurrentSettlement != null && hero.Issue == null
                && !Campaign.Current.QuestManager.IsQuestGiver(hero) && hero.CanMoveToSettlement();
        }

        // Unhired companions are adults without a clan. A child or a clan member who is a wanderer only by occupation, such as
        // They Married's children living with their caretakers, is never sent to a castle; one already there can still go back.
        private static bool CanJoinCastles(Hero hero)
        {
            return hero.Clan == null && !hero.IsChild;
        }

        private static bool HasFreeCompanion(Settlement settlement)
        {
            foreach (Hero hero in settlement.HeroesWithoutParty)
            {
                if (hero != null && hero.IsWanderer && hero.CompanionOf == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Settlement NearbyTown(Settlement castle, Settlement playerSettlement)
        {
            List<Settlement> towns = new List<Settlement>();
            foreach (Town town in Town.AllTowns)
            {
                Settlement settlement = town == null ? null : town.Settlement;
                if (settlement != null && settlement != playerSettlement && !settlement.IsUnderSiege)
                {
                    towns.Add(settlement);
                }
            }

            List<Settlement> nearby = Nearest(castle, towns, NearbyChoices);
            List<Settlement> empty = new List<Settlement>();
            foreach (Settlement town in nearby)
            {
                if (!HasFreeCompanion(town))
                {
                    empty.Add(town);
                }
            }

            List<Settlement> choices = empty.Count > 0 ? empty : nearby;
            return choices.Count == 0 ? null : choices[MBRandom.RandomInt(choices.Count)];
        }

        private static List<Settlement> Nearest(Settlement from, List<Settlement> candidates, int count)
        {
            List<KeyValuePair<float, Settlement>> reachable = new List<KeyValuePair<float, Settlement>>();
            foreach (Settlement candidate in candidates)
            {
                float distance = Distance(from, candidate, MobileParty.NavigationType.Default);
                if (!(distance < UnreachableDistance))
                {
                    distance = Distance(from, candidate, MobileParty.NavigationType.All);
                }

                if (distance < UnreachableDistance)
                {
                    reachable.Add(new KeyValuePair<float, Settlement>(distance, candidate));
                }
            }

            reachable.Sort(delegate(KeyValuePair<float, Settlement> left, KeyValuePair<float, Settlement> right) { return left.Key.CompareTo(right.Key); });
            List<Settlement> nearest = new List<Settlement>();
            for (int i = 0; i < reachable.Count && i < count; i++)
            {
                nearest.Add(reachable[i].Value);
            }

            return nearest;
        }

        private static float Distance(Settlement from, Settlement to, MobileParty.NavigationType navigation)
        {
            return Campaign.Current.Models.MapDistanceModel.GetDistance(from, to, false, false, navigation);
        }

        private static void SendToTown(Hero hero, Settlement town)
        {
            if (town == null)
            {
                return;
            }

            Move(hero, town);
            Sent.Remove(hero.StringId);
        }

        private static void Move(Hero hero, Settlement target)
        {
            LeaveSettlementAction.ApplyForCharacterOnly(hero);
            EnterSettlementAction.ApplyForCharacterOnly(hero, target);
            LastMoved[hero.StringId] = Today();
        }
    }

    // In towns the base game seats unhired companions in the tavern; in castles it puts them in the courtyard.
    // War Sails hands every hero outside its storyline to this base model, so the change holds with it too.
    [HarmonyPatch]
    internal static class CastleCompanionTavernPatch
    {
        static List<MethodBase> Targets()
        {
            List<MethodBase> targets = new List<MethodBase>();
            MethodInfo vanilla = AccessTools.Method(typeof(DefaultHeroAgentLocationModel), "GetLocationForHero");
            if (vanilla != null)
            {
                targets.Add(vanilla);
            }

            return targets;
        }

        static bool Prepare()
        {
            return Targets().Count > 0;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static void Postfix(Hero hero, Settlement settlement, ref HeroAgentLocationModel.HeroLocationDetail __2, ref Location __result)
        {
            if (hero == null || !hero.IsWanderer || hero.CompanionOf != null || settlement == null || hero.CurrentSettlement != settlement
                || !settlement.HeroesWithoutParty.Contains(hero) || !CastleCompanions.Hosts(settlement))
            {
                return;
            }

            Location tavern = settlement.LocationComplex == null ? null : settlement.LocationComplex.GetLocationWithId("tavern");
            if (tavern != null)
            {
                __2 = HeroAgentLocationModel.HeroLocationDetail.Wanderer;
                __result = tavern;
            }
        }
    }

    // War Sails can be loaded after this module's regular Harmony pass. Patch its wrapper after every submodule assembly is ready,
    // without referencing NavalDLC directly or making it a required dependency.
    internal static class CastleWarSailsCompanionGuard
    {
        public static void TryPatch(Harmony harmony)
        {
            Type model = AccessTools.TypeByName("NavalDLC.GameComponents.NavalDLCHeroAgentLocationModel");
            MethodInfo target = model == null ? null : AccessTools.Method(model, "GetLocationForHero");
            MethodInfo postfix = AccessTools.Method(typeof(CastleCompanionTavernPatch), "Postfix");
            if (target == null || postfix == null)
            {
                return;
            }

            Patches patches = Harmony.GetPatchInfo(target);
            if (patches != null)
            {
                foreach (Patch patch in patches.Postfixes)
                {
                    if (patch.owner == harmony.Id && patch.PatchMethod == postfix)
                    {
                        return;
                    }
                }
            }

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }
    }
}
