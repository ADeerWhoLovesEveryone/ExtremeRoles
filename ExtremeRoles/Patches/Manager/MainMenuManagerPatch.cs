﻿using System;

#if RELEASE
using BepInEx;
#endif
using HarmonyLib;

using TMPro;
using Twitch;

using UnityEngine;

using ExtremeRoles.Extension.UnityEvent;
using ExtremeRoles.Helper;
using ExtremeRoles.Resources;
using ExtremeRoles.Performance;

using MenuButton = ExtremeRoles.Module.CustomMonoBehaviour.MenuButton;
using UnityObject = UnityEngine.Object;

namespace ExtremeRoles.Patches.Manager;
/*
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class MainMenuManagerStartPatch
{
    private static Color discordColor => new Color32(88, 101, 242, byte.MaxValue);

    public static void Prefix(MainMenuManager __instance)
    {

        var template = GameObject.Find("ExitGameButton");
        if (template == null) { return; }

        // Mod ExitButton
        PassiveButton passiveExitButton = template.GetComponent<PassiveButton>();
        passiveExitButton.OnClick.AddListener(
            (UnityEngine.Events.UnityAction)(() => Logging.BackupCurrentLog()));

        // UpdateButton
        GameObject updateButtonObj = UnityObject.Instantiate(template, template.transform);
        UnityObject.Destroy(updateButtonObj.GetComponent<AspectPosition>());
        UnityObject.Destroy(updateButtonObj.GetComponent<ConditionalHide>());
        UnityObject.Destroy(updateButtonObj.GetComponentInChildren<TextTranslatorTMP>());

        MenuButton updateButton = updateButtonObj.AddComponent<MenuButton>();
        updateButton.name = "ExtremeRolesUpdateButton";
        updateButton.transform.localPosition = new Vector3(0.0f, 0.6f, 0.0f);
        updateButton.gameObject.SetActive(true);
        updateButton.AddAction(async () => await Module.Updater.Instance.CheckAndUpdate());
        updateButton.SetText(Translation.GetString("UpdateButton"));

        // DiscordButton
        MenuButton discordButton = UnityObject.Instantiate(
            updateButton, template.transform);
        discordButton.name = "ExtremeRolesDiscordButton";
        discordButton.transform.localPosition = new Vector3(0.0f, 1.2f, 0.0f);
        discordButton.gameObject.SetActive(true);
        discordButton.AddAction(() => Application.OpenURL("https://discord.gg/UzJcfBYcyS"));
        discordButton.SetText("Discord");

        SpriteRenderer buttonSpriteDiscord = discordButton.GetComponent<SpriteRenderer>();
        buttonSpriteDiscord.color = discordButton.Text.color = discordColor;
        discordButton.Button.OnMouseOut.AddListener((Action)delegate
        {
            buttonSpriteDiscord.color = discordButton.Text.color = discordColor;
        });

        if (!Module.Updater.Instance.IsInit)
        {
            TwitchManager man = FastDestroyableSingleton<TwitchManager>.Instance;
            var infoPop = UnityObject.Instantiate(man.TwitchPopup);
            infoPop.TextAreaTMP.fontSize *= 0.7f;
            infoPop.TextAreaTMP.enableAutoSizing = false;
            Module.Updater.Instance.InfoPopup = infoPop;
        }
    }

    public static void Postfix(MainMenuManager __instance)
    {
        FastDestroyableSingleton<ModManager>.Instance.ShowModStamp();

#if RELEASE
        if (!ExtremeRolesPlugin.IgnoreOverrideConsoleDisable.Value &&
            ConsoleManager.ConfigConsoleEnabled.Value)
        {
            ConsoleManager.ConfigConsoleEnabled.Value = false;
            Application.Quit();
            return;
        }
#endif

        var amongUsLogo = GameObject.Find("bannerLogo_AmongUs");
        if (amongUsLogo != null)
        {
            amongUsLogo.transform.localScale *= 0.9f;
            amongUsLogo.transform.position += Vector3.up * 0.25f;
        }

        var exrLogo = new GameObject("bannerLogoExtremeRoles");
        exrLogo.transform.position = Vector3.up;
        var renderer = exrLogo.AddComponent<SpriteRenderer>();
        renderer.sprite = Loader.CreateSpriteFromResources(
            Resources.Path.TitleBurner, 300f);

        if (Module.Prefab.Prop == null || Module.Prefab.Text == null)
        {
            TwitchManager man = DestroyableSingleton<TwitchManager>.Instance;
            Module.Prefab.Prop = UnityObject.Instantiate(man.TwitchPopup);
            UnityObject.DontDestroyOnLoad(Module.Prefab.Prop);
            Module.Prefab.Prop.name = "propForInEx";
            Module.Prefab.Prop.gameObject.SetActive(false);

            Module.Prefab.Text = UnityObject.Instantiate(man.TwitchPopup.TextAreaTMP);
            Module.Prefab.Text.fontSize =
                Module.Prefab.Text.fontSizeMax =
                Module.Prefab.Text.fontSizeMin = 2.25f;
            Module.Prefab.Text.alignment = TextAlignmentOptions.Center;
            UnityObject.DontDestroyOnLoad(Module.Prefab.Text);
            UnityObject.Destroy(Module.Prefab.Text.GetComponent<TextTranslatorTMP>());
            Module.Prefab.Text.gameObject.SetActive(false);
            UnityObject.DontDestroyOnLoad(Module.Prefab.Text);

        }
        Compat.CompatModMenu.CreateMenuButton();

		// CustomRegion.Update();
	}
}
*/
