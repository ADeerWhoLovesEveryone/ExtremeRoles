﻿using System.Collections.Generic;

using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.AbilityBehavior;
using ExtremeRoles.Performance;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.Solo.Impostor;

public sealed class Zombie : 
    SingleRoleBase,
    IRoleAbility,
    IRoleAwake<RoleTypes>,
    IRoleOnRevive
{
    public override bool IsAssignGhostRole
    {
        get => false;
    }

    public bool IsAwake
    {
        get
        {
            return GameSystem.IsLobby || this.awakeRole;
        }
    }

    public RoleTypes NoneAwakeRole => RoleTypes.Impostor;

    public ExtremeAbilityButton Button { get; set; }

    public enum ZombieOption
    {
        AwakeKillCount,
        ResurrectKillCount,
        ResurrectDelayTime,
        CanResurrectOnExil,
    }

    public enum ZombieRpcOps : byte
    {
        UseResurrect,
        ResetFlash,
    }

    private bool awakeRole;
    private bool awakeHasOtherVision;
    private int awakeKillCount;
    private int resurrectKillCount;

    private int killCount;

    private bool canResurrect;
    private bool canResurrectOnExil;
    private bool isResurrected;

    private bool activateResurrectTimer;
    private float resurrectTimer;


    private Vector3 curPos;

    private TMPro.TextMeshPro resurrectText;

    public Zombie() : base(
        ExtremeRoleId.Zombie,
        ExtremeRoleType.Impostor,
        ExtremeRoleId.Zombie.ToString(),
        Palette.ImpostorRed,
        false, true, false, false)
    { }

    public static void RpcAbility(ref MessageReader reader)
    {
        ZombieRpcOps ops = (ZombieRpcOps)reader.ReadByte();
        byte zombiePlayerId = reader.ReadByte();

        switch (ops)
        {
            case ZombieRpcOps.UseResurrect:
                Zombie zombie = ExtremeRoleManager.GetSafeCastedRole<Zombie>(
                    zombiePlayerId);
                if (zombie == null) { return; }
                UseResurrect(zombie);
                break;
            default:
                break;
        }
    }

    public static void UseResurrect(Zombie zombie)
    {
        zombie.isResurrected = true;
        zombie.activateResurrectTimer = false;
    }

    public void CreateAbility()
    {
        this.CreateAbilityCountButton(
            "featMagicCircle",
            Resources.Loader.CreateSpriteFromResources(
                Resources.Path.TestButton),
            IsActivate,
            SetMagicCircle,
             () => { });
    }

    public bool IsActivate()
        => this.curPos != CachedPlayerControl.LocalPlayer.PlayerControl.transform.position;

    public bool UseAbility()
    {
        this.curPos = CachedPlayerControl.LocalPlayer.PlayerControl.transform.position;
        return true;
    }

    public bool IsAbilityUse()
    {
        return this.IsCommonUse();
    }

    public void SetMagicCircle()
    {
        if (this.killCount >= this.resurrectKillCount &&
            this.Button.Behavior is AbilityCountBehavior behavior &&
            behavior.AbilityCount <= 0 &&
            !this.canResurrect)
        {
            this.canResurrect = true;
            this.isResurrected = false;
        }
    }

    public void ResetOnMeetingStart()
    {
        if (this.resurrectText != null)
        {
            this.resurrectText.gameObject.SetActive(false);
        }
    }

    public void ResetOnMeetingEnd(GameData.PlayerInfo exiledPlayer = null)
    {
        return;
    }

    public void ReviveAction(PlayerControl player)
    {
        
    }

    public string GetFakeOptionString() => "";

    public void Update(PlayerControl rolePlayer)
    {

        if (rolePlayer.Data.IsDead && this.infoBlock())
        {
            FastDestroyableSingleton<HudManager>.Instance.Chat.gameObject.SetActive(false);
        }

        if (!rolePlayer.moveable ||
            MeetingHud.Instance ||
            ExileController.Instance ||
            CachedShipStatus.Instance == null ||
            !CachedShipStatus.Instance.enabled)
        {
            return;
        }

        if (this.isResurrected) { return; }

        if (rolePlayer.Data.IsDead &&
            this.activateResurrectTimer &&
            this.canResurrect)
        {
            if (this.resurrectText == null)
            {
                this.resurrectText = Object.Instantiate(
                    FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
                    Camera.main.transform, false);
                this.resurrectText.transform.localPosition = new Vector3(0.0f, 0.0f, -250.0f);
                this.resurrectText.enableWordWrapping = false;
            }

            this.resurrectText.gameObject.SetActive(true);
            this.resurrectTimer -= Time.fixedDeltaTime;
            this.resurrectText.text = string.Format(
                Translation.GetString("resurrectText"),
                Mathf.CeilToInt(this.resurrectTimer));

            if (this.resurrectTimer <= 0.0f)
            {
                this.activateResurrectTimer = false;
                revive(rolePlayer);
            }
        }
    }

    public override bool TryRolePlayerKillTo(
        PlayerControl rolePlayer, PlayerControl targetPlayer)
    {
        if ((!this.awakeRole ||
            (!this.canResurrect && !this.isResurrected)))
        {
            ++this.killCount;

            if (this.killCount >= this.awakeKillCount && !this.awakeRole)
            {
                this.awakeRole = true;
                this.HasOtherVision = this.awakeHasOtherVision;
            }
            if (this.killCount >= this.resurrectKillCount &&
                this.Button.Behavior is AbilityCountBehavior behavior &&
                behavior.AbilityCount <= 0 &&
                !this.canResurrect)
            {
                this.canResurrect = true;
                this.isResurrected = false;
            }
        }
        return true;
    }

    public override string GetColoredRoleName(bool isTruthColor = false)
    {
        if (isTruthColor || IsAwake)
        {
            return base.GetColoredRoleName();
        }
        else
        {
            return Design.ColoedString(
                Palette.White, Translation.GetString(RoleTypes.Crewmate.ToString()));
        }
    }
    public override string GetFullDescription()
    {
        if (IsAwake)
        {
            return Translation.GetString(
                $"{this.Id}FullDescription");
        }
        else
        {
            return Translation.GetString(
                $"{RoleTypes.Crewmate}FullDescription");
        }
    }

    public override string GetImportantText(bool isContainFakeTask = true)
    {
        if (IsAwake)
        {
            return base.GetImportantText(isContainFakeTask);

        }
        else
        {
            return Design.ColoedString(
                Palette.White,
                $"{this.GetColoredRoleName()}: {Translation.GetString("crewImportantText")}");
        }
    }

    public override string GetIntroDescription()
    {
        if (IsAwake)
        {
            return base.GetIntroDescription();
        }
        else
        {
            return Design.ColoedString(
                Palette.CrewmateBlue,
                CachedPlayerControl.LocalPlayer.Data.Role.Blurb);
        }
    }

    public override Color GetNameColor(bool isTruthColor = false)
    {
        if (isTruthColor || IsAwake)
        {
            return base.GetNameColor(isTruthColor);
        }
        else
        {
            return Palette.White;
        }
    }

    public override void ExiledAction(
        PlayerControl rolePlayer)
    {

        if (this.isResurrected) { return; }

        // 追放でオフ時は以下の処理を行わない
        if (!this.canResurrectOnExil) { return; }

        if (this.canResurrect)
        {
            this.activateResurrectTimer = true;
        }
    }

    public override void RolePlayerKilledAction(
        PlayerControl rolePlayer,
        PlayerControl killerPlayer)
    {
        if (this.isResurrected) { return; }
        
        if (this.canResurrect)
        {
            this.activateResurrectTimer = true;
        }
    }

    public override bool IsBlockShowMeetingRoleInfo() => this.infoBlock();

    public override bool IsBlockShowPlayingRoleInfo() => this.infoBlock();
         

    protected override void CreateSpecificOption(
        IOption parentOps)
    {
        CreateIntOption(
            ZombieOption.AwakeKillCount,
            1, 0, 3, 1,
            parentOps,
            format: OptionUnit.Percentage);

        this.CreateAbilityCountOption(parentOps, 1, 3, 3f);

        CreateIntOption(
            ZombieOption.ResurrectKillCount,
            2, 0, 3, 1,
            parentOps,
            format: OptionUnit.Percentage);

        CreateFloatOption(
            ZombieOption.ResurrectDelayTime,
            5.0f, 4.0f, 60.0f, 0.1f,
            parentOps, format: OptionUnit.Second);
        CreateBoolOption(
            ZombieOption.CanResurrectOnExil,
            false, parentOps);
    }

    protected override void RoleSpecificInit()
    {
        var allOpt = OptionHolder.AllOption;

        this.killCount = allOpt[
            GetRoleOptionId(ZombieOption.AwakeKillCount)].GetValue();
        this.resurrectKillCount = allOpt[
            GetRoleOptionId(ZombieOption.ResurrectKillCount)].GetValue();

        this.resurrectTimer = allOpt[
            GetRoleOptionId(ZombieOption.ResurrectDelayTime)].GetValue();
        this.canResurrectOnExil = allOpt[
            GetRoleOptionId(ZombieOption.CanResurrectOnExil)].GetValue();

        this.awakeHasOtherVision = this.HasOtherVision;
        this.canResurrect = false;
        this.isResurrected = false;
        this.activateResurrectTimer = false;

        if (this.killCount <= 0)
        {
            this.awakeRole = true;
            this.HasOtherVision = this.awakeHasOtherVision;
        }
        else
        {
            this.awakeRole = false;
            this.HasOtherVision = false;
        }
    }

    private bool infoBlock()
    {
        // ・詳細
        // 復活を使用後に死亡 => 常に見える
        // 非復活可能状態でキル、死亡後復活出来ない => 常に見える
        // 非復活可能状態でキル、死亡後復活出来る => 復活できるまで見えない
        // 非復活可能状態で追放、死亡後復活できる => 見えない
        // 非復活可能状態で追放、死亡後復活出来ない => 常に見える
        // 復活可能状態で死亡か追放 => 見えない

        if (this.isResurrected)
        {
            return false;
        }
        else
        {
            return this.activateResurrectTimer;
        }
    }

    private void revive(PlayerControl rolePlayer)
    {
        if (rolePlayer == null) { return; }

        byte playerId = rolePlayer.PlayerId;

        Player.RpcUncheckRevive(playerId);

        if (rolePlayer.Data == null ||
            rolePlayer.Data.IsDead ||
            rolePlayer.Data.Disconnected) { return; }

        var allPlayer = GameData.Instance.AllPlayers;
        ShipStatus ship = CachedShipStatus.Instance;

        List<Vector2> randomPos = new List<Vector2>();

        if (ExtremeRolesPlugin.Compat.IsModMap)
        {
            randomPos = ExtremeRolesPlugin.Compat.ModMap.GetSpawnPos(
                playerId);
        }
        else
        {
            switch (GameOptionsManager.Instance.CurrentGameOptions.GetByte(
                ByteOptionNames.MapId))
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    Vector2 baseVec = Vector2.up;
                    baseVec = baseVec.Rotate(
                        (float)(playerId - 1) * (360f / (float)allPlayer.Count));
                    Vector2 offset = baseVec * ship.SpawnRadius + new Vector2(0f, 0.3636f);
                    randomPos.Add(ship.InitialSpawnCenter + offset);
                    randomPos.Add(ship.MeetingSpawnCenter + offset);
                    break;
                case 4:
                    randomPos.AddRange(GameSystem.GetAirShipRandomSpawn());
                    break;
                default:
                    break;
            }
        }

        Player.RpcUncheckSnap(playerId, randomPos[
            RandomGenerator.Instance.Next(randomPos.Count)]);

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.ResurrecterRpc))
        {
            caller.WriteByte((byte)ZombieRpcOps.UseResurrect);
            caller.WriteByte(playerId);
        }
        UseResurrect(this);

        FastDestroyableSingleton<HudManager>.Instance.Chat.chatBubPool.ReclaimAll();
        if (this.resurrectText != null)
        {
            this.resurrectText.gameObject.SetActive(false);
        }
    }
}
