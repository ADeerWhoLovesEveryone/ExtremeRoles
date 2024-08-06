﻿using System;

using UnityEngine;
using AmongUs.GameOptions;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.CustomOption;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;
using ExtremeRoles.Performance.Il2Cpp;
using ExtremeRoles.Module.Ability;


using ExtremeRoles.Module.CustomOption.Factory;


namespace ExtremeRoles.Roles.Solo.Impostor;

public sealed class OverLoader : SingleRoleBase, IRoleAutoBuildAbility, IRoleAwake<RoleTypes>
{

    public enum OverLoaderOption
    {
        AwakeImpostorNum,
        AwakeKillCount,
        KillCoolReduceRate,
        MoveSpeed
    }

    public RoleTypes NoneAwakeRole => RoleTypes.Impostor;

    public bool IsAwake
    {
        get
        {
            return GameSystem.IsLobby || this.isAwake;
        }
    }

    public bool IsOverLoad;

    private float reduceRate;
    private float defaultKillCool;
    private int defaultKillRange;

    private bool isAwake;
    private int awakeImpNum;
    private int awakeKillCount;
    private int killCount;

    private bool isAwakedHasOtherVision;
    private bool isAwakedHasOtherKillCool;
    private bool isAwakedHasOtherKillRange;


    public ExtremeAbilityButton Button
    {
        get => this.overLoadButton;
        set
        {
            this.overLoadButton = value;
        }
    }

    private ExtremeAbilityButton overLoadButton;


    public OverLoader() : base(
        ExtremeRoleId.OverLoader,
        ExtremeRoleType.Impostor,
        ExtremeRoleId.OverLoader.ToString(),
        Palette.ImpostorRed,
        true, false, true, true)
    {
        this.IsOverLoad = false;
    }

    public static void SwitchAbility(byte rolePlayerId, bool activate)
    {
        var overLoader = ExtremeRoleManager.GetSafeCastedRole<OverLoader>(rolePlayerId);
        if (overLoader != null)
        {
            overLoader.IsOverLoad = activate;
            overLoader.IsBoost = activate;
        }
    }

    public void CreateAbility()
    {
        this.CreatePassiveAbilityButton(
            "overLoad", "downLoad",
			Resources.UnityObjectLoader.LoadSpriteFromResources(
			   ObjectPath.OverLoaderOverLoad),
			Resources.UnityObjectLoader.LoadSpriteFromResources(
			   ObjectPath.OverLoaderDownLoad),
            this.CleanUp);
    }

    public bool IsAbilityUse() =>
        this.IsAwake && IRoleAbility.IsCommonUse();

    public void ResetOnMeetingEnd(NetworkedPlayerInfo exiledPlayer = null)
    {
        return;
    }

    public void ResetOnMeetingStart()
    {
        return;
    }

    public bool UseAbility()
    {
        this.KillCoolTime = this.defaultKillCool * ((100f - this.reduceRate) / 100f);
        this.KillRange = 2;
        abilityOn();
        return true;
    }

    public void CleanUp()
    {
        this.KillCoolTime = this.defaultKillCool;
        this.KillRange = this.defaultKillRange;
        abilityOff();
    }

    public string GetFakeOptionString() => "";

    public void Update(PlayerControl rolePlayer)
    {
        if (!this.isAwake)
        {
            if (this.Button != null)
            {
                this.Button.SetButtonShow(false);
            }

            int impNum = 0;

            foreach (var player in GameData.Instance.AllPlayers.GetFastEnumerator())
            {
                if (ExtremeRoleManager.GameRole[player.PlayerId].IsImpostor() &&
                    (!player.IsDead && !player.Disconnected))
                {
                    ++impNum;
                }
            }

            if (this.awakeImpNum >= impNum &&
                this.killCount >= this.awakeKillCount)
            {
                this.Button.SetButtonShow(true);
                this.isAwake = true;
                this.HasOtherVision = this.isAwakedHasOtherVision;
                this.HasOtherKillCool = this.isAwakedHasOtherKillCool;
                this.HasOtherKillRange = this.isAwakedHasOtherKillRange;
            }
        }
    }

