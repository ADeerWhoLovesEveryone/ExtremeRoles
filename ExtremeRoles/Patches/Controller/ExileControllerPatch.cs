﻿using System;
using System.Linq;
using HarmonyLib;

using AmongUs.GameOptions;

using ExtremeRoles.Compat;
using ExtremeRoles.Compat.ModIntegrator;
using ExtremeRoles.GameMode;
using ExtremeRoles.GameMode.Option.ShipGlobal.Sub;
using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAssign;
using ExtremeRoles.Module.ExtremeShipStatus;
using ExtremeRoles.Module.SystemType.OnemanMeetingSystem;
using ExtremeRoles.GhostRoles;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Roles.API.Extension.State;
using ExtremeRoles.Performance;

using Il2CppObject = Il2CppSystem.Object;
using Assassin = ExtremeRoles.Roles.Combination.Assassin;


#nullable enable

namespace ExtremeRoles.Patches.Controller;

[HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
public static class ExileControllerBeginePatch
{
    private const string TransKeyBase = "ExileText";

	/* JAJPs
	[Info   :Extreme Roles] TransKey:ExileTextSP    Value:{0}がインポスターだった。
	[Info   :Extreme Roles] TransKey:ExileTextSN    Value:{0}はインポスターではなかった。
	[Info   :Extreme Roles] TransKey:ExileTextPP    Value:{0}はインポスターだった。
	[Info   :Extreme Roles] TransKey:ExileTextPN    Value:{0}はインポスターではなかった。
	[Info   :Extreme Roles] TransKey:NoExileSkip    Value:誰も追放されなかった。（投票スキップ）
	[Info   :Extreme Roles] TransKey:NoExileTie    Value:誰も追放されなかった。（同数投票）
	[Info   :Extreme Roles] TransKey:ExileTextNonConfirm    Value:{0}が追放された。
	[Info   :Extreme Roles] TransKey:ImpostorsRemainS    Value:インポスターが{0}人残っている。
	[Info   :Extreme Roles] TransKey:ImpostorsRemainP    Value:インポスターが{0}人残っている。
	*/

	[HarmonyPrefix, HarmonyPriority(Priority.Last)]
    public static bool Prefix(
        ExileController __instance,
        [HarmonyArgument(0)] ExileController.InitProperties init)
    {
		if (CompatModManager.Instance.IsModMap<SubmergedIntegrator>())
		{
			return true;
		}
		else
		{
			return PrefixRun(__instance, init);
		}
    }

    public static void Postfix(ExileController __instance)
    {
        if (!MeetingReporter.IsExist ||
			OnemanMeetingSystemManager.IsActive) { return; }

		string reports = MeetingReporter.Instance.GetMeetingEndReport();

		if (string.IsNullOrEmpty(reports)) { return; }

        TMPro.TextMeshPro infoText = UnityEngine.Object.Instantiate(
            __instance.ImpostorText,
            __instance.Text.transform);

        float textOffset = GameOptionsManager.Instance.CurrentGameOptions.GetBool(
            BoolOptionNames.ConfirmImpostor) ? -0.4f : -0.2f;

        infoText.transform.localPosition += new UnityEngine.Vector3(0f, textOffset, 0f);
        infoText.gameObject.SetActive(true);
        infoText.text = reports;

        __instance.StartCoroutine(
            Effects.Bloop(0.25f, infoText.transform, 1f, 0.5f));
    }

	public static bool PrefixRun(
		ExileController __instance,
		ExileController.InitProperties init)
	{
		if (!RoleAssignState.Instance.IsRoleSetUpEnd) { return true; }

		var state = ExtremeRolesPlugin.ShipState;
		__instance.initData = init;
		if (state.AssassinMeetingTrigger)
		{
			assassinMeetingEndBegin(__instance, state);
			return false;
		}
		else if (init.confirmImpostor)
		{
			var shipOption = ExtremeGameModeManager.Instance.ShipOption;
			confirmExile(
				__instance, shipOption.Exile);
			return false;
		}
		return true;
	}

    private static void assassinMeetingEndBegin(
        ExileController instance, ExtremeShipStatus state)
    {
		instance.initData.confirmImpostor = true;
		instance.initData.voteTie = false;

		setExiledTarget(instance);
        NetworkedPlayerInfo? player = GameData.Instance.GetPlayerById(
            state.IsMarinPlayerId);
		if (player == null)
		{
			return;
		}

        string transKey = state.IsAssassinateMarin ?
            "assassinateMarinSucsess" : "assassinateMarinFail";
        string printStr = $"{player.PlayerName}{Tr.GetString(transKey)}";

        if (instance.Player)
        {
            instance.Player.gameObject.SetActive(false);
        }
        instance.completeString = printStr;
        instance.ImpostorText.text = string.Empty;

        instance.StartCoroutine(instance.Animate());
    }

    private static void confirmExile(
        ExileController instance,
        in ExileOption option)
    {
        setExiledTarget(instance);
        var transController = FastDestroyableSingleton<TranslationController>.Instance;

        var allPlayer = GameData.Instance.AllPlayers.ToArray();

		var init = instance.initData;
		bool invalidExiled = init != null && init.outfit != null;

		var alivePlayers = allPlayer.Where(
            x =>
            {
                return
                    (
                        (invalidExiled && x.PlayerId != init!.networkedPlayer.PlayerId) ||
						!invalidExiled
                    ) && !x.IsDead && !x.Disconnected;
            });
        var allRoles = ExtremeRoleManager.GameRole;

        int aliveImpNum = Enumerable.Count(
            alivePlayers,
            (NetworkedPlayerInfo p) =>
            {
                return allRoles[p.PlayerId].IsImpostor();
            });
        int aliveCrewNum = Enumerable.Count(
            alivePlayers,
            (NetworkedPlayerInfo p) =>
            {
                return allRoles[p.PlayerId].IsCrewmate();
            });
        int aliveNeutNum = Enumerable.Count(
            alivePlayers,
            (NetworkedPlayerInfo p) =>
            {
                return allRoles[p.PlayerId].IsNeutral();
            });

        string completeString = string.Empty;

		var mode = option.Mode;
        if (invalidExiled)
        {
            string playerName = init!.outfit!.PlayerName;
            var exiledPlayerRole = allRoles[init!.networkedPlayer.PlayerId];
            switch (mode)
            {
                case ConfirmExileMode.AllTeam:
                    string team = Tr.GetString(exiledPlayerRole.Team.ToString());
                    completeString = option.IsConfirmRole ?
						Tr.GetString("ExileTextAllTeamWithRole", playerName, team, exiledPlayerRole.GetColoredRoleName()) :
						Tr.GetString("ExileTextAllTeam", playerName, team);
                    break;
                default:
                    completeString = getCompleteString(
                        playerName, exiledPlayerRole, in option);
                    break;
            }

			instance.Player.UpdateFromPlayerOutfit(init!.outfit, PlayerMaterial.MaskType.Exile, false, false, (Il2CppSystem.Action)(() =>
			{
				SkinViewData skinViewData;
				if (GameManager.Instance != null)
				{
					skinViewData = ShipStatus.Instance.CosmeticsCache.GetSkin(instance.initData.outfit.SkinId);
				}
				else
				{
					skinViewData = instance.Player.GetSkinView();
				}
				if (GameManager.Instance != null &&
					!FastDestroyableSingleton<HatManager>.Instance.CheckLongModeValidCosmetic(
					init!.outfit!.SkinId, instance.Player.GetIgnoreLongMode()))
				{
					skinViewData = ShipStatus.Instance.CosmeticsCache.GetSkin("skin_None");
				}
				if (instance.useIdleAnim)
				{
					instance.Player.FixSkinSprite(skinViewData.IdleFrame);
					return;
				}
				instance.Player.FixSkinSprite(skinViewData.EjectFrame);
			}), false);
			instance.Player.ToggleName(false);
			if (!instance.useIdleAnim)
			{
				instance.Player.SetCustomHatPosition(instance.exileHatPosition);
				instance.Player.SetCustomVisorPosition(instance.exileVisorPosition);
			}
		}
        else if (init != null)
        {
            completeString = transController.GetString(
                init.voteTie ? StringNames.NoExileTie : StringNames.NoExileSkip,
                Array.Empty<Il2CppObject>());
            instance.Player.gameObject.SetActive(false);
        }

        instance.completeString = completeString;
        instance.ImpostorText.text = mode switch
        {
            ConfirmExileMode.Impostor => transController.GetString(
                aliveImpNum == 1 ? StringNames.ImpostorsRemainS : StringNames.ImpostorsRemainP,
                [ aliveImpNum ]),

            ConfirmExileMode.Crewmate => Tr.GetString(
                aliveCrewNum == 1 ? "CrewmateRemainS" : "CrewmateRemainP", aliveCrewNum),

            ConfirmExileMode.Neutral => Tr.GetString(
                aliveNeutNum == 1 ?  "NeutralRemainS" : "NeutralRemainP", aliveNeutNum),

            ConfirmExileMode.AllTeam => Tr.GetString(
				"AllTeamAlive", aliveCrewNum, aliveImpNum, aliveNeutNum),

            _ => string.Empty
        };
        instance.StartCoroutine(instance.Animate());
    }

    private static string getSuffix(
        bool isExiledSameMode,
        bool isModeTeamContain)
    {
        string modeTeamSuffix;

        if (isExiledSameMode && isModeTeamContain)
        {
            modeTeamSuffix = "PP";
        }
        else if (isExiledSameMode)
        {
            modeTeamSuffix = "SP";
        }
        else if (isModeTeamContain)
        {
            modeTeamSuffix = "PN";
        }
        else
        {
            modeTeamSuffix = "SN";
        }

        return modeTeamSuffix;
    }

    private static string getCompleteString(
        string playerName,
        SingleRoleBase exiledPlayerRole,
		in ExileOption option)
    {
		var mode = option.Mode;

        string teamSuffix = mode switch
        {
            ConfirmExileMode.Crewmate => "Crew",
            ConfirmExileMode.Neutral => "Neut",
            _ => string.Empty,
        };

        var allPlayer = GameData.Instance.AllPlayers.ToArray();
        var allRoles = ExtremeRoleManager.GameRole;

        bool isExiledSameMode = false;
        int modeTeamAlive = 0;
        switch (mode)
        {
            case ConfirmExileMode.Impostor:
                modeTeamAlive = allPlayer.Count(
					(NetworkedPlayerInfo p) =>
						p != null &&
						ExtremeRoleManager.TryGetRole(p.PlayerId, out var role) &&
						role!.IsImpostor());
                isExiledSameMode = exiledPlayerRole.IsImpostor();
                break;
            case ConfirmExileMode.Crewmate:
                modeTeamAlive = allPlayer.Count(
					(NetworkedPlayerInfo p) =>
						p != null &&
						ExtremeRoleManager.TryGetRole(p.PlayerId, out var role) &&
						role!.IsCrewmate());
                isExiledSameMode = exiledPlayerRole.IsCrewmate();
                break;
            case ConfirmExileMode.Neutral:
                modeTeamAlive = allPlayer.Count(
					(NetworkedPlayerInfo p) =>
						p != null &&
						ExtremeRoleManager.TryGetRole(p.PlayerId, out var role) &&
						role!.IsNeutral());
                isExiledSameMode = exiledPlayerRole.IsNeutral();
                break;
            default:
                break;
        };
        string suffix = getSuffix(isExiledSameMode, modeTeamAlive > 1);
        string transKey = $"{TransKeyBase}{suffix}{teamSuffix}";

        if (Enum.TryParse(transKey, out StringNames sn))
        {
            return FastDestroyableSingleton<TranslationController>.Instance.GetString(
                sn, [ playerName ]);
        }
        else
        {
            return
                option.IsConfirmRole ?
                Tr.GetString(
					$"{transKey}WithRole",
                    playerName,
                    exiledPlayerRole.GetColoredRoleName()
                ) :
				Tr.GetString(
					transKey, playerName);
        }
    }

    private static void setExiledTarget(
        ExileController instance)
    {
        if (instance.specialInputHandler != null)
        {
            instance.specialInputHandler.disableVirtualCursor = true;
        }
        ExileController.Instance = instance;
        ControllerManager.Instance.CloseAndResetAll();

        instance.Text.gameObject.SetActive(false);
        instance.Text.text = string.Empty;
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
public static class ExileControllerReEnableGameplayPatch
{
    public static void Postfix()
    {
        ReEnablePostfix();
    }

    public static void ReEnablePostfix()
    {
        if (ExtremeRoleManager.GameRole.Count == 0) { return; }

        MeetingReporter.Reset();

        var role = ExtremeRoleManager.GetLocalPlayerRole();

        if (!role.TryGetKillCool(out float killCool)) { return; }

        PlayerControl.LocalPlayer.SetKillTimer(killCool);
    }

}

[HarmonyPatch]
public static class ExileControllerWrapUpPatch
{

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    public static class BaseExileControllerPatch
    {
        public static void Prefix()
        {
            WrapUpPrefix();
        }
        public static void Postfix(ExileController __instance)
        {
            WrapUpPostfix(__instance.initData.networkedPlayer);
        }
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    public static class AirshipExileControllerPatch
    {
        public static void Prefix()
        {
            WrapUpPrefix();
        }
        public static void Postfix(AirshipExileController __instance)
        {
            WrapUpPostfix(__instance.initData.networkedPlayer);
        }
    }

    public static void WrapUpPostfix(NetworkedPlayerInfo? exiled)
    {
        InfoOverlay.Instance.IsBlock = false;
        Meeting.Hud.MeetingHudSelectPatch.SetSelectBlock(false);

        if (ExtremeRoleManager.GameRole.Count == 0) { return; }

        var state = ExtremeRolesPlugin.ShipState;

        if (state.TryGetDeadAssasin(out byte playerId) &&
			ExtremeRoleManager.TryGetSafeCastedRole(playerId, out Assassin? assasin))
        {
            assasin!.ExiledAction(
				Helper.Player.GetPlayerControlById(playerId));
        }


        var role = ExtremeRoleManager.GetLocalPlayerRole();
        if (role is IRoleAbility abilityRole)
        {
            abilityRole.Button.OnMeetingEnd();
        }
        if (role is IRoleResetMeeting resetRole)
        {
            resetRole.ResetOnMeetingEnd(exiled);
        }
        if (role is MultiAssignRoleBase multiAssignRole)
        {
            if (multiAssignRole.AnotherRole is IRoleAbility abilityMultiAssignRole)
            {
                abilityMultiAssignRole.Button.OnMeetingEnd();
            }
            if (multiAssignRole.AnotherRole is IRoleResetMeeting resetMultiAssignRole)
            {
                resetMultiAssignRole.ResetOnMeetingEnd(exiled);
            }
        }

        var ghostRole = ExtremeGhostRoleManager.GetLocalPlayerGhostRole();
        if (ghostRole != null)
        {
            ghostRole.ResetOnMeetingEnd();
        }
    }

    public static void WrapUpPrefix()
    {
        if (ExtremeRolesPlugin.ShipState.AssassinMeetingTrigger)
        {
            ExtremeRolesPlugin.ShipState.AssassinMeetingTriggerOff();
        }
    }
}
