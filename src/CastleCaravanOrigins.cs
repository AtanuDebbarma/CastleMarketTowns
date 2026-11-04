using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    // Tracks which covered castle a caravan originated from (not where it currently visits).
    internal static class CastleCaravanOrigins
    {
        private static readonly Dictionary<string, string> Origins = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> PreviousStops = new Dictionary<string, string>();
        private static readonly Dictionary<string, float> PreviousStopDays = new Dictionary<string, float>();
        private static Campaign _campaign;
        private static bool _migrated;
        private static bool _loggedBadOrigin;

        internal static string SerializeOrigins()
        {
            return SerializeMap(Origins);
        }

        internal static string SerializePreviousStops()
        {
            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, string> pair in PreviousStops)
            {
                float day;
                PreviousStopDays.TryGetValue(pair.Key, out day);
                entries.Add(pair.Key + "=" + pair.Value + "|" + day.ToString("R", CultureInfo.InvariantCulture));
            }

            return string.Join(";", entries.ToArray());
        }

        internal static void LoadOrigins(string data)
        {
            Origins.Clear();
            LoadMap(data, Origins);
            // New campaign / load: migration must run again against this campaign's parties.
            _migrated = false;
            _campaign = null;
            _loggedBadOrigin = false;
        }

        internal static void LoadPreviousStops(string data)
        {
            PreviousStops.Clear();
            PreviousStopDays.Clear();
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                int eq = entries[i].IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string partyId = entries[i].Substring(0, eq);
                string rest = entries[i].Substring(eq + 1);
                int bar = rest.LastIndexOf('|');
                if (bar <= 0)
                {
                    PreviousStops[partyId] = rest;
                    PreviousStopDays[partyId] = (float)CampaignTime.Now.ToDays;
                    continue;
                }

                float day;
                PreviousStops[partyId] = rest.Substring(0, bar);
                if (!float.TryParse(rest.Substring(bar + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out day))
                {
                    day = (float)CampaignTime.Now.ToDays;
                }

                PreviousStopDays[partyId] = day;
            }
        }

        internal static void ResetForNewCampaign()
        {
            Origins.Clear();
            PreviousStops.Clear();
            PreviousStopDays.Clear();
            _migrated = false;
            _campaign = null;
            _loggedBadOrigin = false;
        }

        internal static void EnsureMigrated()
        {
            if (Campaign.Current == null)
            {
                return;
            }

            if (_campaign != Campaign.Current)
            {
                // Campaign changed without LoadOrigins (e.g. new game): drop previous-campaign statics.
                if (_campaign != null)
                {
                    Origins.Clear();
                    PreviousStops.Clear();
                    PreviousStopDays.Clear();
                }

                _campaign = Campaign.Current;
                _migrated = false;
                _loggedBadOrigin = false;
            }

            if (_migrated)
            {
                return;
            }

            _migrated = true;
            PruneDestroyed();
            RepairMissingOrigins();
        }

        // Backfill saved origins for active caravans whose home/born settlement is a covered castle.
        private static void RepairMissingOrigins()
        {
            foreach (MobileParty party in MobileParty.AllCaravanParties)
            {
                if (party == null || !party.IsActive || !party.IsCaravan || string.IsNullOrEmpty(party.StringId))
                {
                    continue;
                }

                if (Origins.ContainsKey(party.StringId))
                {
                    continue;
                }

                Settlement origin = ResolveSpawnSettlement(party.Owner, party.HomeSettlement);
                if (origin != null && origin.IsCastle && CastleMarketTownsBehavior.AppliesTo(origin))
                {
                    Origins[party.StringId] = origin.StringId;
                }
            }
        }

        internal static Settlement ResolveSpawnSettlement(Hero hero, Settlement partyHome)
        {
            if (partyHome != null)
            {
                return partyHome;
            }

            if (hero == null)
            {
                return null;
            }

            if (hero.HomeSettlement != null)
            {
                return hero.HomeSettlement;
            }

            return hero.BornSettlement;
        }

        internal static void RecordOrigin(MobileParty caravan, Settlement castle)
        {
            if (caravan == null || castle == null || !castle.IsCastle)
            {
                return;
            }

            Origins[caravan.StringId] = castle.StringId;
        }

        internal static void Forget(MobileParty caravan)
        {
            if (caravan == null)
            {
                return;
            }

            Origins.Remove(caravan.StringId);
            PreviousStops.Remove(caravan.StringId);
            PreviousStopDays.Remove(caravan.StringId);
        }

        internal static Settlement GetOriginCastle(MobileParty caravan)
        {
            if (caravan == null)
            {
                return null;
            }

            EnsureMigrated();

            string castleId;
            if (!Origins.TryGetValue(caravan.StringId, out castleId) || string.IsNullOrEmpty(castleId))
            {
                // Do not fall back to HomeSettlement for gameplay effects: that would grant
                // size scaling without counting toward the origin cap. Migration/repair fills gaps.
                if (!_loggedBadOrigin)
                {
                    _loggedBadOrigin = true;
                    Debug.Print("Castle Market Towns: a caravan had no saved castle origin; it will not count as castle-origin until repaired.");
                }

                return null;
            }

            Settlement settlement = Settlement.Find(castleId);
            if (settlement == null || !settlement.IsCastle)
            {
                return null;
            }

            return settlement;
        }

        internal static bool IsCastleOriginCaravan(MobileParty caravan)
        {
            Settlement origin = GetOriginCastle(caravan);
            return origin != null && CastleMarketTownsBehavior.AppliesTo(origin);
        }

        internal static int CountOriginCaravans(Settlement castle)
        {
            if (castle == null)
            {
                return 0;
            }

            EnsureMigrated();
            PruneDestroyed();
            int count = 0;
            string castleId = castle.StringId;
            foreach (KeyValuePair<string, string> pair in Origins)
            {
                if (pair.Value != castleId)
                {
                    continue;
                }

                MobileParty party = FindCaravan(pair.Key);
                if (party != null && party.IsActive && party.IsCaravan)
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool AtOriginCap(Settlement castle)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || castle == null || !CastleMarketTownsBehavior.AppliesTo(castle))
            {
                return false;
            }

            if (settings.UnlimitedCastleOriginCaravans)
            {
                return false;
            }

            int cap = settings.MaxCastleOriginCaravans;
            if (cap < 1)
            {
                cap = 1;
            }

            if (cap > 20)
            {
                cap = 20;
            }

            return CountOriginCaravans(castle) >= cap;
        }

        internal static bool HeroBlockedByOriginCap(Hero hero)
        {
            if (hero == null)
            {
                return false;
            }

            Settlement home = ResolveSpawnSettlement(hero, null);
            if (home == null || !home.IsCastle || !CastleMarketTownsBehavior.AppliesTo(home))
            {
                return false;
            }

            return AtOriginCap(home);
        }

        internal static void RememberStop(MobileParty caravan, Settlement settlement)
        {
            if (caravan == null || settlement == null)
            {
                return;
            }

            PreviousStops[caravan.StringId] = settlement.StringId;
            PreviousStopDays[caravan.StringId] = (float)CampaignTime.Now.ToDays;
        }

        internal static bool IsRecentPreviousStop(MobileParty caravan, Settlement candidate, float withinDays)
        {
            if (caravan == null || candidate == null)
            {
                return false;
            }

            string previousId;
            if (!PreviousStops.TryGetValue(caravan.StringId, out previousId) || previousId != candidate.StringId)
            {
                return false;
            }

            float day;
            if (!PreviousStopDays.TryGetValue(caravan.StringId, out day))
            {
                return true;
            }

            return (CampaignTime.Now.ToDays - day) <= withinDays;
        }

        internal static int CountTargetingOrPresent(Settlement castle, MobileParty exclude)
        {
            if (castle == null || castle.Town == null)
            {
                return 0;
            }

            int count = 0;
            string excludeId = exclude == null ? null : exclude.StringId;
            foreach (MobileParty party in MobileParty.AllCaravanParties)
            {
                if (party == null || !party.IsActive || !party.IsCaravan)
                {
                    continue;
                }

                if (excludeId != null && party.StringId == excludeId)
                {
                    continue;
                }

                if (party.CurrentSettlement == castle)
                {
                    count++;
                    continue;
                }

                if (party.TargetSettlement == castle)
                {
                    count++;
                }
            }

            return count;
        }

        private static void PruneDestroyed()
        {
            List<string> remove = null;
            foreach (KeyValuePair<string, string> pair in Origins)
            {
                if (FindCaravan(pair.Key) == null)
                {
                    if (remove == null)
                    {
                        remove = new List<string>();
                    }

                    remove.Add(pair.Key);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                Origins.Remove(remove[i]);
                PreviousStops.Remove(remove[i]);
                PreviousStopDays.Remove(remove[i]);
            }
        }

        private static MobileParty FindCaravan(string partyId)
        {
            if (string.IsNullOrEmpty(partyId))
            {
                return null;
            }

            foreach (MobileParty party in MobileParty.AllCaravanParties)
            {
                if (party != null && party.StringId == partyId)
                {
                    return party;
                }
            }

            return null;
        }

        private static string SerializeMap(Dictionary<string, string> values)
        {
            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, string> pair in values)
            {
                entries.Add(pair.Key + "=" + pair.Value);
            }

            return string.Join(";", entries.ToArray());
        }

        private static void LoadMap(string data, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                int separator = entries[i].IndexOf('=');
                if (separator > 0)
                {
                    values[entries[i].Substring(0, separator)] = entries[i].Substring(separator + 1);
                }
            }
        }
    }
}