    public override bool TryRolePlayerKillTo(
        PlayerControl rolePlayer, PlayerControl targetPlayer)
    {
        if (!this.isAwake)
        {
            ++this.killCount;
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
                Palette.ImpostorRed, Tr.GetString(RoleTypes.Impostor.ToString()));
        }
    }
    public override string GetFullDescription()
    {
        if (IsAwake)
        {
            return Tr.GetString(
                $"{this.Id}FullDescription");
        }
        else
        {
            return Tr.GetString(
                $"{RoleTypes.Impostor}FullDescription");
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
            return string.Concat(new string[]
            {
                FastDestroyableSingleton<TranslationController>.Instance.GetString(
                    StringNames.ImpostorTask, Array.Empty<Il2CppSystem.Object>()),
                "\r\n",
                Palette.ImpostorRed.ToTextColor(),
                FastDestroyableSingleton<TranslationController>.Instance.GetString(
                    StringNames.FakeTasks, Array.Empty<Il2CppSystem.Object>()),
                "</color>"
            });
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
                Palette.ImpostorRed,
                PlayerControl.LocalPlayer.Data.Role.Blurb);
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
            return Palette.ImpostorRed;
        }
    }


    protected override void CreateSpecificOption(
        AutoParentSetOptionCategoryFactory factory)
    {
        factory.CreateIntOption(
            OverLoaderOption.AwakeImpostorNum,
            GameSystem.MaxImposterNum, 1,
            GameSystem.MaxImposterNum, 1);

        factory.CreateIntOption(
            OverLoaderOption.AwakeKillCount,
            0, 0, 3, 1);

        IRoleAbility.CreateCommonAbilityOption(
            factory, 7.5f);

        factory.CreateFloatOption(
            OverLoaderOption.KillCoolReduceRate,
            75.0f, 50.0f, 90.0f, 1.0f,
            format: OptionUnit.Percentage);
        factory.CreateFloatOption(
            OverLoaderOption.MoveSpeed,
            1.5f, 1.0f, 3.0f, 0.1f,
            format: OptionUnit.Multiplier);
    }

    protected override void RoleSpecificInit()
    {
        var curOption = GameOptionsManager.Instance.CurrentGameOptions;

        if (!this.HasOtherKillCool)
        {
            this.HasOtherKillCool = true;
            this.KillCoolTime = curOption.GetFloat(FloatOptionNames.KillCooldown);
        }
        if (!this.HasOtherKillRange)
        {
            this.HasOtherKillRange = true;
            this.KillRange = curOption.GetInt(Int32OptionNames.KillDistance);
        }

        this.defaultKillCool = this.KillCoolTime;
        this.defaultKillRange = this.KillRange;
        this.IsOverLoad = false;

        var cate = this.Loader;

        this.awakeImpNum = cate.GetValue<OverLoaderOption, int>(
            OverLoaderOption.AwakeImpostorNum);
        this.awakeKillCount = cate.GetValue<OverLoaderOption, int>(
            OverLoaderOption.AwakeKillCount);

        this.MoveSpeed = cate.GetValue<OverLoaderOption, float>(
            OverLoaderOption.MoveSpeed);
        this.reduceRate = cate.GetValue<OverLoaderOption, float>(
            OverLoaderOption.KillCoolReduceRate);

        this.killCount = 0;

        this.isAwakedHasOtherVision = false;
        this.isAwakedHasOtherKillCool = true;
        this.isAwakedHasOtherKillRange = false;

        if (this.HasOtherVision)
        {
            this.HasOtherVision = false;
            this.isAwakedHasOtherVision = true;
        }

        this.defaultKillCool = this.KillCoolTime;

        if (this.HasOtherKillCool)
        {
            this.HasOtherKillCool = false;
        }

        if (this.HasOtherKillRange)
        {
            this.HasOtherKillRange = false;
            this.isAwakedHasOtherKillRange = true;
        }

        if (this.awakeImpNum >= curOption.GetInt(Int32OptionNames.NumImpostors) &&
            this.awakeKillCount == 0)
        {
            this.isAwake = true;
            this.HasOtherVision = this.isAwakedHasOtherVision;
            this.HasOtherKillCool = this.isAwakedHasOtherKillCool;
            this.HasOtherKillRange = this.isAwakedHasOtherKillRange;
        }
    }

    private void abilityOn()
    {
        byte localPlayerId = PlayerControl.LocalPlayer.PlayerId;

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.OverLoaderSwitchAbility))
        {
            caller.WriteByte(localPlayerId);
            caller.WriteByte(byte.MaxValue);
        }
        SwitchAbility(localPlayerId, true);

    }
    private void abilityOff()
    {
        byte localPlayerId = PlayerControl.LocalPlayer.PlayerId;

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.OverLoaderSwitchAbility))
        {
            caller.WriteByte(localPlayerId);
            caller.WriteByte(byte.MinValue);
        }
        SwitchAbility(localPlayerId, false);
    }
}
