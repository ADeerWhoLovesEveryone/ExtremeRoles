﻿using Hazel;
using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Module.Ability;
using ExtremeRoles.Module.SystemType;
using ExtremeRoles.Module.SystemType.Roles;
using ExtremeRoles.Module.CustomOption.Factory;


#nullable enable

namespace ExtremeRoles.Roles.Solo.Neutral;

public sealed class Monika :
	SingleRoleBase,
	IRoleAutoBuildAbility,
	IRoleReportHook
{
	public enum Ops
	{
		CanUseVent,
		CanUseSabotage,
		UseOtherButton,
		Range,
	}

    public ExtremeAbilityButton? Button { get; set; }
	private MonikaTrashSystem? trashSystem;
	private MonikaMeetingNumSystem? meetingNumSystem;
	private byte targetPlayer;
	private float range;

	public Monika(): base(
        ExtremeRoleId.Monika,
        ExtremeRoleType.Neutral,
        ExtremeRoleId.Monika.ToString(),
        ColorPalette.MonikaRoseSaumon,
        false, false, false, false)
    { }

	public void CreateAbility()
    {
		this.CreateNormalAbilityButton(
			"monikaPlayerTrash",
			UnityObjectLoader.LoadFromResources(ExtremeRoleId.Monika));
	}

	public bool IsAbilityUse()
	{
		if (this.trashSystem is null)
		{
			return false;
		}

		this.targetPlayer = byte.MaxValue;
		var player = Player.GetClosestPlayerInRange(
			PlayerControl.LocalPlayer, this,
			this.range);

		if (player == null ||
			this.trashSystem.InvalidPlayer(player))
		{
			return false;
		}
		this.targetPlayer = player.PlayerId;

		return
			IRoleAbility.IsCommonUse();
	}

	public bool UseAbility()
    {
		if (this.targetPlayer == byte.MaxValue ||
			this.trashSystem == null)
		{
			return false;
		}

		if (PlayerControl.LocalPlayer != null &&
			ExtremeRoleManager.TryGetRole(targetPlayer, out var role) &&
			role.Id is ExtremeRoleId.Monika)
		{
			// モニカに対して能力を使用したときは殺す
			Player.RpcUncheckMurderPlayer(
				PlayerControl.LocalPlayer.PlayerId,
				this.targetPlayer,
				byte.MaxValue);
			return true;
		}

		this.trashSystem.RpcAddTrash(this.targetPlayer);
		return true;
    }

    protected override void CreateSpecificOption(
        AutoParentSetOptionCategoryFactory factory)
    {
		factory.CreateBoolOption(
			Ops.CanUseVent, false);
		factory.CreateBoolOption(
			Ops.CanUseSabotage, false);
		factory.CreateBoolOption(
			Ops.UseOtherButton, true);
		IRoleAbility.CreateCommonAbilityOption(
            factory);
		factory.CreateFloatOption(
			Ops.Range, 1.3f, 0.1f, 3.0f, 0.1f);
	}

    protected override void RoleSpecificInit()
    {
		var loader = this.Loader;
		this.trashSystem = ExtremeSystemTypeManager.Instance.CreateOrGet<MonikaTrashSystem>(
			ExtremeSystemType.MonikaTrashSystem);

		this.UseVent = loader.GetValue<Ops, bool>(Ops.CanUseVent);
		this.UseSabotage = loader.GetValue<Ops, bool>(Ops.CanUseSabotage);

		if (loader.GetValue<Ops, bool>(Ops.UseOtherButton))
		{
			this.meetingNumSystem = ExtremeSystemTypeManager.Instance.CreateOrGet<MonikaMeetingNumSystem>(
				ExtremeSystemType.MonikaMeetingNumSystem);
		}
		this.range = loader.GetValue<Ops, float>(Ops.Range);
    }

    public void ResetOnMeetingStart()
    {
    }

    public void ResetOnMeetingEnd(NetworkedPlayerInfo? exiledPlayer = null)
    {
        return;
    }

	public void HookReportButton(PlayerControl rolePlayer, NetworkedPlayerInfo reporter)
	{
		if (this.meetingNumSystem is null)
		{
			return;
		}
		byte reporterPlayerId = reporter.PlayerId;
		if (rolePlayer.PlayerId == reporterPlayerId)
		{
			if (!this.meetingNumSystem.TryReduce())
			{
				return;
			}
			rolePlayer.RemainingEmergencies = GameOptionsManager.Instance.currentNormalGameOptions.NumEmergencyMeetings;
		}
		else
		{
			this.meetingNumSystem.RpcReduceTo(reporterPlayerId, false);
		}
	}

	public void HookBodyReport(PlayerControl rolePlayer, NetworkedPlayerInfo reporter, NetworkedPlayerInfo reportBody)
	{ }
}
