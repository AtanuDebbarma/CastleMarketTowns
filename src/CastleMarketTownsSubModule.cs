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
                    _harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception exception)
                {
                    TaleWorlds.Library.Debug.Print("Castle Market Towns could not apply " + type.Name + ": " + exception.Message);
                }
            }

            ReportEmptyTranspilers();
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
