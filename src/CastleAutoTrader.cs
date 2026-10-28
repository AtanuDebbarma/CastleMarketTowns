using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // AutoTrader is optional and not referenced: its types are looked up by name, and nothing is patched without it.
    // Without support, AutoTrader classifies castles as villages and reads Village.Gold, which castles do not have.
    // Either fully Ready or completely inactive: MCM off, API mismatch, or a runtime failure all fall back to vanilla IsTown.
    internal static class CastleAutoTrader
    {
        private const string MenuOptionId = "castle_market_towns_autotrader";

        private static bool _ready;
        private static bool _reportedInactive;
        private static bool _transpilerOk;
        private static int _isTownMatches;
        private static MethodInfo _condition;
        private static MethodInfo _consequence;
        private static MethodInfo _getBehavior;
        private static Campaign _behaviorCampaign;
        private static object _cachedBehavior;

        internal static bool Active
        {
            get
            {
                CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
                return settings == null || settings.AutoTraderSupport;
            }
        }

        internal static bool Ready
        {
            get { return _ready; }
        }

        // Used by the GetMerchantType redirect: when support is not Ready, return the settlement's real IsTown.
        internal static bool Operational
        {
            get { return _ready && Active; }
        }

        public static void TryPatch(Harmony harmony)
        {
            _ready = false;
            _reportedInactive = false;
            _transpilerOk = false;
            _isTownMatches = 0;
            _condition = null;
            _consequence = null;
            _getBehavior = null;
            _behaviorCampaign = null;
            _cachedBehavior = null;

            Type connector = AccessTools.TypeByName("AutoTrader.AutoTraderLogicConnector");
            if (connector == null)
            {
                return;
            }

            Type behaviorType = AccessTools.TypeByName("AutoTrader.TradeBehavior");
            if (behaviorType == null)
            {
                Reject("AutoTrader.TradeBehavior was not found.");
                return;
            }

            if (!typeof(CampaignBehaviorBase).IsAssignableFrom(behaviorType))
            {
                Reject("AutoTrader.TradeBehavior is not a campaign behavior.");
                return;
            }

            MethodInfo getBehavior = typeof(Campaign).GetMethod("GetCampaignBehavior", Type.EmptyTypes);
            if (getBehavior == null || !getBehavior.IsGenericMethodDefinition)
            {
                Reject("Campaign.GetCampaignBehavior<T>() was not found.");
                return;
            }

            Type merchantType = AccessTools.Inner(connector, "MerchantType");
            MethodInfo getMerchantType = AccessTools.DeclaredMethod(connector, "GetMerchantType", Type.EmptyTypes);
            MethodInfo condition = AccessTools.DeclaredMethod(behaviorType, "AutoTradeGoodsCondition", new Type[] { typeof(MenuCallbackArgs) });
            MethodInfo consequence = AccessTools.DeclaredMethod(behaviorType, "AutoTradeGoodsConsequence", new Type[] { typeof(MenuCallbackArgs) });

            if (getMerchantType == null || getMerchantType.IsStatic || merchantType == null || !merchantType.IsEnum || getMerchantType.ReturnType != merchantType)
            {
                Reject("AutoTrader.AutoTraderLogicConnector.GetMerchantType did not match the expected instance method returning MerchantType.");
                return;
            }

            if (condition == null || condition.IsStatic || condition.ReturnType != typeof(bool))
            {
                Reject("AutoTrader.TradeBehavior.AutoTradeGoodsCondition did not match the expected instance method returning bool.");
                return;
            }

            if (consequence == null || consequence.IsStatic || consequence.ReturnType != typeof(void))
            {
                Reject("AutoTrader.TradeBehavior.AutoTradeGoodsConsequence did not match the expected instance void method.");
                return;
            }

            if (CountIsTownCalls(getMerchantType) != 1)
            {
                Reject("AutoTrader GetMerchantType did not contain exactly one Settlement.IsTown check.");
                return;
            }

            _transpilerOk = false;
            _isTownMatches = 0;
            harmony.Patch(getMerchantType, transpiler: new HarmonyMethod(AccessTools.Method(typeof(CastleAutoTrader), "MerchantTypeTranspiler")));

            if (!_transpilerOk || _isTownMatches != 1 || !HasOwnTranspiler(harmony, getMerchantType))
            {
                Reject("AutoTrader GetMerchantType could not be redirected with exactly one IsTown replacement.");
                return;
            }

            _condition = condition;
            _consequence = consequence;
            _getBehavior = getBehavior.MakeGenericMethod(behaviorType);
            _ready = true;
        }

        public static void AddMenus(CampaignGameStarter starter, int index)
        {
            if (!_ready || starter == null)
            {
                return;
            }

            // AutoTrader's own ID so its translation modules cover the castle button too.
            starter.AddGameMenuOption(
                "castle",
                MenuOptionId,
                "{=ATTrade}Automatically trade wares",
                Condition,
                Consequence,
                false,
                index,
                false,
                null);
        }

        private static void Reject(string reason)
        {
            _ready = false;
            _condition = null;
            _consequence = null;
            _getBehavior = null;
            _behaviorCampaign = null;
            _cachedBehavior = null;
            Debug.Print("Castle Market Towns could not enable AutoTrader support: " + reason + " That support is inactive, likely because an AutoTrader update changed it.");
        }

        private static void Disable(string reason, Exception exception, bool notifyPlayer)
        {
            _ready = false;
            _condition = null;
            _consequence = null;
            _behaviorCampaign = null;
            _cachedBehavior = null;

            Exception root = Unwrap(exception);
            if (!_reportedInactive)
            {
                _reportedInactive = true;
                string detail = root == null ? reason : reason + " " + root;
                Debug.Print("Castle Market Towns AutoTrader support is inactive. " + detail);
            }

            if (notifyPlayer)
            {
                string message = new TextObject("{=cmt_error_autotrader}AutoTrader could not run at this castle.").ToString();
                if (root != null && !string.IsNullOrEmpty(root.Message))
                {
                    message = message + " " + root.Message;
                }

                InformationManager.DisplayMessage(new InformationMessage(message));
            }
        }

        private static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }

        private static int CountIsTownCalls(MethodInfo target)
        {
            int count = 0;
            foreach (CodeInstruction instruction in PatchProcessor.GetOriginalInstructions(target))
            {
                if (IsSettlementIsTown(instruction))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsSettlementIsTown(CodeInstruction instruction)
        {
            MethodInfo called = instruction.operand as MethodInfo;
            return called != null && called.Name == "get_IsTown" && called.DeclaringType == typeof(Settlement);
        }

        private static IEnumerable<CodeInstruction> MerchantTypeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            int matches = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (IsSettlementIsTown(list[i]))
                {
                    matches++;
                }
            }

            _isTownMatches = matches;
            if (matches != 1)
            {
                _transpilerOk = false;
                return list;
            }

            MethodInfo helper = AccessTools.Method(typeof(CastleTownRules), "AutoTraderTradesAsTown");
            for (int i = 0; i < list.Count; i++)
            {
                if (IsSettlementIsTown(list[i]))
                {
                    list[i].opcode = OpCodes.Call;
                    list[i].operand = helper;
                    break;
                }
            }

            _transpilerOk = true;
            return list;
        }

        private static bool HasOwnTranspiler(Harmony harmony, MethodBase target)
        {
            Patches info = Harmony.GetPatchInfo(target);
            if (info == null)
            {
                return false;
            }

            foreach (Patch patch in info.Transpilers)
            {
                if (patch.owner == harmony.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static object TradeBehavior()
        {
            Campaign campaign = Campaign.Current;
            if (campaign == null || _getBehavior == null)
            {
                return null;
            }

            if (_behaviorCampaign == campaign && _cachedBehavior != null)
            {
                return _cachedBehavior;
            }

            object behavior = _getBehavior.Invoke(campaign, null);
            if (behavior == null)
            {
                return null;
            }

            _behaviorCampaign = campaign;
            _cachedBehavior = behavior;
            return behavior;
        }

        private static bool IsEligibleCastle()
        {
            return _ready && Active && CastleTownRules.Covered(Settlement.CurrentSettlement);
        }

        private static object InvokeAutoTrader(MethodInfo method, MenuCallbackArgs args, bool notifyPlayer)
        {
            object behavior = TradeBehavior();
            if (behavior == null)
            {
                Disable("AutoTrader campaign behavior was missing.", null, notifyPlayer);
                return null;
            }

            if (method == null)
            {
                Disable("AutoTrader trade method was missing.", null, notifyPlayer);
                return null;
            }

            return method.Invoke(behavior, new object[] { args });
        }

        private static bool Condition(MenuCallbackArgs args)
        {
            if (!IsEligibleCastle())
            {
                return false;
            }

            try
            {
                object result = InvokeAutoTrader(_condition, args, false);
                return _ready && result is bool && (bool)result;
            }
            catch (Exception exception)
            {
                Disable("AutoTrader condition failed.", exception, false);
                return false;
            }
        }

        private static void Consequence(MenuCallbackArgs args)
        {
            if (!IsEligibleCastle() || _consequence == null)
            {
                return;
            }

            try
            {
                InvokeAutoTrader(_consequence, args, true);
            }
            catch (Exception exception)
            {
                Disable("AutoTrader consequence failed.", exception, true);
            }
        }
    }
}
