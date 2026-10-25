using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CastleMarketTowns
{
    public class CastleMarketTownsSubModule : MBSubModuleBase
    {
        private readonly Harmony _harmony = new Harmony("castle.market.towns");
        private bool _brothelsPatched;

        protected override void OnSubModuleLoad()
        {
            foreach (System.Type type in AccessTools.GetTypesFromAssembly(typeof(CastleMarketTownsSubModule).Assembly))
            {
                if (type.GetCustomAttributes(typeof(HarmonyAttribute), false).Length == 0)
                {
                    continue;
                }

                try
                {
                    if (!PatchTargetsExist(type))
                    {
                        continue;
                    }

                    _harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception exception)
                {
                    TaleWorlds.Library.Debug.Print("Castle Market Towns could not apply " + type.Name + ": " + exception.Message);
                }
            }

            ReportEmptyTranspilers();
            CastleWorkshops.ReportMissingLookups();
        }

        // Harmony quietly skips a class when its Prepare method returns false. Check those classes explicitly so a renamed or
        // removed game method produces the same named diagnostic as a patching exception or a transpiler that matched no code.
        private static bool PatchTargetsExist(System.Type patchType)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo prepare = patchType.GetMethod("Prepare", flags, null, System.Type.EmptyTypes, null);
            if (prepare != null && prepare.ReturnType == typeof(bool) && !(bool)prepare.Invoke(null, null))
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not find any game methods for " + patchType.Name + "; that castle feature is inactive, likely because another mod or a game update changed it.");
                return false;
            }

            MethodInfo targetMethods = patchType.GetMethod("TargetMethods", flags, null, System.Type.EmptyTypes, null);
            if (targetMethods == null)
            {
                return true;
            }

            System.Collections.IEnumerable targets = targetMethods.Invoke(null, null) as System.Collections.IEnumerable;
            bool found = false;
            if (targets != null)
            {
                foreach (object target in targets)
                {
                    if (target == null)
                    {
                        TaleWorlds.Library.Debug.Print("Castle Market Towns found a missing game method for " + patchType.Name + "; that castle feature is inactive, likely because another mod or a game update changed it.");
                        return false;
                    }

                    found = true;
                }
            }

            if (!found)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not find any game methods for " + patchType.Name + "; that castle feature is inactive, likely because another mod or a game update changed it.");
            }

            return found;
        }

        // Every transpiler here routes the patched method through a helper in this assembly; no such call means it matched nothing.
        private void ReportEmptyTranspilers()
        {
            Assembly mod = typeof(CastleMarketTownsSubModule).Assembly;
            foreach (MethodBase method in _harmony.GetPatchedMethods())
            {
                try
                {
                    if (!HasOwnTranspiler(Harmony.GetPatchInfo(method)))
                    {
                        continue;
                    }

                    bool changed = false;
                    foreach (CodeInstruction instruction in PatchProcessor.GetCurrentInstructions(method))
                    {
                        MethodInfo called = instruction.operand as MethodInfo;
                        if (called != null && called.DeclaringType != null && called.DeclaringType.Assembly == mod)
                        {
                            changed = true;
                            break;
                        }
                    }

                    if (!changed)
                    {
                        TaleWorlds.Library.Debug.Print("Castle Market Towns found nothing to change in " + method.DeclaringType.Name + "." + method.Name + "; that castle feature is inactive, likely because another mod or a game update changed it.");
                    }
                }
                catch (System.Exception exception)
                {
                    TaleWorlds.Library.Debug.Print("Castle Market Towns could not check " + method.Name + ": " + exception.Message);
                }
            }
        }

        private bool HasOwnTranspiler(Patches info)
        {
            if (info == null)
            {
                return false;
            }

            foreach (Patch patch in info.Transpilers)
            {
                if (patch.owner == _harmony.Id)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            if (_brothelsPatched)
            {
                return;
            }

            _brothelsPatched = true;
            try
            {
                CastleBrothelGuard.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not guard Captivity Events brothels: " + exception.Message);
            }

            try
            {
                CastleSmithyGuard.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not open smithies in castles: " + exception.Message);
            }

            try
            {
                CastleWarSailsCompanionGuard.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not place companions in castle taverns under War Sails: " + exception.Message);
            }

            try
            {
                CastleImprovedGarrisons.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not add Improved Garrisons support: " + exception.Message);
            }

            try
            {
                CastleCompanionsCraft.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not add CompanionsCraft support: " + exception.Message);
            }

            try
            {
                CastleTheyMarried.TryPatch(_harmony);
            }
            catch (System.Exception exception)
            {
                TaleWorlds.Library.Debug.Print("Castle Market Towns could not add They Married support: " + exception.Message);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            Campaign campaign = game.GameType as Campaign;
            if (campaign == null)
            {
                return;
            }

            CampaignGameStarter starter = gameStarter as CampaignGameStarter;
            if (starter != null)
            {
                starter.AddBehavior(new CastleMarketTownsBehavior());
            }
        }
    }
}
