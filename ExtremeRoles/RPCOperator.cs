﻿using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace ExtremeRoles
{
    public static class RPCOperator
    {

        public enum Command
        {
            // メインコントール
            Initialize = 60,
            RoleSetUpComplete,
            ForceEnd,
            SetNormalRole,
            SetCombinationRole,
            ShareOption,
            CustomVentUse,
            StartVentAnimation,
            UncheckedShapeShift,
            UncheckedMurderPlayer,
            CleanDeadBody,
            FixLightOff,
            ReplaceDeadReason,
            SetRoleWin,
            SetWinGameControlId,
            SetWinPlayer,
            ShareMapId,
            ShareVersion,

            // 役職関連
            // 役職メインコントール
            ReplaceRole,

            // クルーメイト
            BodyGuardFeatShield,
            BodyGuardResetShield,
            TimeMasterShieldOn,
            TimeMasterShieldOff,
            TimeMasterRewindTime,
            TimeMasterResetMeeting,
            AgencyTakeTask,
            AgencySetNewTask,
            FencerCounterOn,
            FencerCounterOff,
            FencerEnableKillButton,
            CuresMakerCurseKillCool,

            // インポスター
            AssasinVoteFor,
            CarrierCarryBody,
            CarrierSetBody,
            PainterPaintBody,
            FakerCreateDummy,
            OverLoaderSwitchAbility,
            CrackerCrackDeadBody,
            MerySetCamp,
            MeryAcivateVent,
            SlaveDriverSetNewTask,


            // ニュートラル
            AliceShipBroken,
            TaskMasterSetNewTask,
            JesterOutburstKill,
            YandereSetOneSidedLover,
        }

        public static void Call(
            uint netId, Command ops)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                netId, (byte)ops,
                Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void Call(
            uint netId, Command ops, List<byte> value)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                netId, (byte)ops,
                Hazel.SendOption.Reliable, -1);
            foreach (byte writeVale in value)
            {
                writer.Write(writeVale);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RoleIsWin(byte playerId)
        {
            Call(PlayerControl.LocalPlayer.NetId,
                Command.SetRoleWin, new List<byte>{ playerId });
            SetRoleWin(playerId);
        }

        public static void CleanDeadBody(byte targetId)
        {
            DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            for (int i = 0; i < array.Length; ++i)
            {
                if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == targetId)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                    break;
                }
            }
        }

        public static void Initialize()
        {
            OptionHolder.Load();
            RandomGenerator.Initialize();
            Helper.Player.ResetTarget();
            Roles.ExtremeRoleManager.Initialize();
            ExtremeRolesPlugin.GameDataStore.Initialize();
            ExtremeRolesPlugin.Info.ResetOverlays();

            Patches.KillAnimationCoPerformKillPatch.HideNextAnimation = false;
        }

        public static void ForceEnd()
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.Role.IsImpostor)
                {
                    player.RemoveInfected();
                    player.MurderPlayer(player);
                    player.Data.IsDead = true;
                }
            }
        }
        public static void FixLightOff()
        {

            var switchMiniGame = Minigame.Instance as SwitchMinigame;

            if (switchMiniGame != null)
            {
                Minigame.Instance.ForceClose();
            }

            SwitchSystem switchSystem = ShipStatus.Instance.Systems[
                SystemTypes.Electrical].Cast<SwitchSystem>();
            switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
        }

        public static void SetCombinationRole(
            byte roleId, byte playerId, byte id, byte bytedRoleType)
        {
            Roles.ExtremeRoleManager.SetPlayerIdToMultiRoleId(
                roleId, playerId, id, bytedRoleType);
        }

        public static void SetNormalRole(byte roleId, byte playerId)
        {
            Roles.ExtremeRoleManager.SetPlyerIdToSingleRoleId(
                roleId, playerId);
        }

        public static void ShareOption(int numOptions, MessageReader reader)
        {
            OptionHolder.ShareOption(numOptions, reader);
        }

        public static void ReplaceDeadReason(byte playerId, byte reason)
        {
            ExtremeRolesPlugin.GameDataStore.ReplaceDeadReason(
                playerId, (Module.GameDataContainer.PlayerStatus)reason);
        }

        public static void CustomVentUse(
            int ventId, byte playerId, byte isEnter)
        {
            PlayerControl player = Helper.Player.GetPlayerControlById(playerId);
            if (player == null) { return; }

            MessageReader reader = new MessageReader();
            
            byte[] bytes = System.BitConverter.GetBytes(ventId);
            if (!System.BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(bytes);
            }
            reader.Buffer = bytes;
            reader.Length = bytes.Length;

            Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault(
                (x) => x.Id == ventId);

            var ventContainer = ExtremeRolesPlugin.GameDataStore.CustomVent;

            HudManager.Instance.StartCoroutine(
                Effects.Lerp(
                    0.6f, new System.Action<float>((p) => {
                        if (vent != null && vent.myRend != null)
                        {
                            vent.myRend.sprite = ventContainer.GetVentSprite(
                                ventId, (int)(p * 17));
                            if (p == 1f)
                            {
                                vent.myRend.sprite = ventContainer.GetVentSprite(
                                    ventId, 0);
                            }
                        }
                    })));

            player.MyPhysics.HandleRpc(isEnter != 0 ? (byte)19 : (byte)20, reader);
        }

        public static void StartVentAnimation(int ventId)
        {

            if (ShipStatus.Instance == null) { return; }
            Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault(
                (x) => x.Id == ventId);

            var ventContainer = ExtremeRolesPlugin.GameDataStore.CustomVent;

            if (ventContainer.IsCustomVent(ventId))
            {
                if (HudManager.Instance == null) { return; }

                HudManager.Instance.StartCoroutine(
                    Effects.Lerp(
                        0.6f, new System.Action<float>((p) => {
                            if (vent != null && vent.myRend != null)
                            {
                                vent.myRend.sprite = ventContainer.GetVentSprite(
                                    ventId, (int)(p * 17));
                                if (p == 1f)
                                {
                                    vent.myRend.sprite = ventContainer.GetVentSprite(
                                        ventId, 0);
                                }
                            }   
                        })
                    )
                );
            }
            else
            {
                vent?.GetComponent<PowerTools.SpriteAnim>()?.Play(
                    vent.ExitVentAnim, 1f);
            }
        }

        public static void UncheckedShapeShift(
            byte sourceId, byte targetId, byte useAnimation)
        {
            PlayerControl source = Helper.Player.GetPlayerControlById(sourceId);
            PlayerControl target = Helper.Player.GetPlayerControlById(targetId);

            bool animate = true;

            if (useAnimation != byte.MaxValue)
            {
                animate = false;
            }
            source.Shapeshift(target, animate);
        }

        public static void UncheckedMurderPlayer(
            byte sourceId, byte targetId, byte useAnimation)
        {

            PlayerControl source = Helper.Player.GetPlayerControlById(sourceId);
            PlayerControl target = Helper.Player.GetPlayerControlById(targetId);

            if (source != null && target != null)
            {
                if (useAnimation == 0)
                {
                    Patches.KillAnimationCoPerformKillPatch.HideNextAnimation = true;
                }
                source.MurderPlayer(target);

                if (!target.Data.IsDead) { return; }

                var targetRole = Roles.ExtremeRoleManager.GameRole[targetId];
                var multiAssignRole = targetRole as Roles.API.MultiAssignRoleBase;

                if (Roles.ExtremeRoleManager.IsDisableWinCheckRole(targetRole))
                {
                    ExtremeRolesPlugin.GameDataStore.WinCheckDisable = true;
                }

                targetRole.RolePlayerKilledAction(
                    target, source);
                if (multiAssignRole != null)
                {
                    if (multiAssignRole.AnotherRole != null)
                    {
                        multiAssignRole.AnotherRole.RolePlayerKilledAction(
                            target, source);
                    }
                }

                ExtremeRolesPlugin.GameDataStore.WinCheckDisable = false;

                var player = PlayerControl.LocalPlayer;

                if (player.PlayerId != targetId)
                {
                    var hockRole = Roles.ExtremeRoleManager.GameRole[
                        player.PlayerId] as Roles.API.Interface.IRoleMurderPlayerHock;
                    multiAssignRole = Roles.ExtremeRoleManager.GameRole[
                        player.PlayerId] as Roles.API.MultiAssignRoleBase;

                    if (hockRole != null)
                    {
                        hockRole.HockMuderPlayer(
                            source, target);
                    }
                    if (multiAssignRole != null)
                    {
                        hockRole = multiAssignRole.AnotherRole as Roles.API.Interface.IRoleMurderPlayerHock;
                        if (hockRole != null)
                        {
                            hockRole.HockMuderPlayer(
                                source, target);
                        }
                    }
                    
                }

            }
        }

        public static void SetWinGameControlId(int id)
        {
            ExtremeRolesPlugin.GameDataStore.WinGameControlId = id;
        }

        public static void SetWinPlayer(List<byte> playerId)
        {
            foreach (byte id in playerId)
            {
                PlayerControl player = Helper.Player.GetPlayerControlById(id);
                if (player == null) { continue; }
                ExtremeRolesPlugin.GameDataStore.PlusWinner.Add(player);
            }
        }

        public static void SetRoleWin(byte winPlayerId)
        {
            Roles.ExtremeRoleManager.GameRole[winPlayerId].IsWin = true;
        }
        public static void ShareMapId(byte mapId)
        {
            PlayerControl.GameOptions.MapId = mapId;
        }

        public static void AddVersionData(
            int major, int minor,
            int build, int revision, int clientId)
        {
            ExtremeRolesPlugin.GameDataStore.PlayerVersion[
                clientId] = new System.Version(
                    major, minor, build, revision);
        }

        public static void ReplaceRole(
            byte callerId, byte targetId, byte operation)
        {
            Roles.ExtremeRoleManager.RoleReplace(
                callerId, targetId,
                (Roles.ExtremeRoleManager.ReplaceOperation)operation);
        }

        public static void BodyGuardFeatShield(
            byte playerId,
            byte targetPlayer)
        {
            ExtremeRolesPlugin.GameDataStore.ShildPlayer.Add(
                playerId, targetPlayer);
        }

        public static void BodyGuardResetShield(byte playerId)
        {
            ExtremeRolesPlugin.GameDataStore.ShildPlayer.Remove(playerId);
        }
        public static void TimeMasterShieldOn(
            byte playerId)
        {
            Roles.Solo.Crewmate.TimeMaster.ShieldOn(playerId);
        }
        public static void TimeMasterShieldOff(byte playerId)
        {
            Roles.Solo.Crewmate.TimeMaster.ShieldOff(playerId);
        }
        public static void TimeMasterRewindTime(byte playerId)
        {
            Roles.Solo.Crewmate.TimeMaster.TimeRewind(playerId);
        }
        public static void TimeMasterResetMeeting(byte playerId)
        {
            Roles.Solo.Crewmate.TimeMaster.ResetMeeting(playerId);
        }
        public static void AgencyTakeTask(
            byte targetPlayerId, List<int> getTaskId)
        {
            Roles.Solo.Crewmate.Agency.TakeTargetPlayerTask(
                targetPlayerId, getTaskId);
        }
        public static void AgencySetNewTask(
            byte callerId, int index, int taskIndex)
        {
            Roles.Solo.Crewmate.Agency.ReplaceToNewTask(
                callerId, index, taskIndex);
        }
        public static void FencerCounterOn(
            byte playerId)
        {
            Roles.Solo.Crewmate.Fencer.CounterOn(playerId);
        }
        public static void FencerCounterOff(byte playerId)
        {
            Roles.Solo.Crewmate.Fencer.CounterOff(playerId);
        }
        public static void FencerEnableKillButton(byte playerId)
        {
            Roles.Solo.Crewmate.Fencer.EnableKillButton(playerId);
        }

        public static void CuresMakerCurseKillCool(
            byte playerId, byte targetPlayerId)
        {
            Roles.Solo.Crewmate.CurseMaker.CurseKillCool(
                playerId, targetPlayerId);
        }

        public static void AssasinVoteFor(byte targetId)
        {
            Roles.Combination.Assassin.VoteFor(
                targetId);
        }
        public static void CarrierCarryBody(
            byte callerId, byte targetId)
        {
            Roles.Solo.Impostor.Carrier.CarryDeadBody(
                callerId, targetId);
        }
        public static void CarrierSetBody(byte callerId)
        {
            Roles.Solo.Impostor.Carrier.PlaceDeadBody(
                callerId);
        }
        public static void PainterPaintBody(
            byte callerId, byte targetId)
        {
            Roles.Solo.Impostor.Painter.PaintDeadBody(
                callerId, targetId);
        }
        public static void FakerCreateDummy(
            byte callerId, byte targetId)
        {
            Roles.Solo.Impostor.Faker.CreateDummy(
                callerId, targetId);
        }

        public static void OverLoaderSwitchAbility(
            byte callerId, byte activate)
        {

            Roles.Solo.Impostor.OverLoader.SwitchAbility(
                callerId, activate == byte.MaxValue);
        }

        public static void CrackerCrackDeadBody(
            byte callerId, byte targetId)
        {
            Roles.Solo.Impostor.Cracker.CrackDeadBody(
                callerId, targetId);
        }

        public static void MarySetCamp(byte callerId)
        {
            Roles.Solo.Impostor.Mery.SetCamp(callerId);
        }
        public static void MaryActiveVent(int index)
        {
            Roles.Solo.Impostor.Mery.ActivateVent(index);
        }
        public static void SlaveDriverSetNewTask(
            byte callerId, int index, int taskIndex)
        {
            Roles.Solo.Impostor.SlaveDriver.ReplaceToNewTask(
                callerId, index, taskIndex);
        }

        public static void AliceShipBroken(
            byte callerId, byte targetPlayerId, List<int> taskId)
        {
            Roles.Solo.Neutral.Alice.ShipBroken(
                callerId, targetPlayerId, taskId);
        }

        public static void TaskMasterSetNewTask(
            byte callerId, int index, int taskIndex)
        {
            Roles.Solo.Neutral.TaskMaster.ReplaceToNewTask(
                callerId, index, taskIndex);
        }
        public static void JesterOutburstKill(
            byte killerId, byte targetId)
        {
            Roles.Solo.Neutral.Jester.OutburstKill(
                killerId, targetId);
        }
        public static void YandereSetOneSidedLover(
            byte playerId, byte loverId)
        {
            Roles.Solo.Neutral.Yandere.SetOneSidedLover(
                playerId, loverId);
        }

    }

}
