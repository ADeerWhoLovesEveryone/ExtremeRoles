﻿using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

using UnityEngine;
using AmongUs.Data;

using ExtremeRoles.GameMode;

using ExtremeRoles.Helper;
using ExtremeRoles.Performance;
using ExtremeRoles.Performance.Il2Cpp;
using ExtremeRoles.Extension.Manager;
using ExtremeRoles.Compat;
using ExtremeRoles.Compat.Interface;

namespace ExtremeRoles.Patches.Manager;

[HarmonyPatch]
public static class GameStartManagerPatch
{
    private const float kickTime = 30f;
    private const float timerMaxValue = 600f;
    private const string errorColorPlaceHolder = "<color=#FF0000FF>{0}\n</color>";

    private static bool isCustomServer;

    private static float timer;
    private static float kickingTimer;

    private static bool isVersionSent;
    private static bool update = false;

    private static string currentText = "";
    private static bool prevOptionValue;
    private static TMPro.TextMeshPro customShowText;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public static bool BeginGamePrefix()
    {
        if (!AmongUsClient.Instance.AmHost) { return true; }

        foreach (InnerNet.ClientData client in AmongUsClient.Instance.allClients.GetFastEnumerator())
        {
            if (client.Character == null) continue;
            var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
            if (dummyComponent != null && dummyComponent.enabled)
            {
                continue;
            }

            if (!ExtremeRolesPlugin.ShipState.TryGetPlayerVersion(
                client.Id, out Version clientVer))
            {
                return false;
            }
            int diff = Assembly.GetExecutingAssembly().GetName().Version.CompareTo(
                clientVer);
            if (diff != 0)
            {
                return false;
            }
        }

        InfoOverlay.Instance.Hide();
        // ホストはここでオプションを読み込み
        OptionManager.Load();

        if (ExtremeGameModeManager.Instance.ShipOption.IsRandomMap)
        {
            // 0 = Skeld
            // 1 = Mira HQ
            // 2 = Polus
            // 3 = Dleks - deactivated
            // 4 = Airship
			// 5 = Fungle

            var rng = RandomGenerator.GetTempGenerator();

            List<byte> possibleMaps = new List<byte>() { 0, 1, 2, 4, 5 };

			foreach (var mod in CompatModManager.Instance.LoadedMod.Values)
			{
				if (mod is IMapMod mapMod)
				{
					possibleMaps.Add(mapMod.MapId);
				}
			}

            byte mapId = possibleMaps[
                rng.Next(possibleMaps.Count)];

            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.ShareMapId))
            {
                caller.WriteByte(mapId);
            }
            RPCOperator.ShareMapId(mapId);
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public static void StartPrefix(GameStartManager __instance)
    {
        // ロビーコードコピー
        GUIUtility.systemCopyBuffer = InnerNet.GameCode.IntToGameName(
            AmongUsClient.Instance.GameId);

        isVersionSent = false;
        timer = timerMaxValue;
        kickingTimer = 0f;
        isCustomServer = FastDestroyableSingleton<ServerManager>.Instance.IsCustomServer();

        prevOptionValue = DataManager.Settings.Gameplay.StreamerMode;

        // 値リセット
        RPCOperator.Initialize();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public static void StartPostfix(GameStartManager __instance)
    {
        updateText(__instance, DataManager.Settings.Gameplay.StreamerMode);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static void UpdatePrefix(GameStartManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !GameData.Instance) { return; } // Not host or no instance
        update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static void UpdatePostfix(GameStartManager __instance)
    {
        if (PlayerControl.LocalPlayer != null && !isVersionSent)
        {
            isVersionSent = true;
            GameSystem.ShareVersion();
        }

        // ルームコード設定

        bool isStreamerMode = DataManager.Settings.Gameplay.StreamerMode;

        if (isStreamerMode != prevOptionValue)
        {
            prevOptionValue = isStreamerMode;
            updateText(__instance, isStreamerMode);
        }

        // Instanceミス
        if (!GameData.Instance) { return; }

        var localGameVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var state = ExtremeRolesPlugin.ShipState;

        // ホスト以外
        if (!AmongUsClient.Instance.AmHost)
        {
            if (!state.TryGetPlayerVersion(
                AmongUsClient.Instance.HostId, out Version hostVersion) ||
                localGameVersion.CompareTo(hostVersion) != 0)
            {
                kickingTimer += Time.deltaTime;
                if (kickingTimer > kickTime)
                {
                    kickingTimer = 0;
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                    SceneChanger.ChangeScene("MainMenu");
                }

                __instance.GameStartText.text = Tr.GetString(
					"errorDiffHostVersion",
                    Mathf.Round(kickTime - kickingTimer));
                __instance.GameStartText.transform.localPosition =
                    __instance.StartButton.transform.localPosition + Vector3.up * 2;
            }
            else
            {
                __instance.GameStartText.transform.localPosition =
                    __instance.StartButton.transform.localPosition;
                if (__instance.startState != GameStartManager.StartingStates.Countdown)
                {
                    __instance.GameStartText.text = string.Empty;
                }
            }
            return;
        }

        bool blockStart = false;
        string message = string.Format(
            errorColorPlaceHolder,
            Tr.GetString("errorCannotGameStart"));
        foreach (InnerNet.ClientData client in
            AmongUsClient.Instance.allClients.GetFastEnumerator())
        {
            if (client.Character == null) { continue; }

            var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
            if (dummyComponent != null && dummyComponent.enabled)
            {
                continue;
            }
            else if (!state.TryGetPlayerVersion(client.Id, out Version clientVer))
            {
                blockStart = true;
                message += string.Format(
                    errorColorPlaceHolder,
                    $"{client.Character.Data.PlayerName}:  {Tr.GetString("errorNotInstalled")}");
            }
            else
            {
                int diff = localGameVersion.CompareTo(clientVer);
                if (diff > 0)
                {
                    message += string.Format(
                        errorColorPlaceHolder,
                        $"{client.Character.Data.PlayerName}:  {Tr.GetString("errorOldInstalled")}");
                    blockStart = true;
                }
                else if (diff < 0)
                {
                    message += string.Format(
                        errorColorPlaceHolder,
                        $"{client.Character.Data.PlayerName}:  {Tr.GetString("errorNewInstalled")}");
                    blockStart = true;
                }
            }
        }

		if (blockStart)
        {
			if (__instance.StartButtonGlyph != null)
			{
				__instance.StartButtonGlyph.SetColor(Palette.DisabledClear);
			}
			__instance.StartButton.SetButtonEnableState(false);

			__instance.GameStartText.text = message;
            __instance.GameStartText.transform.localPosition =
                __instance.StartButton.transform.localPosition + Vector3.up * 2;
        }
        else
        {
			bool isPlayerOk = __instance.LastPlayerCount >= __instance.MinPlayers;

			if (__instance.StartButtonGlyph != null)
			{
				__instance.StartButtonGlyph.SetColor(isPlayerOk ?
					Palette.EnabledColor : Palette.DisabledClear);
			}

			__instance.StartButton.SetButtonEnableState(isPlayerOk);
			__instance.GameStartText.transform.localPosition =
                __instance.StartButton.transform.localPosition;
        }

		if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !isCustomServer)
        {
            // プレイヤーカウントアップデート
            if (update)
            {
                currentText = __instance.PlayerCounter.text;
            }

            timer = Mathf.Max(0f, timer -= Time.deltaTime);
            int minutes = (int)timer / 60;
            int seconds = (int)timer % 60;

            __instance.PlayerCounter.text = $"{currentText}\n({minutes:00}:{seconds:00})";
		}
    }

    private static void updateText(
        GameStartManager instance,
        bool isStreamerMode)
    {
        var button = GameObject.Find("Main Camera/Hud/GameStartManager/GameRoomButton");
        if (button == null) { return; }

        var info = button.transform.FindChild("GameRoomInfo_TMP");
        if (info == null) { return; }

        if (customShowText == null)
        {
            customShowText = UnityEngine.Object.Instantiate(
                instance.GameStartText, button.transform);
            customShowText.name = "StreamerModeCustomMessage";
            customShowText.transform.localPosition = new Vector3(0.0f, -0.32f, 0.0f);
            customShowText.text = $"<size=60%>{ClientOption.Instance.StreamerModeReplacementText.Value}</size>";
            customShowText.gameObject.SetActive(false);
        }

        if (isStreamerMode)
        {
            button.transform.localPosition = new Vector3(0.0f, -0.85f, 0.0f);
            info.localPosition = new Vector3(0.0f, -0.08f, 0.0f);
            customShowText.gameObject.SetActive(true);
        }
        else
        {
            button.transform.localPosition = new Vector3(0.0f, -0.958f, 0.0f);
            info.localPosition = new Vector3(0.0f, -0.229f, 0.0f);
            customShowText.gameObject.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.SetStartCounter))]
public static class GameStartManagerSetStartCounterPatch
{
    public static void Postfix(GameStartManager __instance, sbyte sec)
    {
        if (sec > 0)
        {
            __instance.startState = GameStartManager.StartingStates.Countdown;
        }

        if (sec <= 0)
        {
            __instance.startState = GameStartManager.StartingStates.NotStarting;
        }
    }
}
