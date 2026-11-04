using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using TaleWorlds.Library;

namespace CastleMarketTowns
{
    [HarmonyPatch]
    internal static class CastleCpiMcmVisibilityPatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("MCM.Implementation.AttributeSettingsPropertyDiscoverer") != null;
        }

        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("MCM.Implementation.AttributeSettingsPropertyDiscoverer");
            if (type == null)
            {
                return null;
            }

            MethodInfo method = AccessTools.Method(type, "GetProperties", new Type[] { typeof(BaseSettings) });
            if (method == null)
            {
                method = AccessTools.Method(type, "GetProperties");
            }

            return method;
        }

        static void Postfix(BaseSettings settings, ref IEnumerable<ISettingsPropertyDefinition> __result)
        {
            try
            {
                if (settings == null || settings.Id == null || settings.Id.IndexOf("CastleMarketTowns") < 0 || __result == null)
                {
                    return;
                }

                CastleMarketTownsSettings cmt = settings as CastleMarketTownsSettings;
                if (cmt == null)
                {
                    cmt = CastleMarketTownsSettings.Current;
                }

                bool showCpi = CastleCaravanSizeCompat.ShowMcmGroup();
                bool hideOriginCapSlider = cmt != null && cmt.UnlimitedCastleOriginCaravans;

                if (showCpi && !hideOriginCapSlider)
                {
                    return;
                }

                List<ISettingsPropertyDefinition> kept = new List<ISettingsPropertyDefinition>();
                foreach (ISettingsPropertyDefinition definition in __result)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    string group = GetGroupName(definition);
                    if (!showCpi && IsCpiSupportGroup(group))
                    {
                        continue;
                    }

                    if (hideOriginCapSlider && IsOriginCapSlider(definition, group))
                    {
                        continue;
                    }

                    kept.Add(definition);
                }

                __result = kept;
            }
            catch (Exception e)
            {
                Debug.Print("Castle Market Towns MCM visibility filter skipped: " + e.Message);
            }
        }

        private static bool IsCpiSupportGroup(string group)
        {
            return group.IndexOf(CastleMarketTownsSettings.CpiSupportGroupLeaf) >= 0
                || group.IndexOf("cmt_mcm_cpi_support") >= 0;
        }

        private static bool IsOriginCapSlider(ISettingsPropertyDefinition definition, string group)
        {
            // Legacy marker path (older builds) plus property/display match for the flat Castle market entry.
            if (group.IndexOf("cmt_mcm_castle_origin_cap_slider") >= 0)
            {
                return true;
            }

            string propertyName = GetPropertyName(definition);
            if (propertyName == CastleMarketTownsSettings.CastleOriginCapSliderProperty)
            {
                return true;
            }

            string display = GetDisplayName(definition);
            return display.IndexOf(CastleMarketTownsSettings.CastleOriginCapSliderDisplay) >= 0
                || display.IndexOf("cmt_mcm_max_castle_origin_caravans_name") >= 0;
        }

        private static string GetGroupName(ISettingsPropertyDefinition definition)
        {
            string groupName = null;
            try
            {
                groupName = definition.GroupName;
            }
            catch
            {
            }

            try
            {
                PropertyInfo raw = definition.GetType().GetProperty(
                    "GroupNameRaw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (raw != null)
                {
                    string rawName = raw.GetValue(definition, null) as string;
                    if (!string.IsNullOrEmpty(rawName))
                    {
                        if (string.IsNullOrEmpty(groupName))
                        {
                            groupName = rawName;
                        }
                        else
                        {
                            groupName = groupName + "|" + rawName;
                        }
                    }
                }
            }
            catch
            {
            }

            return groupName == null ? string.Empty : groupName;
        }

        private static string GetDisplayName(ISettingsPropertyDefinition definition)
        {
            try
            {
                PropertyInfo display = definition.GetType().GetProperty(
                    "DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (display != null)
                {
                    object value = display.GetValue(definition, null);
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetPropertyName(ISettingsPropertyDefinition definition)
        {
            try
            {
                PropertyInfo reference = definition.GetType().GetProperty(
                    "PropertyReference", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object pref = reference != null ? reference.GetValue(definition, null) : null;
                if (pref != null)
                {
                    string[] names = new string[] { "Name", "PropertyName", "Id", "MemberName" };
                    for (int i = 0; i < names.Length; i++)
                    {
                        PropertyInfo pi = pref.GetType().GetProperty(
                            names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (pi == null)
                        {
                            continue;
                        }

                        object value = pi.GetValue(pref, null);
                        if (value != null)
                        {
                            string text = value.ToString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                return text;
                            }
                        }
                    }

                    PropertyInfo propertyInfo = pref.GetType().GetProperty(
                        "PropertyInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    PropertyInfo member = propertyInfo != null
                        ? propertyInfo.GetValue(pref, null) as PropertyInfo
                        : null;
                    if (member != null)
                    {
                        return member.Name;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

    }
}
