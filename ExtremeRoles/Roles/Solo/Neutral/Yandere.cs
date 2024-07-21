﻿using System.Collections.Generic;

using UnityEngine;

using AmongUs.GameOptions;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;

using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Roles.API.Extension.Neutral;
using ExtremeRoles.Performance;
using ExtremeRoles.Performance.Il2Cpp;

using ExtremeRoles.Module.CustomOption.Factory;

namespace ExtremeRoles.Roles.Solo.Neutral;


public sealed class Yandere :
    SingleRoleBase,
    IRoleUpdate,
    IRoleMurderPlayerHook,
    IRoleResetMeeting,
	IRoleReviveHook,
	IRoleSpecialSetUp
{
    public PlayerControl OneSidedLover = null;

    private bool isOneSidedLoverShare = false;

    private bool hasOneSidedArrow = false;
    private Arrow oneSidedArrow = null;

    private int maxTargetNum = 0;

    private int targetKillReduceRate = 0;
    private float noneTargetKillMultiplier = 0.0f;
    private float defaultKillCool;

    private bool isRunawayNextMeetingEnd = false;
    private bool isRunaway = false;

    private float timeLimit = 0f;
    private float timer = 0f;

    private float blockTargetTime = 0f;
    private float blockTimer = 0.0f;

    private float setTargetRange;
    private float setTargetTime;

    private string oneSidePlayerName = string.Empty;

    private KillTarget target;

    private Dictionary<byte, float> progress;

    public sealed class KillTarget
    {
        private bool isUseArrow;

        private Dictionary<byte, Arrow> targetArrow = new Dictionary<byte, Arrow>();
        private Dictionary<byte, PlayerControl> targetPlayer = new Dictionary<byte, PlayerControl>();

        public KillTarget(
            bool isUseArrow)
        {
            this.isUseArrow = isUseArrow;
            this.targetArrow.Clear();
            this.targetPlayer.Clear();
        }

        public void Add(byte playerId)
        {
            var player = Player.GetPlayerControlById(playerId);

            this.targetPlayer.Add(playerId, player);
            if (this.isUseArrow)
            {
                this.targetArrow.Add(
                    playerId, new Arrow(
                        new Color32(
                            byte.MaxValue,
                            byte.MaxValue,
                            byte.MaxValue,
                            byte.MaxValue)));
            }
            else
            {
                this.targetArrow.Add(playerId, null);
            }
        }

        public void ArrowActivate(bool active)
        {
            foreach (var arrow in this.targetArrow.Values)
            {
                if (arrow != null)
                {
                    arrow.SetActive(active);
                }
            }
        }

        public bool IsContain(byte playerId) => targetPlayer.ContainsKey(playerId);

        public int Count() => this.targetPlayer.Count;

        public void Remove(byte playerId)
        {
            this.targetPlayer.Remove(playerId);

            if (this.targetArrow[playerId] != null)
            {
                this.targetArrow[playerId].Clear();
            }

            this.targetArrow.Remove(playerId);
        }

        public void Update()
        {
            List<byte> remove = new List<byte>();

            foreach (var (playerId, playerControl) in this.targetPlayer)
            {
                if (playerControl == null ||
                    playerControl.Data.Disconnected ||
                    playerControl.Data.IsDead)
                {
                    remove.Add(playerId);
                    continue;
                }

                if (this.targetArrow[playerId] != null && this.isUseArrow)
                {
                    this.targetArrow[playerId].UpdateTarget(
                        playerControl.GetTruePosition());
                }
            }

            foreach (byte playerId in remove)
            {
                this.Remove(playerId);
            }
        }
    }

    public enum YandereOption
    {
        TargetKilledKillCoolReduceRate,
        NoneTargetKilledKillCoolMultiplier,
        BlockTargetTime,
        SetTargetRange,
        SetTargetTime,
        MaxTargetNum,
        RunawayTime,
        HasOneSidedArrow,
        HasTargetArrow,
    }

    public Yandere(): base(
        ExtremeRoleId.Yandere,
        ExtremeRoleType.Neutral,
        ExtremeRoleId.Yandere.ToString(),
        ColorPalette.YandereVioletRed,
        false, false, true, false)
    { }

    public static void SetOneSidedLover(
        byte rolePlayerId, byte oneSidedLoverId)
    {
        var yandere = ExtremeRoleManager.GetSafeCastedRole<Yandere>(rolePlayerId);
        if (yandere != null)
        {
            yandere.OneSidedLover = Player.GetPlayerControlById(oneSidedLoverId);
        }
    }

    public override bool TryRolePlayerKillTo(
        PlayerControl rolePlayer, PlayerControl targetPlayer)
    {
        if (this.target.IsContain(targetPlayer.PlayerId))
        {
            this.KillCoolTime = this.defaultKillCool * (
                (100f - this.targetKillReduceRate) / 100f);
        }
        else
        {
            this.KillCoolTime = this.KillCoolTime * this.noneTargetKillMultiplier;
        }

        return true;
    }

    public override bool IsSameTeam(SingleRoleBase targetRole) =>
        this.IsNeutralSameTeam(targetRole);

    public override string GetIntroDescription() => string.Format(
        base.GetIntroDescription(),
        this.oneSidePlayerName);

    public override string GetImportantText(
        bool isContainFakeTask = true)
    {
        return string.Format(
            base.GetImportantText(isContainFakeTask),
            this.oneSidePlayerName);
    }

    public override string GetRolePlayerNameTag(
        SingleRoleBase targetRole, byte targetPlayerId)
    {
        if (this.OneSidedLover == null) { return ""; }

        if (targetPlayerId == this.OneSidedLover.PlayerId)
        {
            return Design.ColoedString(
                ColorPalette.YandereVioletRed,
                $" ♥");
        }

        return base.GetRolePlayerNameTag(targetRole, targetPlayerId);
    }


    public void Update(PlayerControl rolePlayer)
    {

        if (CachedShipStatus.Instance == null ||
            GameData.Instance == null ||
            MeetingHud.Instance != null ||
            this.progress == null ||
            (!this.isOneSidedLoverShare && this.OneSidedLover == null))
        {
            return;
        }

        if (!CachedShipStatus.Instance.enabled ||
            ExtremeRolesPlugin.ShipState.AssassinMeetingTrigger)
        {
            return;
        }

        var playerInfo = GameData.Instance.GetPlayerById(
           rolePlayer.PlayerId);
        if (playerInfo.IsDead || playerInfo.Disconnected)
        {
            this.target.ArrowActivate(false);

            if (this.hasOneSidedArrow && this.oneSidedArrow != null)
            {
                this.oneSidedArrow.SetActive(false);
            }
            return;
        }

        if (this.OneSidedLover == null)
        {
            this.isRunaway = true;
            this.target.Update();
            updateCanKill();
            return;
        }

        if (!this.isOneSidedLoverShare)
        {
            this.isOneSidedLoverShare = true;

            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.YandereSetOneSidedLover))
            {
                caller.WriteByte(rolePlayer.PlayerId);
                caller.WriteByte(this.OneSidedLover.PlayerId);
            }
            SetOneSidedLover(
                rolePlayer.PlayerId,
                this.OneSidedLover.PlayerId);
            this.CanKill = false;
        }

        // 不必要なデータを削除、役職の人と想い人
        this.progress.Remove(rolePlayer.PlayerId);
        this.progress.Remove(this.OneSidedLover.PlayerId);


        Vector2 oneSideLoverPos = this.OneSidedLover.GetTruePosition();

        if (this.blockTimer > this.blockTargetTime)
        {
            // 片思いびとが生きてる時の処理
            if (!this.OneSidedLover.Data.Disconnected &&
                !this.OneSidedLover.Data.IsDead)
            {
                searchTarget(rolePlayer, oneSideLoverPos);
            }
            else
            {
                this.isRunaway = true;
            }
        }
        else
        {
            this.blockTimer += Time.deltaTime;
        }

        updateOneSideLoverArrow(oneSideLoverPos);

        this.target.Update();

        updateCanKill();

        checkRunawayNextMeeting();
    }

    public void HookMuderPlayer(
        PlayerControl source, PlayerControl target)
    {
        if (this.target.IsContain(target.PlayerId))
        {
            this.target.Remove(target.PlayerId);
        }
    }

    public void IntroBeginSetUp()
    {

        int playerIndex;

        do
        {
            playerIndex = UnityEngine.Random.RandomRange(
                0, PlayerCache.AllPlayerControl.Count - 1);

            this.OneSidedLover = PlayerCache.AllPlayerControl[playerIndex];

            var role = ExtremeRoleManager.GameRole[this.OneSidedLover.PlayerId];
            if (role.Id != ExtremeRoleId.Yandere &&
                role.Id != ExtremeRoleId.Xion) { break; }

            var multiAssignRole = role as MultiAssignRoleBase;
            if (multiAssignRole != null)
            {
                if (multiAssignRole.AnotherRole != null)
                {

                    if (multiAssignRole.AnotherRole.Id != ExtremeRoleId.Yandere)
                    {
                        break;
                    }
                }
            }

        } while (true);
        this.oneSidePlayerName = this.OneSidedLover.Data.PlayerName;
        this.isOneSidedLoverShare = false;
    }

    public void IntroEndSetUp()
    {
        foreach(var player in GameData.Instance.AllPlayers.GetFastEnumerator())
        {
            this.progress.Add(player.PlayerId, 0.0f);
        }
        this.CanKill = false;
    }

    public void ResetOnMeetingEnd(NetworkedPlayerInfo exiledPlayer = null)
    {
        if (this.isRunawayNextMeetingEnd)
        {
            this.CanKill = true;
            this.isRunaway = true;
            this.isRunawayNextMeetingEnd = false;
        }
        this.target.ArrowActivate(true);

        if (this.hasOneSidedArrow && this.oneSidedArrow != null)
        {
            this.oneSidedArrow.SetActive(true);
        }
    }

    public void ResetOnMeetingStart()
    {
        this.KillCoolTime = this.defaultKillCool;
        this.CanKill = false;
        this.isRunaway = false;
        this.timer = 0f;

        this.target.ArrowActivate(false);

        if (this.hasOneSidedArrow && this.oneSidedArrow != null)
        {
            this.oneSidedArrow.SetActive(false);
        }

    }

    protected override void CreateSpecificOption(
        AutoParentSetOptionCategoryFactory factory)
    {
        factory.CreateIntOption(
            YandereOption.TargetKilledKillCoolReduceRate,
            85, 25, 99, 1,
            format: OptionUnit.Percentage);

        factory.CreateFloatOption(
            YandereOption.NoneTargetKilledKillCoolMultiplier,
            1.2f, 1.0f, 2.0f, 0.1f,
            format: OptionUnit.Multiplier);

        factory.CreateFloatOption(
            YandereOption.BlockTargetTime,
            5.0f, 0.5f, 30.0f, 0.5f,
			format: OptionUnit.Second);

        factory.CreateFloatOption(
            YandereOption.SetTargetRange,
            1.8f, 0.5f, 5.0f, 0.1f);

        factory.CreateFloatOption(
            YandereOption.SetTargetTime,
            2.0f, 0.1f, 7.5f, 0.1f,
            format: OptionUnit.Second);

        factory.CreateIntOption(
            YandereOption.MaxTargetNum,
            5, 1, GameSystem.VanillaMaxPlayerNum, 1);

        factory.CreateFloatOption(
            YandereOption.RunawayTime,
            60.0f, 25.0f, 120.0f, 0.25f,
            format: OptionUnit.Second);

        factory.CreateBoolOption(
            YandereOption.HasOneSidedArrow,
            true);

        factory.CreateBoolOption(
            YandereOption.HasTargetArrow,
            true);
    }

    protected override void RoleSpecificInit()
    {
		var cate = this.Loader;

        this.setTargetRange = cate.GetValue<YandereOption, float>(
            YandereOption.SetTargetRange);
        this.setTargetTime = cate.GetValue<YandereOption, float>(
            YandereOption.SetTargetTime);

        this.targetKillReduceRate = cate.GetValue<YandereOption, int>(
            YandereOption.TargetKilledKillCoolReduceRate);
        this.noneTargetKillMultiplier = cate.GetValue<YandereOption, float>(
            YandereOption.NoneTargetKilledKillCoolMultiplier);

        this.maxTargetNum = cate.GetValue<YandereOption, int>(
            YandereOption.MaxTargetNum);

        this.timer = 0.0f;
        this.timeLimit = cate.GetValue<YandereOption, float>(
            YandereOption.RunawayTime);

        this.blockTimer = 0.0f;
        this.blockTargetTime = cate.GetValue<YandereOption, float>(
            YandereOption.BlockTargetTime);

        this.hasOneSidedArrow = cate.GetValue<YandereOption, bool>(
            YandereOption.HasOneSidedArrow);
        this.target = new KillTarget(
			cate.GetValue<YandereOption, bool>(YandereOption.HasTargetArrow));

        this.progress = new Dictionary<byte, float>();

        if (this.HasOtherKillCool)
        {
            this.defaultKillCool = this.KillCoolTime;
        }
        else
        {
            this.defaultKillCool = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(
                FloatOptionNames.KillCooldown);
            this.HasOtherKillCool = true;
        }

        this.isRunaway = false;
        this.isOneSidedLoverShare = false;
        this.oneSidePlayerName = string.Empty;
    }

    private void checkRunawayNextMeeting()
    {
        if (this.isRunaway || this.isRunawayNextMeetingEnd) { return; }

        if (this.target.Count() == 0)
        {
            this.timer += Time.deltaTime;
            if (this.timer >= this.timeLimit)
            {
                this.isRunawayNextMeetingEnd = true;
            }
        }
        else
        {
            this.timer = 0.0f;
        }
    }

    private void searchTarget(
        PlayerControl rolePlayer,
        Vector2 pos)
    {
        foreach (NetworkedPlayerInfo playerInfo in
            GameData.Instance.AllPlayers.GetFastEnumerator())
        {

			byte playerId = playerInfo.PlayerId;

			if (!this.progress.TryGetValue(playerId, out float playerProgress))
			{
				continue;
			}

            if (!playerInfo.Disconnected &&
                !playerInfo.IsDead &&
                rolePlayer.PlayerId != playerId &&
                this.OneSidedLover.PlayerId != playerId &&
                !playerInfo.Object.inVent)
            {
                PlayerControl @object = playerInfo.Object;
                if (@object)
                {
                    Vector2 vector = @object.GetTruePosition() - pos;
                    float magnitude = vector.magnitude;

                    if (magnitude <= this.setTargetRange &&
                        !PhysicsHelpers.AnyNonTriggersBetween(
                            pos, vector.normalized,
                            magnitude, Constants.ShipAndObjectsMask))
                    {
                        playerProgress += Time.deltaTime;
                    }
                    else
                    {
                        playerProgress = 0.0f;
                    }
                }
            }
            else
            {
                playerProgress = 0.0f;
            }

            if (playerProgress >= this.setTargetTime &&
                !this.target.IsContain(playerId) &&
                this.target.Count() < this.maxTargetNum)
            {
                this.target.Add(playerId);
                this.progress.Remove(playerId);
            }
            else
            {
                this.progress[playerId] = playerProgress;
            }
        }
    }

    private void updateCanKill()
    {
        if (this.isRunaway)
        {
            this.CanKill = true;
            return;
        }

        this.CanKill = this.target.Count() > 0;
    }

    private void updateOneSideLoverArrow(Vector2 pos)
    {

        if (!this.hasOneSidedArrow) { return; }

        if (this.oneSidedArrow == null)
        {
            this.oneSidedArrow = new Arrow(
                this.NameColor);
        }
        this.oneSidedArrow.UpdateTarget(pos);
    }

	public void HookRevive(PlayerControl revivePlayer)
	{
		lock(this.progress)
		{
			byte playerId = revivePlayer.PlayerId;
			if (this.progress.ContainsKey(playerId))
			{
				return;
			}
			this.progress.Add(playerId, 0.0f);
		}
	}
}
