﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAbilityButton;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

using BepInEx.IL2CPP.Utils.Collections;


namespace ExtremeRoles.Roles.Solo.Impostor
{
    public class Bomber : SingleRoleBase, IRoleAbility, IRoleUpdate
    {
        public enum BomberOption
        {
            ExplosionRange,
            ExplosionKillChance,
            TimerMaxTime,
            TimerMinTime,
            TellExplosion
        }

        private float timer = 0f;
        private float timerMinTime = 0f;
        private float timerMaxTime = 0f;
        private int explosionKillChance;
        private float explosionRange;
        private bool tellExplosion;
        private byte setTargetPlayerId;
        private byte bombSettingPlayerId;

        private Queue<byte> bombPlayerId = new Queue<byte>();
        private TMPro.TextMeshPro tellText;

        public RoleAbilityButtonBase Button
        {
            get => this.bombButton;
            set
            {
                this.bombButton = value;
            }
        }
        private RoleAbilityButtonBase bombButton;


        public Bomber() : base(
            ExtremeRoleId.Bomber,
            ExtremeRoleType.Impostor,
            ExtremeRoleId.Bomber.ToString(),
            Palette.ImpostorRed,
            true, false, true, true)
        {
            this.bombPlayerId.Clear();
        }

        public void CreateAbility()
        {

            this.CreateAbilityCountButton(
                Translation.GetString("setBomb"),
                Loader.CreateSpriteFromResources(
                    Path.BomberSetBomb),
                checkAbility: CheckAbility,
                abilityCleanUp: CleanUp);
        }

        public bool IsAbilityUse()
        {
            this.setTargetPlayerId = byte.MaxValue;
            var player = PlayerControl.LocalPlayer.FindClosestTarget(false);
            if (player != null)
            {
                this.setTargetPlayerId = player.PlayerId;
            }
            return this.IsCommonUse() && this.setTargetPlayerId != byte.MaxValue;
        }

        public void CleanUp()
        {
            bombPlayerId.Enqueue(this.bombSettingPlayerId);
            bombSettingPlayerId = byte.MaxValue;
        }

        public bool CheckAbility()
        {
            byte targetPlayerId = byte.MaxValue;
            var player = PlayerControl.LocalPlayer.FindClosestTarget(false);
            if (player != null)
            {
                targetPlayerId = player.PlayerId;
            }
            return this.bombSettingPlayerId == targetPlayerId;
        }

        public bool UseAbility()
        {
            this.bombSettingPlayerId = this.setTargetPlayerId;
            return true;
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            this.CreateAbilityCountOption(
                parentOps, 2, 5, 2.5f);
            CustomOption.Create(
               GetRoleOptionId((int)BomberOption.ExplosionRange),
               string.Concat(
                   this.RoleName,
                   BomberOption.ExplosionRange.ToString()),
               2, 1, 5, 1,
               parentOps);
            CustomOption.Create(
               GetRoleOptionId((int)BomberOption.ExplosionKillChance),
               string.Concat(
                   this.RoleName,
                   BomberOption.ExplosionKillChance.ToString()),
               50, 25, 75, 1,
               parentOps, format: OptionUnit.Percentage);
            CustomOption.Create(
                GetRoleOptionId((int)BomberOption.TimerMinTime),
                string.Concat(
                    this.RoleName,
                    BomberOption.TimerMinTime.ToString()),
                15f, 5.0f, 30f, 0.5f,
                parentOps, format: OptionUnit.Second);
            CustomOption.Create(
                GetRoleOptionId((int)BomberOption.TimerMaxTime),
                string.Concat(
                    this.RoleName,
                    BomberOption.TimerMaxTime.ToString()),
                60f, 45f, 75f, 0.5f,
                parentOps, format: OptionUnit.Second);
            CustomOption.Create(
               GetRoleOptionId((int)BomberOption.TellExplosion),
               string.Concat(
                   this.RoleName,
                   BomberOption.TellExplosion.ToString()),
               true, parentOps);
        }

        protected override void RoleSpecificInit()
        {
            this.RoleAbilityInit();

            var allOption = OptionHolder.AllOption;

            this.timerMinTime = allOption[
                GetRoleOptionId((int)BomberOption.TimerMinTime)].GetValue();
            this.timerMaxTime = allOption[
                GetRoleOptionId((int)BomberOption.TimerMaxTime)].GetValue();
            this.explosionKillChance = allOption[
                GetRoleOptionId((int)BomberOption.ExplosionKillChance)].GetValue();
            this.explosionRange = allOption[
                GetRoleOptionId((int)BomberOption.ExplosionRange)].GetValue();
            this.tellExplosion = allOption[
                GetRoleOptionId((int)BomberOption.TellExplosion)].GetValue();

            this.bombPlayerId.Clear();
            resetTimer();

        }

        public void RoleAbilityResetOnMeetingStart()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }

