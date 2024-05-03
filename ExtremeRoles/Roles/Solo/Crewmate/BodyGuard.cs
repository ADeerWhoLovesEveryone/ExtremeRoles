﻿using System;
using System.Collections.Generic;

using UnityEngine;
using Hazel;
using TMPro;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.ButtonAutoActivator;
using ExtremeRoles.Module.ExtremeShipStatus;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;
using ExtremeRoles.Module.Ability;
using ExtremeRoles.Module.Ability.ModeSwitcher;
using ExtremeRoles.Module.Ability.Behavior;
using ExtremeRoles.Module.Ability.Behavior.Interface;

#nullable enable

namespace ExtremeRoles.Roles.Solo.Crewmate;

public sealed class BodyGuard :
    SingleRoleBase,
    IRoleAbility,
    IRoleMeetingButtonAbility,
    IRoleUpdate,
    IRoleSpecialReset
{
	public sealed class BodyGuardAbilityBehavior : BehaviorBase
    {
        public int AbilityCount { get; private set; }

		private bool isUpdate;
		private Func<bool> featShield;
        private Func<bool> canUse;
        private Action resetShield;
        private Func<bool> resetModeCheck;

        private bool isReset;

        private readonly GraphicSwitcher<BodyGuardAbilityMode> switcher;
#pragma warning disable CS8618
		private TextMeshPro abilityCountText;

		public BodyGuardAbilityBehavior(
            GraphicMode<BodyGuardAbilityMode> featShieldMode,
            GraphicMode<BodyGuardAbilityMode> resetMode,
            Func<bool> featShield,
            Action resetShield,
            Func<bool> canUse,
            Func<bool> resetModeCheck) : base(
                resetMode.Graphic.Text,
                resetMode.Graphic.Img)
        {
            this.resetShield = resetShield;
            this.featShield = featShield;
            this.canUse = canUse;
            this.resetModeCheck = resetModeCheck;

            this.switcher = new GraphicSwitcher<BodyGuardAbilityMode>(this, resetMode, featShieldMode);
        }
#pragma warning restore CS8618
		public void SetAbilityCount(int newAbilityNum)
        {
            this.AbilityCount = newAbilityNum;
            this.isUpdate = true;
            updateAbilityCountText();
        }

        public override void AbilityOff()
        { }

        public override void ForceAbilityOff()
        { }

        public override void Initialize(ActionButton button)
        {
            var coolTimerText = button.cooldownTimerText;

            this.abilityCountText = UnityEngine.Object.Instantiate(
                coolTimerText, coolTimerText.transform.parent);
            this.abilityCountText.enableWordWrapping = false;
            this.abilityCountText.transform.localScale = Vector3.one * 0.5f;
            this.abilityCountText.transform.localPosition += new Vector3(-0.05f, 0.65f, 0);
            updateAbilityCountText();
        }

        public override bool IsCanAbilityActiving() => true;

        public override bool IsUse() =>
            (this.AbilityCount > 0 && this.canUse.Invoke()) ||
			this.isReset;

        public override bool TryUseAbility(
            float timer, AbilityState curState, out AbilityState newState)
        {
            newState = curState;

            if (timer > 0 ||
                curState != AbilityState.Ready)
            {
                return false;
            }

            if (this.AbilityCount <= 0 || this.isReset)
            {
                this.resetShield.Invoke();
                newState = AbilityState.CoolDown;
            }
            else if (
                this.AbilityCount > 0 &&
                this.featShield.Invoke())
            {
                newState = AbilityState.CoolDown;
                reduceAbilityCount();
            }

            return true;
        }

        public override AbilityState Update(AbilityState curState)
        {
            this.isReset = this.resetModeCheck.Invoke();
            if (this.isReset || this.AbilityCount <= 0)
            {
                this.abilityCountText.gameObject.SetActive(false);
                this.switcher.Switch(BodyGuardAbilityMode.Reset);
            }
            else
            {
                this.abilityCountText.gameObject.SetActive(true);
                this.switcher.Switch(BodyGuardAbilityMode.FeatShield);
            }

            if (this.isUpdate)
            {
                this.isUpdate = false;
                return AbilityState.CoolDown;
            }

            return this.AbilityCount <= 0 && !this.isReset ? AbilityState.None : curState;
        }

        private void updateAbilityCountText()
        {
            this.abilityCountText.text = string.Format(
                Translation.GetString(ICountBehavior.DefaultButtonCountText),
                this.AbilityCount);
        }

        private void reduceAbilityCount()
        {
            --this.AbilityCount;
            if (this.abilityCountText != null)
            {
                updateAbilityCountText();
            }
        }

    }

	private sealed class ShildFeatedPlayer
	{
		private List<(byte, byte)> shield = new List<(byte, byte)>();

		public ShildFeatedPlayer()
		{
			Clear();
		}

		public void Clear()
		{
			this.shield.Clear();
		}

		public void Add(byte rolePlayerId, byte targetPlayerId)
		{
			this.shield.Add((rolePlayerId, targetPlayerId));
		}

		public bool IsGuard(byte checkRolePlayerId)
		{
			foreach (var (rolePlayerId, _) in shield)
			{
				if (rolePlayerId == checkRolePlayerId) { return true; }
			}
			return false;
		}

		public void Remove(byte removeRolePlayerId)
		{
			List<(byte, byte)> remove = new List<(byte, byte)>();

			foreach (var (rolePlayerId, targetPlayerId) in shield)
			{
				if (rolePlayerId != removeRolePlayerId) { continue; }
				remove.Add((rolePlayerId, targetPlayerId));
			}

			foreach (var val in remove)
			{
				this.shield.Remove(val);
			}

		}

		public bool TryGetBodyGuardPlayerId(
			byte targetPlayerId, out byte bodyGuardPlayerId)
		{

			bodyGuardPlayerId = default(byte);
			if (this.shield.Count == 0) { return false; }

			foreach (var (rolePlayerId, shieldPlayerId) in this.shield)
			{
				if (shieldPlayerId == targetPlayerId)
				{
					bodyGuardPlayerId = rolePlayerId;
					return true;
				}
			}
			return false;
		}
		public bool IsShielding(byte rolePlayerId, byte targetPlayerId)
		{
			if (this.shield.Count == 0) { return false; }
			return this.shield.Contains((rolePlayerId, targetPlayerId));
		}
	}

	public enum BodyGuardOption
    {
        ShieldRange,
        FeatMeetingAbilityTaskGage,
        FeatMeetingReportTaskGage,
        IsReportPlayerName,
        ReportPlayerMode,
        IsBlockMeetingKill,
    }

    public enum BodyGuardRpcOps : byte
    {
        FeatShield,
        ResetShield,
        CoverDead,
        AwakeMeetingReport
    }

    public enum BodyGuardReportPlayerNameMode
    {
        GuardedPlayerNameOnly,
        BodyGuardPlayerNameOnly,
        BothPlayerName,
    }

    public enum BodyGuardAbilityMode
    {
        FeatShield,
        Reset
    }

    public static bool IsBlockMeetingKill { get; private set; } = true;

    private byte targetPlayer = byte.MaxValue;

    private int shildNum;
    private float shieldRange;

    private bool awakeMeetingAbility;
    private float meetingAbilityTaskGage;
    private bool awakeMeetingReport;
    private float meetingReportTaskGage;

    private bool isReportWithPlayerName;
    private BodyGuardReportPlayerNameMode reportMode;

    private TextMeshPro? meetingText;

    private static ShildFeatedPlayer shilded = new ShildFeatedPlayer();

#pragma warning disable CS8618
	public ExtremeAbilityButton Button { get; set; }
	private Sprite shildButtonImage;

	public BodyGuard() : base(
        ExtremeRoleId.BodyGuard,
        ExtremeRoleType.Crewmate,
        ExtremeRoleId.BodyGuard.ToString(),
        ColorPalette.BodyGuardOrange,
        false, true, false, false)
    { }
#pragma warning restore CS8618

	public static void ResetAllShild()
    {
        shilded.Clear();
    }

    public static void Ability(ref MessageReader reader)
    {
        BodyGuardRpcOps ops = (BodyGuardRpcOps)reader.ReadByte();
        switch (ops)
        {
            case BodyGuardRpcOps.FeatShield:
                byte featBodyGuardPlayerId = reader.ReadByte();
                byte targetPlayerId = reader.ReadByte();
                featShield(featBodyGuardPlayerId, targetPlayerId);
                break;
            case BodyGuardRpcOps.ResetShield:
                resetShield(reader.ReadByte());
                break;
            case BodyGuardRpcOps.CoverDead:
                byte killerPlayerId = reader.ReadByte();
                byte prevTargetPlayerId = reader.ReadByte();
                byte targetBodyGuardPlayerId = reader.ReadByte();
                coverDead(killerPlayerId, prevTargetPlayerId, targetBodyGuardPlayerId);
                break;
            case BodyGuardRpcOps.AwakeMeetingReport:
                awakeReportMeeting(reader.ReadByte());
                break;
            default:
                break;
        }

    }

    public static bool TryRpcKillGuardedBodyGuard(byte killerPlayerId, byte targetPlayerId)
    {
        if (!TryGetShiledPlayerId(targetPlayerId, out byte bodyGuardPlayerId))
        {
            return false;
        }

        return rpcTryKillBodyGuard(killerPlayerId, targetPlayerId, bodyGuardPlayerId);
    }

    public static bool TryGetShiledPlayerId(
        byte targetPlayerId, out byte bodyGuardPlayerId)
    {
        return shilded.TryGetBodyGuardPlayerId(targetPlayerId, out bodyGuardPlayerId);
    }

    private static bool rpcTryKillBodyGuard(
        byte killerPlayerId, byte prevTargetPlayerId, byte targetBodyGuard)
    {
        PlayerControl bodyGuardPlayer = Player.GetPlayerControlById(targetBodyGuard);
        if (bodyGuardPlayer == null ||
            bodyGuardPlayer.Data == null ||
            bodyGuardPlayer.Data.IsDead ||
            bodyGuardPlayer.Data.Disconnected)
        {
            return false;
        }

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.BodyGuardAbility))
        {
            caller.WriteByte((byte)BodyGuardRpcOps.CoverDead);
            caller.WriteByte(killerPlayerId);
            caller.WriteByte(prevTargetPlayerId);
            caller.WriteByte(targetBodyGuard);
        }
        coverDead(killerPlayerId, prevTargetPlayerId, targetBodyGuard);
        return true;
    }

    private static void featShield(byte rolePlayerId, byte targetPlayer)
    {
        shilded.Add(rolePlayerId, targetPlayer);
    }

    private static void resetShield(byte playerId)
    {
        shilded.Remove(playerId);
    }

    private static void coverDead(
        byte killerPlayerId, byte prevTargetPlayerId, byte targetBodyGuard)
    {
        if (targetBodyGuard == CachedPlayerControl.LocalPlayer.PlayerId)
        {
            Sound.PlaySound(Sound.Type.GuardianAngleGuard, 0.6f);
        }

        // 必ずテレポートしないキル
        RPCOperator.UncheckedMurderPlayer(
            killerPlayerId, targetBodyGuard, byte.MinValue);

        PlayerControl bodyGuardPlayer = Player.GetPlayerControlById(targetBodyGuard);

        if (bodyGuardPlayer == null ||
            bodyGuardPlayer.Data == null ||
            !bodyGuardPlayer.Data.IsDead || // 死んでないつまり守護天使に守られた
            bodyGuardPlayer.Data.Disconnected)
        {
            return;
        }

        ExtremeRolesPlugin.ShipState.ReplaceDeadReason(
            targetBodyGuard, ExtremeShipStatus.PlayerStatus.Martyrdom);

        BodyGuard bodyGuard = ExtremeRoleManager.GetSafeCastedRole<BodyGuard>(
            targetBodyGuard);

        if (bodyGuard == null ||
            !bodyGuard.awakeMeetingReport) { return; }

        var prevTargetPlayer = Player.GetPlayerControlById(prevTargetPlayerId);
        string reportStr = getReportStrings(
            bodyGuard,
            bodyGuardPlayer.Data.DefaultOutfit.PlayerName,
            prevTargetPlayer ?
                prevTargetPlayer.Data.DefaultOutfit.PlayerName :
                string.Empty);

		MeetingReporter.Instance.AddMeetingChatReport(reportStr);
	}

    private static void awakeReportMeeting(byte bodyGuardPlayerId)
    {
        BodyGuard bodyGuard = ExtremeRoleManager.GetSafeCastedRole<BodyGuard>(
            bodyGuardPlayerId);
        if (bodyGuard != null)
        {
            bodyGuard.awakeMeetingReport = true;
        }
    }

    private static string getReportStrings(
        BodyGuard bodyGuard,
        string bodyGuardPlayerName,
        string guardedPlayerName)
    {

        string defaultReport = Translation.GetString("martyrdomReport");

        if (!bodyGuard.isReportWithPlayerName)
        {
            return defaultReport;
        }

        return bodyGuard.reportMode switch
        {
            BodyGuardReportPlayerNameMode.GuardedPlayerNameOnly =>
                string.Format(
                    Translation.GetString("martyrdomReportWithGurdedPlayer"),
                    bodyGuardPlayerName),
            BodyGuardReportPlayerNameMode.BodyGuardPlayerNameOnly =>
                 string.Format(
                     Translation.GetString("martyrdomReportWithBodyGurdPlayer"),
                     bodyGuardPlayerName),
            BodyGuardReportPlayerNameMode.BothPlayerName =>
                string.Format(
                    Translation.GetString("martyrdomReportWithBoth"),
                    bodyGuardPlayerName, guardedPlayerName),
            _ => defaultReport
        };
    }

    public override string GetRolePlayerNameTag(
        SingleRoleBase targetRole, byte targetPlayerId)
    {
        if (shilded.IsShielding(
			CachedPlayerControl.LocalPlayer.PlayerId, targetPlayerId))
        {
            return Design.ColoedString(this.NameColor, $" ■");
        }

        return base.GetRolePlayerNameTag(targetRole, targetPlayerId);
    }


    public override void ExiledAction(PlayerControl rolePlayer)
    {
        resetShield(rolePlayer.PlayerId);
    }

    public override void RolePlayerKilledAction(
        PlayerControl rolePlayer, PlayerControl killerPlayer)
    {
        resetShield(rolePlayer.PlayerId);
    }

    public void CreateAbility()
    {

        this.shildButtonImage = Loader.CreateSpriteFromResources(
                Path.BodyGuardShield);

        this.Button = new ExtremeAbilityButton(
            new BodyGuardAbilityBehavior(
                featShieldMode: new(
					BodyGuardAbilityMode.FeatShield,
					new ButtonGraphic(
						Translation.GetString("shield"),
						this.shildButtonImage)),
                resetMode: new(
					BodyGuardAbilityMode.Reset,
					new ButtonGraphic(
						Translation.GetString("resetShield"),
						Loader.CreateSpriteFromResources(
							Path.BodyGuardResetShield))),
                featShield: UseAbility,
                resetShield: Reset,
                canUse: IsAbilityUse,
                resetModeCheck: IsResetMode),
            new RoleButtonActivator(),
            KeyCode.F);

		((IRoleAbility)(this)).RoleAbilityInit();

		if (this.Button.Behavior is BodyGuardAbilityBehavior behavior)
		{
			int abilityNum = OptionManager.Instance.GetValue<int>(GetRoleOptionId(
				RoleAbilityCommonOption.AbilityCount));

			this.shildNum = abilityNum;
			behavior.SetAbilityCount(abilityNum);
		}

        this.Button.SetLabelToCrewmate();
    }

    public void Reset()
    {
        PlayerControl localPlayer = CachedPlayerControl.LocalPlayer;
        byte playerId = localPlayer.PlayerId;

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.BodyGuardAbility))
        {
            caller.WriteByte((byte)BodyGuardRpcOps.ResetShield);
            caller.WriteByte(playerId);
        }
        resetShield(playerId);

        if (this.Button.Behavior is BodyGuardAbilityBehavior behavior)
        {
            behavior.SetAbilityCount(this.shildNum);
        }
    }

    public bool UseAbility()
    {
        PlayerControl localPlayer = CachedPlayerControl.LocalPlayer;
        byte playerId = localPlayer.PlayerId;

        if (this.targetPlayer != byte.MaxValue)
        {
            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.BodyGuardAbility))
            {
                caller.WriteByte((byte)BodyGuardRpcOps.FeatShield);
                caller.WriteByte(playerId);
                caller.WriteByte(this.targetPlayer);
            }
            featShield(playerId, this.targetPlayer);

            this.targetPlayer = byte.MaxValue;

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsResetMode() =>
		this.targetPlayer == byte.MaxValue &&
		shilded.IsGuard(CachedPlayerControl.LocalPlayer.PlayerId);

    public bool IsAbilityUse()
    {
        this.targetPlayer = byte.MaxValue;

        PlayerControl target = Player.GetClosestPlayerInRange(
            CachedPlayerControl.LocalPlayer, this,
            this.shieldRange);

		if (target != null)
		{
			byte targetId = target.PlayerId;

			if (!shilded.IsShielding(
				CachedPlayerControl.LocalPlayer.PlayerId, targetId))
			{
				this.targetPlayer = targetId;
			}
		}

		return IRoleAbility.IsCommonUse() && this.targetPlayer != byte.MaxValue;
    }

    public void ResetOnMeetingStart()
    { }

    public void ResetOnMeetingEnd(GameData.PlayerInfo? exiledPlayer = null)
    { }

    public bool IsBlockMeetingButtonAbility(PlayerVoteArea instance)
    {
        byte bodyGuardPlayerId = CachedPlayerControl.LocalPlayer.PlayerId;
        byte targetPlayerId = instance.TargetPlayerId;

        if (targetPlayerId == bodyGuardPlayerId ||
			shilded.IsShielding(bodyGuardPlayerId, targetPlayerId))
        {
            return true;
        }
        else if (this.Button.Behavior is BodyGuardAbilityBehavior behavior)
        {
            return
                !this.awakeMeetingAbility ||
                behavior.AbilityCount <= 0 ||
                instance.TargetPlayerId == 253;
        }
        else
        {
            return true;
        }
    }

    public void ButtonMod(PlayerVoteArea instance, UiElement abilityButton)
    {
        abilityButton.name = $"bodyGuardFeatShield_{instance.TargetPlayerId}";
        var controllerHighlight = abilityButton.transform.FindChild("ControllerHighlight");
        if (controllerHighlight != null)
        {
            controllerHighlight.localScale *= new Vector2(1.25f, 1.25f);
        }
    }

    public Action CreateAbilityAction(PlayerVoteArea instance)
    {
        PlayerControl player = CachedPlayerControl.LocalPlayer;
        byte targetPlayerId = instance.TargetPlayerId;

        void meetingfeatShield()
        {
            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.BodyGuardAbility))
            {
                caller.WriteByte((byte)BodyGuardRpcOps.FeatShield);
                caller.WriteByte(player.PlayerId);
                caller.WriteByte(targetPlayerId);
            }
            featShield(player.PlayerId, targetPlayerId);

            if (this.Button.Behavior is BodyGuardAbilityBehavior behavior)
            {
                behavior.SetAbilityCount(behavior.AbilityCount - 1);
            }
        }
        return meetingfeatShield;
    }

    public void AllReset(PlayerControl rolePlayer)
    {
        resetShield(rolePlayer.PlayerId);
    }

    public void SetSprite(SpriteRenderer render)
    {
        render.sprite = this.shildButtonImage;
        render.transform.localScale *= new Vector2(0.625f, 0.625f);
    }

    public void Update(PlayerControl rolePlayer)
    {
        if (!this.awakeMeetingAbility || !this.awakeMeetingReport)
        {
            float taskGage = Player.GetPlayerTaskGage(rolePlayer);

            if (taskGage >= this.meetingAbilityTaskGage &&
                !this.awakeMeetingAbility)
            {
                this.awakeMeetingAbility = true;
            }
            if (taskGage >= this.meetingReportTaskGage &&
                !this.awakeMeetingReport)
            {
                using (var caller = RPCOperator.CreateCaller(
                    RPCOperator.Command.BodyGuardAbility))
                {
                    caller.WriteByte((byte)BodyGuardRpcOps.AwakeMeetingReport);
                    caller.WriteByte(rolePlayer.PlayerId);
                }
                this.awakeMeetingReport = true;
            }
        }
        if (this.awakeMeetingAbility && MeetingHud.Instance)
        {
            if (this.meetingText == null)
            {
                this.meetingText = UnityEngine.Object.Instantiate(
                    FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                    MeetingHud.Instance.transform);
                this.meetingText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                this.meetingText.transform.position = Vector3.zero;
                this.meetingText.transform.localPosition = new Vector3(-2.85f, 3.15f, -20f);
                this.meetingText.transform.localScale *= 0.9f;
                this.meetingText.color = Palette.White;
                this.meetingText.gameObject.SetActive(false);
            }

            if (this.Button.Behavior is BodyGuardAbilityBehavior behavior)
            {
                this.meetingText.text = string.Format(
                    Helper.Translation.GetString("meetingShieldState"),
                    behavior.AbilityCount);
            }
            this.meetingText.gameObject.SetActive(true);
        }
        else
        {
            if (this.meetingText != null)
            {
                this.meetingText.gameObject.SetActive(false);
            }
        }
    }

    protected override void CreateSpecificOption(
        IOptionInfo parentOps)
    {

        CreateFloatOption(
            BodyGuardOption.ShieldRange,
            1.0f, 0.0f, 2.0f, 0.1f,
            parentOps);

        this.CreateAbilityCountOption(
            parentOps, 2, 5);

        CreateIntOption(
            BodyGuardOption.FeatMeetingAbilityTaskGage,
            30, 0, 100, 10,
            parentOps,
            format: OptionUnit.Percentage);
        CreateIntOption(
            BodyGuardOption.FeatMeetingReportTaskGage,
            60, 0, 100, 10,
            parentOps,
            format: OptionUnit.Percentage);
        var reportPlayerNameOpt = CreateBoolOption(
            BodyGuardOption.IsReportPlayerName,
            false, parentOps);
        CreateSelectionOption(
            BodyGuardOption.ReportPlayerMode,
            new string[]
            {
                BodyGuardReportPlayerNameMode.GuardedPlayerNameOnly.ToString(),
                BodyGuardReportPlayerNameMode.BodyGuardPlayerNameOnly.ToString(),
                BodyGuardReportPlayerNameMode.BothPlayerName.ToString(),
            }, reportPlayerNameOpt);
        CreateBoolOption(
            BodyGuardOption.IsBlockMeetingKill,
            true, parentOps);
    }

    protected override void RoleSpecificInit()
    {
        var allOpt = OptionManager.Instance;

        IsBlockMeetingKill = allOpt.GetValue<bool>(
            GetRoleOptionId(BodyGuardOption.IsBlockMeetingKill));

        this.shieldRange = allOpt.GetValue<float>(
            GetRoleOptionId(BodyGuardOption.ShieldRange));

        this.meetingAbilityTaskGage = allOpt.GetValue<int>(
            GetRoleOptionId(BodyGuardOption.FeatMeetingAbilityTaskGage)) / 100.0f;
        this.meetingReportTaskGage = allOpt.GetValue<int>(
            GetRoleOptionId(BodyGuardOption.FeatMeetingReportTaskGage)) / 100.0f;

        this.isReportWithPlayerName = allOpt.GetValue<bool>(
            GetRoleOptionId(BodyGuardOption.IsReportPlayerName));
        this.reportMode = (BodyGuardReportPlayerNameMode)allOpt.GetValue<int>(
            GetRoleOptionId(BodyGuardOption.ReportPlayerMode));

        this.awakeMeetingAbility = this.meetingAbilityTaskGage <= 0.0f;
        this.awakeMeetingReport = this.meetingReportTaskGage <= 0.0f;
    }
}
