﻿using UnityEngine;
using Hazel;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.CustomOption;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;
using ExtremeRoles.Module.Ability;


using ExtremeRoles.Module.CustomOption.Factory;

namespace ExtremeRoles.Roles.Solo.Impostor;

public sealed class Slime :
    SingleRoleBase,
    IRoleAutoBuildAbility,
    IRoleSpecialReset,
    IRolePerformKillHook
{
    public enum SlimeRpc : byte
    {
        Morph,
        Reset,
    }

    public ExtremeAbilityButton Button { get; set; }

    private Console targetConsole;
    private GameObject consoleObj;
    private bool isKilling = false;

    public Slime() : base(
        ExtremeRoleId.Slime,
        ExtremeRoleType.Impostor,
        ExtremeRoleId.Slime.ToString(),
        Palette.ImpostorRed,
        true, false, true, true)
    { }

    public static void Ability(ref MessageReader reader)
    {
        SlimeRpc rpcId = (SlimeRpc)reader.ReadByte();
        byte rolePlayerId = reader.ReadByte();

        var rolePlayer = Player.GetPlayerControlById(rolePlayerId);
        var role = ExtremeRoleManager.GetSafeCastedRole<Slime>(rolePlayerId);
        if (role == null || rolePlayer == null) { return; }
        switch (rpcId)
        {
            case SlimeRpc.Morph:
                int index = reader.ReadPackedInt32();
                setPlayerSpriteToConsole(role, rolePlayer, index);
                break;
            case SlimeRpc.Reset:
                removeMorphConsole(role, rolePlayer);
                break;
            default:
                break;
        }
    }

    private static void setPlayerSpriteToConsole(Slime slime, PlayerControl player, int index)
    {
        Console console = CachedShipStatus.Instance.AllConsoles[index];

        if (console == null || console.Image == null) { return; }

        slime.consoleObj = new GameObject("MorphConsole");
        slime.consoleObj.transform.SetParent(player.transform);

        SpriteRenderer rend = slime.consoleObj.AddComponent<SpriteRenderer>();
        rend.sprite = console.Image.sprite;

        Vector3 scale = player.transform.localScale;
        slime.consoleObj.transform.position = player.transform.position;
        slime.consoleObj.transform.localScale =
            console.transform.lossyScale * (1.0f / scale.x);

        player.cosmetics.Visible = false;
        player.cosmetics.lockVisible = true;
    }

    private static void removeMorphConsole(Slime slime, PlayerControl player)
    {
        if (slime.consoleObj != null)
        {
            Object.Destroy(slime.consoleObj);
        }

        player.cosmetics.lockVisible = false;
        player.cosmetics.Visible = true;
        slime.isKilling = false;
    }

    public void OnStartKill()
    {
        this.isKilling = true;
    }

    public void OnEndKill()
    {
        this.isKilling = false;
    }

    public void CreateAbility()
    {
        this.CreateReclickableAbilityButton(
			OldTranslation.GetString("SlimeMorph"),
			Resources.UnityObjectLoader.LoadSpriteFromResources(
			   ObjectPath.SlimeMorph),
            checkAbility: IsAbilityActive,
            abilityOff: this.CleanUp);
    }

    public bool IsAbilityActive() =>
        PlayerControl.LocalPlayer.moveable || this.isKilling;

    public bool IsAbilityUse()
    {
        PlayerControl localPlayer = PlayerControl.LocalPlayer;

        this.targetConsole = Player.GetClosestConsole(
            localPlayer, localPlayer.MaxReportDistance);

        if (this.targetConsole == null) { return false; }

        return
            IRoleAbility.IsCommonUse() &&
            this.targetConsole.Image != null &&
            GameSystem.IsValidConsole(localPlayer, this.targetConsole);
    }

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
        PlayerControl player = PlayerControl.LocalPlayer;
        for (int i = 0; i < CachedShipStatus.Instance.AllConsoles.Length; ++i)
        {
            Console console = CachedShipStatus.Instance.AllConsoles[i];
            if (console != this.targetConsole) { continue; }

            using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.SlimeAbility))
            {
                caller.WriteByte((byte)SlimeRpc.Morph);
                caller.WriteByte(player.PlayerId);
                caller.WritePackedInt(i);
            }

            setPlayerSpriteToConsole(this, player, i);

            return true;
        }
        return false;
    }

    public void CleanUp()
    {
        PlayerControl player = PlayerControl.LocalPlayer;

        using (var caller = RPCOperator.CreateCaller(
            RPCOperator.Command.SlimeAbility))
        {
            caller.WriteByte((byte)SlimeRpc.Reset);
            caller.WriteByte(player.PlayerId);
        }
        removeMorphConsole(this, player);
    }

    public override void RolePlayerKilledAction(
        PlayerControl rolePlayer, PlayerControl killerPlayer)
    {
        removeMorphConsole(this, rolePlayer);
    }

    protected override void CreateSpecificOption(
        AutoParentSetOptionCategoryFactory factory)
    {
        IRoleAbility.CreateCommonAbilityOption(
            factory, 30.0f);
    }

    protected override void RoleSpecificInit()
    {
        this.isKilling = false;
    }

    public void AllReset(PlayerControl rolePlayer)
    {
        removeMorphConsole(this, rolePlayer);
    }
}
