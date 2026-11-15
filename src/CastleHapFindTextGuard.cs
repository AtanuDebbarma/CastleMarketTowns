using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CastleMarketTowns
{
    // HAP initializes HAPTextObjects with GameTexts.FindText in a static ctor.
    // That can NullReference before GameTexts is ready; the type then stays dead and
    // town/prosperity UI throws TypeInitializationException. Swallow only that NRE so
    // HAP finishes init with id-tagged TextObjects; CMT's HAP food API can then be tested.
    // Safe no-op when HAP is absent. Prefer loading HAP below CMT so this runs first.
    internal static class CastleHapFindTextGuard
    {
        private static bool _patched;
        private static bool _logged;

        internal static void TryPatch(Harmony harmony)
        {
            if (_patched || !CastleHapCompat.ModulePresent)
            {
                return;
            }

            MethodInfo[] methods = typeof(GameTexts).GetMethods(BindingFlags.Public | BindingFlags.Static);
            int patched = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "FindText" || method.ReturnType != typeof(TextObject))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                try
                {
                    harmony.Patch(
                        method,
                        finalizer: new HarmonyMethod(typeof(CastleHapFindTextGuard).GetMethod(
                            "FindTextFinalizer",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                    patched++;
                }
                catch (Exception exception)
                {
                    Debug.Print("Castle Market Towns could not guard GameTexts.FindText for Harvest and Production: " + exception.Message);
                }
            }

            _patched = patched > 0;
            if (_patched)
            {
                Debug.Print("Castle Market Towns: guarding GameTexts.FindText for Harvest and Production early text init (" + patched + " overload(s)).");
            }
        }

        // Harmony finalizer: return null to swallow; otherwise rethrow.
        private static Exception FindTextFinalizer(Exception __exception, ref TextObject __result, object[] __args)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!(__exception is NullReferenceException))
            {
                return __exception;
            }

            string id = null;
            if (__args != null && __args.Length > 0)
            {
                id = __args[0] as string;
            }

            if (string.IsNullOrEmpty(id))
            {
                __result = TextObject.GetEmpty();
            }
            else
            {
                __result = new TextObject("{=" + id + "}" + id);
            }

            if (!_logged)
            {
                _logged = true;
                Debug.Print("Castle Market Towns: absorbed early GameTexts.FindText NullReferenceException so Harvest and Production text objects can finish initializing.");
            }

            return null;
        }
    }
}
