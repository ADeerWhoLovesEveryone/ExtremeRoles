﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using HarmonyLib;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;


namespace ExtremeRoles.Patches.Option
{
    [HarmonyPatch]
    class GameOptionsDataPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(GameOptionsData).GetMethods().Where(
                x => x.ReturnType == typeof(string) &&
                x.GetParameters().Length == 1 &&
                x.GetParameters()[0].ParameterType == typeof(int));
        }

        private static void Postfix(ref string __result)
        {

            List<string> pages = new List<string>();
            pages.Add(__result);

            StringBuilder entry = new StringBuilder();
            List<string> entries = new List<string>();

            var allOption = OptionHolder.AllOption;

            entries.Add(
                CustomOption.OptionToString(allOption[(int)OptionHolder.CommonOptionKey.PresetSelection]));

            entries.Add(
                CustomOption.OptionToString(allOption[(int)OptionHolder.CommonOptionKey.UseStrongRandomGen]));

            var optionName = Design.ColoedString(
                new Color(204f / 255f, 204f / 255f, 0, 1f),
                translate("crewmateRoles"));
            var min = allOption[(int)OptionHolder.CommonOptionKey.MinCremateRoles].GetValue();
            var max = allOption[(int)OptionHolder.CommonOptionKey.MaxCremateRoles].GetValue();
            if (min > max) { min = max; }
            var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            optionName = Design.ColoedString(
                new Color(204f / 255f, 204f / 255f, 0, 1f),
                translate("neutralRoles"));
            min = allOption[(int)OptionHolder.CommonOptionKey.MinNeutralRoles].GetValue();
            max = allOption[(int)OptionHolder.CommonOptionKey.MaxNeutralRoles].GetValue();
            if (min > max) { min = max; }
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            optionName = Design.ColoedString(
                new Color(204f / 255f, 204f / 255f, 0, 1f),
                translate("impostorRoles"));
            min = allOption[(int)OptionHolder.CommonOptionKey.MinImpostorRoles].GetValue();
            max = allOption[(int)OptionHolder.CommonOptionKey.MaxImpostorRoles].GetValue();

            if (min > max) { min = max; }
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            entries.Add(entry.ToString().Trim('\r', '\n'));

            entry = new StringBuilder();

            foreach (OptionHolder.CommonOptionKey id in Enum.GetValues(typeof(OptionHolder.CommonOptionKey)))
            {
                if ((id == OptionHolder.CommonOptionKey.PresetSelection) ||
                    (id == OptionHolder.CommonOptionKey.UseStrongRandomGen) ||
                    (id == OptionHolder.CommonOptionKey.MinCremateRoles) ||
                    (id == OptionHolder.CommonOptionKey.MaxCremateRoles) ||
                    (id == OptionHolder.CommonOptionKey.MinNeutralRoles) ||
                    (id == OptionHolder.CommonOptionKey.MaxNeutralRoles) ||
                    (id == OptionHolder.CommonOptionKey.MinImpostorRoles) ||
                    (id == OptionHolder.CommonOptionKey.MaxImpostorRoles))
                {
                    continue;
                }
                string optionStr = CustomOption.OptionToString(allOption[(int)id]);
                if (optionStr != string.Empty) { entry.AppendLine(optionStr); }
            }

            entries.Add(entry.ToString().Trim('\r', '\n'));

            foreach (CustomOptionBase option in OptionHolder.AllOption.Values)
            {
                if (Enum.IsDefined(typeof(OptionHolder.CommonOptionKey), option.Id))
                {
                    continue;
                }


                if (option.Parent == null)
                {
                    if (!option.Enabled)
                    {
                        continue;
                    }

                    entry = new StringBuilder();
                    if (!option.IsHidden)
                    {
                        entry.AppendLine(CustomOption.OptionToString(option));
                    }

                    addChildren(option, ref entry, option.IsHidden ? 0 : 1);
                    entries.Add(entry.ToString().Trim('\r', '\n'));
                }
            }

            int maxLines = 28;
            int lineCount = 0;
            string page = "";
            foreach (var e in entries)
            {
                int lines = e.Count(c => c == '\n') + 1;

                if (lineCount + lines > maxLines)
                {
                    pages.Add(page);
                    page = "";
                    lineCount = 0;
                }

                page = string.Concat(page,e,"\n\n");
                lineCount += lines + 1;
            }

            page = page.Trim('\r', '\n');
            if (page != "")
            {
                pages.Add(page);
            }

            int numPages = pages.Count;
            int counter = OptionHolder.OptionsPage = OptionHolder.OptionsPage % numPages;

            __result = string.Concat(
                pages[counter].Trim('\r', '\n'),
                "\n\n",
                translate("pressTabForMore"),
                $" ({counter + 1}/{numPages})");

        }

        private static void addChildren(CustomOptionBase option, ref StringBuilder entry, int indentCount = 0)
        {

            string indent = "";

            if (indentCount != 0)
            {
                indent = string.Concat(
                    Enumerable.Repeat("    ", indentCount));
            }

            foreach (var child in option.Children)
            {

                string optionString = CustomOption.OptionToString(child);
                if (optionString != string.Empty)
                {
                    entry.AppendLine(
                        string.Concat(
                            indent,
                            optionString));
                }
                if (indentCount == 0)
                {
                    addChildren(child, ref entry, 0);
                }
                else
                {
                    addChildren(child, ref entry, indentCount + 1);
                }
            }
        }

        private static string translate(string key)
        {
            return Translation.GetString(key);
        }

    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.GetAdjustedNumImpostors))]
    public static class GameOptionsGetAdjustedNumImpostorsPatch
    {
        public static bool Prefix(GameOptionsData __instance, ref int __result)
        {
            __result = PlayerControl.GameOptions.NumImpostors;
            return false;
        }
    }
}