        public void Update(PlayerControl rolePlayer)
        {
            if (rolePlayer.Data.IsDead || rolePlayer.Data.Disconnected) { return; }
            if (this.bombPlayerId.Count == 0) { return; }

            if (MeetingHud.Instance != null ||
                ShipStatus.Instance == null ||
                GameData.Instance == null) { return; }
            if (!ShipStatus.Instance.enabled ||
                ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return; }

            this.timer -= Time.deltaTime;
            if (this.timer > 0) { return; }

            resetTimer();

            byte bombTargetPlayerId = this.bombPlayerId.Dequeue();
            PlayerControl bombPlayer = Player.GetPlayerControlById(bombTargetPlayerId);

            if (bombPlayer == null) { return; }
            if (bombPlayer.Data.IsDead || bombPlayer.Data.Disconnected) { return; }
            
            HashSet<PlayerControl> target = getAllPlayerInExplosion(
                rolePlayer, bombPlayer);
            foreach (PlayerControl player in target)
            {
                if (explosionKillChance > Random.RandomRange(0, 100))
                {
                    explosionKill(rolePlayer, bombPlayer, player);
                }
            }
            explosionKill(rolePlayer, bombPlayer, bombPlayer);
            if (this.tellExplosion)
            {
                rolePlayer.StartCoroutine(
                    showText().WrapToIl2Cpp());
            }
        }

        private void resetTimer()
        {
            this.timer = Random.RandomRange(
                this.timerMinTime, this.timerMaxTime);
        }

        private HashSet<PlayerControl> getAllPlayerInExplosion(
            PlayerControl rolePlayer,
            PlayerControl sourcePlayer)
        {
            HashSet<PlayerControl> result = new HashSet<PlayerControl>();

            Vector2 truePosition = sourcePlayer.GetTruePosition();

            Il2CppSystem.Collections.Generic.List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                GameData.PlayerInfo playerInfo = allPlayers[i];

                if (!playerInfo.Disconnected &&
                    !playerInfo.IsDead &&
                    (playerInfo.PlayerId != sourcePlayer.PlayerId) &&
                    (!playerInfo.Object.inVent || OptionHolder.Ship.CanKillVentInPlayer) &&
                    (!ExtremeRoleManager.GameRole[playerInfo.PlayerId].IsImpostor() ||
                     playerInfo.PlayerId == rolePlayer.PlayerId))
                {
                    PlayerControl @object = playerInfo.Object;
                    if (@object)
                    {
                        Vector2 vector = @object.GetTruePosition() - truePosition;
                        float magnitude = vector.magnitude;
                        if (magnitude <= this.explosionRange &&
                            !PhysicsHelpers.AnyNonTriggersBetween(
                                truePosition, vector.normalized,
                                magnitude, Constants.ShipAndObjectsMask))
                        {
                            var bodyGuard = ExtremeRolesPlugin.GameDataStore.ShildPlayer.GetBodyGuardPlayerId(
                                @object.PlayerId);

                            var target = @object;

                            if (bodyGuard != byte.MaxValue)
                            {
                                target = Player.GetPlayerControlById(bodyGuard);
                                if (@object == null)
                                {
                                    target = @object;
                                }
                                else if (target.Data.IsDead || target.Data.Disconnected)
                                {
                                    target = @object;
                                }
                            }
                            result.Add(target);
                        }
                    }
                }
            }

            return result;

        }

        private void explosionKill(
            PlayerControl rolePlayer,
            PlayerControl bombPlayer,
            PlayerControl target)
        {

            RPCOperator.Call(
                rolePlayer.NetId,
                RPCOperator.Command.UncheckedMurderPlayer,
                new List<byte>
                {
                    bombPlayer.PlayerId,
                    target.PlayerId,
                    byte.MaxValue
                });
            RPCOperator.UncheckedMurderPlayer(
                bombPlayer.PlayerId,
                target.PlayerId,
                byte.MaxValue);

            RPCOperator.Call(
                rolePlayer.NetId,
                RPCOperator.Command.ReplaceDeadReason,
                new List<byte>
                {
                    target.PlayerId,
                    (byte)GameDataContainer.PlayerStatus.Explosion
                });
            ExtremeRolesPlugin.GameDataStore.ReplaceDeadReason(
                target.PlayerId, GameDataContainer.PlayerStatus.Explosion);
        }

        private IEnumerator showText()
        {
            if (this.tellText == null)
            {
                this.tellText = Object.Instantiate(
                    Prefab.Text, Camera.main.transform, false);
                this.tellText.transform.localPosition = new Vector3(-4.0f, -2.75f, -250.0f);
                this.tellText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                this.tellText.gameObject.layer = 5;
                this.tellText.text = Helper.Translation.GetString("explosionText");
            }
            this.tellText.gameObject.SetActive(true);

            yield return new WaitForSeconds(3.5f);

            this.tellText.gameObject.SetActive(false);

        }

    }
}
