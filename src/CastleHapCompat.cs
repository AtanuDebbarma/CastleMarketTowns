using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace CastleMarketTowns
{
    // Soft dependency on Harvest and Production. Reflection only; never a hard DLL reference.
    internal static class CastleHapCompat
    {
        internal const string ModuleId = "HarvestAndProduction";
        internal const string SourceId = "cmt_castle_market_food";
        private const string FoodBehaviorTypeName = "HarvestAndProduction.Behaviors.TownFoodEconomyBehavior";
        private const string FoodModelTypeName = "HarvestAndProduction.Models.CustomSettlementFoodModel";

        internal enum AdapterState
        {
            Absent,
            Ready,
            Rejected,
            Disabled
        }

        private static AdapterState _state = AdapterState.Absent;
        private static bool _validated;
        private static bool _stateLogged;
        private static bool _runtimeDisableLogged;
        private static bool _playerNotified;
        private static string _rejectReason = string.Empty;
        private static Assembly _hapAssembly;
        private static MethodInfo _tryRequisition;
        private static MethodInfo _getAvailable;
        private static Type _rawMaterialModeType;
        private static object _modeSkip;
        private static object _modeLast;
        private static object _modeInclude;
        private static Campaign _validatedForCampaign;

        internal static AdapterState State
        {
            get { return _state; }
        }

        internal static bool ModulePresent
        {
            get
            {
                try
                {
                    return ModuleHelper.IsModuleActive(ModuleId);
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static bool ShowMcmGroup
        {
            get { return ModulePresent; }
        }

        internal static bool OwnsCastleRebellions
        {
            get { return ModulePresent; }
        }

        internal static bool FoodIntegrationReady
        {
            get
            {
                EnsureValidated();
                if (_state != AdapterState.Ready)
                {
                    return false;
                }

                CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
                return settings == null || settings.HapFoodIntegration;
            }
        }

        // HAP is loaded: never use CMT SoldItems food bridge (Ready with toggle off, Rejected, or Disabled).
        internal static bool BlocksSoldItemsFoodBridge
        {
            get
            {
                EnsureValidated();
                return _state == AdapterState.Ready
                    || _state == AdapterState.Rejected
                    || _state == AdapterState.Disabled;
            }
        }

        // HAP's caravan food scoring can run even when the food requisition API is Rejected/Disabled.
        internal static bool SuppressFoodCaravanAttraction
        {
            get
            {
                if (!ModulePresent)
                {
                    return false;
                }

                CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
                return settings == null || settings.HapReduceFoodCaravanAttraction;
            }
        }

        // Fail closed: never export food while HAP is loaded, even if the food API is Rejected/Disabled.
        internal static bool SkipFoodSurplusExport
        {
            get { return ModulePresent; }
        }

        internal static void ResetForCampaign()
        {
            _validated = false;
            _stateLogged = false;
            _runtimeDisableLogged = false;
            _playerNotified = false;
            _rejectReason = string.Empty;
            _state = AdapterState.Absent;
            _hapAssembly = null;
            _tryRequisition = null;
            _getAvailable = null;
            _rawMaterialModeType = null;
            _modeSkip = null;
            _modeLast = null;
            _modeInclude = null;
            _validatedForCampaign = null;
        }

        internal static void EnsureValidated()
        {
            Campaign campaign = Campaign.Current;
            if (_validated && _validatedForCampaign == campaign)
            {
                return;
            }

            ResetForCampaign();
            _validated = true;
            _validatedForCampaign = campaign;

            if (!ModulePresent)
            {
                _state = AdapterState.Absent;
                LogStateOnce("Harvest and Production absent; castle market food uses the normal CMT SoldItems bridge.");
                return;
            }

            try
            {
                _hapAssembly = FindAssembly();
                if (_hapAssembly == null)
                {
                    Reject("HarvestAndProduction assembly was not loaded.");
                    return;
                }

                Type behavior = _hapAssembly.GetType(FoodBehaviorTypeName, false);
                if (behavior == null)
                {
                    Reject(FoodBehaviorTypeName + " was not found.");
                    return;
                }

                _rawMaterialModeType = behavior.GetNestedType("RawMaterialMode", BindingFlags.Public);
                if (_rawMaterialModeType == null || !_rawMaterialModeType.IsEnum)
                {
                    Reject("TownFoodEconomyBehavior.RawMaterialMode enum was not found.");
                    return;
                }

                if (!TryParseMode("Skip", out _modeSkip)
                    || !TryParseMode("Last", out _modeLast)
                    || !TryParseMode("Include", out _modeInclude))
                {
                    Reject("RawMaterialMode is missing Skip, Last or Include.");
                    return;
                }

                _getAvailable = behavior.GetMethod(
                    "GetAvailableMarketFood",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Town), _rawMaterialModeType },
                    null);
                if (_getAvailable == null || _getAvailable.ReturnType != typeof(int))
                {
                    Reject("GetAvailableMarketFood(Town, RawMaterialMode) returning int was not found.");
                    return;
                }

                _tryRequisition = behavior.GetMethod(
                    "TryRequisitionMarketFood",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Town), typeof(int), typeof(string), typeof(int).MakeByRefType(), _rawMaterialModeType },
                    null);
                if (_tryRequisition == null || _tryRequisition.ReturnType != typeof(bool))
                {
                    Reject("TryRequisitionMarketFood(Town, int, string, out int, RawMaterialMode) returning bool was not found.");
                    return;
                }

                if (!FoodModelIsHap())
                {
                    Reject("Active SettlementFoodModel is not " + FoodModelTypeName + " from HarvestAndProduction.");
                    return;
                }

                _state = AdapterState.Ready;
                LogStateOnce("Harvest and Production ready; castle market food uses TryRequisitionMarketFood.");
            }
            catch (Exception exception)
            {
                Reject("HAP validation failed: " + exception.Message);
            }
        }

        internal static bool TryRequisitionRemainingDeficit(Town town, int desiredFood, out int transferred)
        {
            transferred = 0;
            EnsureValidated();
            if (_state != AdapterState.Ready || desiredFood <= 0 || town == null)
            {
                return false;
            }

            CastleMarketTownsSettings settings = CastleMarketTownsSettings.Current;
            if (settings != null && !settings.HapFoodIntegration)
            {
                return false;
            }

            try
            {
                object mode = ResolveRawMaterialMode(settings);
                object[] args = new object[] { town, desiredFood, SourceId, 0, mode };
                object result = _tryRequisition.Invoke(null, args);
                transferred = args[3] is int ? (int)args[3] : 0;
                return result is bool && (bool)result;
            }
            catch (Exception exception)
            {
                Disable("TryRequisitionMarketFood failed: " + Unwrap(exception));
                transferred = 0;
                return false;
            }
        }

        private static object ResolveRawMaterialMode(CastleMarketTownsSettings settings)
        {
            int choice = settings == null ? 1 : settings.HapRawMaterialMode;
            if (choice <= 0)
            {
                return _modeSkip;
            }

            if (choice >= 2)
            {
                return _modeInclude;
            }

            return _modeLast;
        }

        private static bool FoodModelIsHap()
        {
            if (_hapAssembly == null || Campaign.Current == null || Campaign.Current.Models == null)
            {
                return false;
            }

            SettlementFoodModel model = Campaign.Current.Models.SettlementFoodModel;
            if (model == null)
            {
                return false;
            }

            Type type = model.GetType();
            return type != null
                && type.FullName == FoodModelTypeName
                && type.Assembly == _hapAssembly;
        }

        private static Assembly FindAssembly()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                {
                    continue;
                }

                try
                {
                    if (assembly.GetName().Name == "HarvestAndProduction")
                    {
                        return assembly;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool TryParseMode(string name, out object value)
        {
            value = null;
            try
            {
                value = Enum.Parse(_rawMaterialModeType, name);
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        private static void Reject(string reason)
        {
            _state = AdapterState.Rejected;
            _rejectReason = reason ?? string.Empty;
            _tryRequisition = null;
            _getAvailable = null;
            LogStateOnce("Harvest and Production active but incompatible; castle market-to-granary conversion disabled. " + _rejectReason);
            NotifyPlayerOnce();
        }

        private static void Disable(string reason)
        {
            _state = AdapterState.Disabled;
            _rejectReason = reason ?? string.Empty;
            _tryRequisition = null;
            _getAvailable = null;
            LogRuntimeDisableOnce("Harvest and Production integration disabled for this session. " + _rejectReason);
            NotifyPlayerOnce();
        }

        private static void LogStateOnce(string message)
        {
            if (_stateLogged)
            {
                return;
            }

            _stateLogged = true;
            Debug.Print("Castle Market Towns: " + message);
        }

        private static void LogRuntimeDisableOnce(string message)
        {
            if (_runtimeDisableLogged)
            {
                return;
            }

            _runtimeDisableLogged = true;
            Debug.Print("Castle Market Towns: " + message);
        }

        private static void NotifyPlayerOnce()
        {
            if (_playerNotified || _state == AdapterState.Absent || _state == AdapterState.Ready)
            {
                return;
            }

            _playerNotified = true;
            try
            {
                string detail = string.IsNullOrEmpty(_rejectReason) ? string.Empty : (" " + _rejectReason);
                string message = new TextObject("{=cmt_error_hap}Harvest and Production is active, but Castle Market Towns could not use its food API. Castle market food will not be converted to Food Stocks.").ToString() + detail;
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
            }
            catch
            {
            }
        }

        private static string Unwrap(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            Exception inner = exception.InnerException;
            return inner != null ? inner.Message : exception.Message;
        }
    }
}
