﻿using System.Reflection;

using UnityEngine;

using HarmonyLib;

using AmongUs.Data;

namespace ExtremeRoles.Patches;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShowerPatch
{
    public static void Postfix(VersionShower __instance)
    {
        var burner = GameObject.Find("bannerLogoExtremeRoles");
        if (burner == null) { return; }

        var modTitle = Object.Instantiate(
            __instance.text, burner.transform);
        modTitle.transform.localPosition = new Vector3(0, -1.4f, 0);
        modTitle.transform.localScale = new Vector3(1.075f, 1.075f, 1.0f);
        modTitle.SetText(
            string.Concat(
                Helper.Translation.GetString("version"),
                Assembly.GetExecutingAssembly().GetName().Version));
        modTitle.alignment = TMPro.TextAlignmentOptions.Center;
		modTitle.fontSize = modTitle.fontSizeMax = modTitle.fontSizeMin = 4.0f;


		var credentials = Object.Instantiate(
            modTitle, modTitle.transform);
        credentials.SetText(
            string.Concat(
                Helper.Translation.GetString("developer"),"yukieiji"));
        credentials.alignment = TMPro.TextAlignmentOptions.Center;
		credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 3.5f;
		credentials.transform.localPosition = new Vector3(0, -0.5f, 0);

        if (DataManager.Settings.Language.CurrentLanguage != SupportedLangs.Japanese)
        {
			var translator = Object.Instantiate(
			credentials, credentials.transform);
			translator.transform.localPosition = new Vector3(0, -0.35f, 0);
			translator.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
			translator.gameObject.SetActive(true);
            translator.SetText(
                string.Concat(
                    Helper.Translation.GetString("langTranslate"),
                    Helper.Translation.GetString("translatorMember")));
            translator.alignment = TMPro.TextAlignmentOptions.Center;
			translator.fontSize = translator.fontSizeMax = translator.fontSizeMin = 3.5f;

			translator.transform.localPosition = new Vector3(0, -0.5f, 0);
            credentials.transform.localScale = new Vector3(0.8f, 0.8f, 1.0f);
        }
        else
        {
            credentials.transform.localScale = new Vector3(0.9f, 0.9f, 1.0f);
        }

    }
}
