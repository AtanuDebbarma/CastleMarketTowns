using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace CastleMarketTowns
{
    // Optional CPI troop-size compatibility for covered-castle-origin caravans.
    internal static class CastleCaravanSizeCompat
    {
        private static readonly TextObject LimitText = new TextObject("{=cmt_caravan_size_compat}Castle caravan party limit");
        private static MethodInfo _tryGetLimit;
        private static bool _resolved;
        private static bool _apiMissingLogged;
        private static MethodBase _patched;
        private static Harmony _harmony;

        internal static bool IsCpiModuleActive()
        {
            try
            {
                if (ModuleHelper.IsModuleActive("CaravanPartyLimitIncreasedFixed"))
                {
                    return true;
                }

                // Soft accept legacy id if an old build is still in the list.
                return ModuleHelper.IsModuleActive("CaravanPartyLimitIncreased");
            }
            catch
            {
                return false;
            }
        }

        internal static bool ApiReady()
        {
            EnsureResolved();
            return _tryGetLimit != null;
        }

        internal static bool ShowMcmGroup()
        {
            return IsCpiModuleActive() && ApiReady();
        }

        internal static void PatchActiveModel(Harmony harmony)
        {
            if (harmony != null)
            {
                _harmony = harmony;
            }

            if (_harmony == null || !ShowMcmGroup())
            {
                return;
            }

            PartySizeLimitModel model = Campaign.Current != null ? Campaign.Current.Models.PartySizeLimitModel : null;
            if (model == null)
            {
                return;
            }

            MethodInfo target = FindPartyMemberSizeLimit(model.GetType());
            if (target == null || _patched == target)
            {
                return;
            }

            try
            {
                if (_patched != null)
                {
                    _harmony.Unpatch(_patched, HarmonyPatchType.All, _harmony.Id);
                }

                HarmonyMethod postfix = new HarmonyMethod(typeof(CastleCaravanSizeCompat).GetMethod(
                    "GetPartyMemberSizeLimitPostfix", BindingFlags.Static | BindingFlags.NonPublic));
                postfix.priority = Priority.Last;
                _harmony.Patch(target, postfix: postfix);
                _patched = target;
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not apply castle caravan size compatibility: " + exception.Message);
            }
        }

        internal static void EnsurePatched()
        {
            PatchActiveModel(_harmony);
        }

        private static MethodInfo FindPartyMemberSizeLimit(Type type)
        {
            Type[] args = new Type[] { typeof(PartyBase), typeof(bool) };
            while (type != null && type != typeof(object))
            {
                MethodInfo method = type.GetMethod(
                    "GetPartyMemberSizeLimit",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null,
                    args,
                    null);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static void GetPartyMemberSizeLimitPostfix(PartyBase party, bool includeDescriptions, ref ExplainedNumber __result)
        {
            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings == null || !ShowMcmGroup() || party == null || !party.IsMobile)
            {
                return;
            }

            MobileParty mobile = party.MobileParty;
            if (mobile == null || !mobile.IsCaravan || !CastleCaravanOrigins.IsCastleOriginCaravan(mobile))
            {
                return;
            }

            int cpiLimit;
            if (!TryGetCpiLimit(party, out cpiLimit) || cpiLimit < 1)
            {
                return;
            }

            int percent = settings.CastleCaravanPartySizePercent;
            if (percent < 80)
            {
                percent = 80;
            }

            if (percent > 100)
            {
                percent = 100;
            }

            if (percent == 100)
            {
                return;
            }

            int desired = (int)Math.Floor(cpiLimit * (percent / 100.0) + 0.0001);
            if (desired < 1)
            {
                desired = 1;
            }

            float current = __result.ResultNumber;
            float delta = desired - current;
            if (delta > 0.01f || delta < -0.01f)
            {
                __result.Add(delta, LimitText, null);
            }
        }

        private static bool TryGetCpiLimit(PartyBase party, out int limit)
        {
            limit = 0;
            EnsureResolved();
            if (_tryGetLimit == null)
            {
                return false;
            }

            try
            {
                object[] args = new object[] { party, 0 };
                object ok = _tryGetLimit.Invoke(null, args);
                if (ok is bool && (bool)ok)
                {
                    limit = (int)args[1];
                    return limit > 0;
                }
            }
            catch (Exception exception)
            {
                if (!_apiMissingLogged)
                {
                    _apiMissingLogged = true;
                    Debug.Print("Castle Market Towns CPI size API failed: " + exception.Message);
                }
            }

            return false;
        }

        private static void EnsureResolved()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            if (!IsCpiModuleActive())
            {
                return;
            }

            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType("CaravanPartyLimitIncreasedFixed.CaravanPartyLimitApi", false);
                    if (type == null)
                    {
                        type = assembly.GetType("CaravanPartyLimitIncreased.CaravanPartyLimitApi", false);
                    }

                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo method = type.GetMethod(
                        "TryGetEffectiveCaravanPartyLimit",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(PartyBase), typeof(int).MakeByRefType() },
                        null);
                    if (method != null && method.ReturnType == typeof(bool))
                    {
                        _tryGetLimit = method;
                        return;
                    }
                }

                if (!_apiMissingLogged)
                {
                    _apiMissingLogged = true;
                    Debug.Print("Castle Market Towns: CaravanPartyLimitIncreasedFixed is enabled but its size API was not found; castle caravan size compatibility is inactive.");
                }
            }
            catch (Exception exception)
            {
                Debug.Print("Castle Market Towns could not resolve CPI size API: " + exception.Message);
            }
        }
    }
}
