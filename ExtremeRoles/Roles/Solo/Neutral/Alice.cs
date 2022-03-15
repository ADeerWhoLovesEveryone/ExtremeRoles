﻿using System.Collections.Generic;
using System.Linq;

using Hazel;

using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAbilityButton;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.Solo.Neutral
{
    public class Alice : SingleRoleBase, IRoleAbility
    {

        public enum AliceOption
        {
            RevartCommonTaskNum,
            RevartLongTaskNum,
            RevartNormalTaskNum,
        }

        public RoleAbilityButtonBase Button
        { 
            get => this.aliceShipBroken;
            set
            {
                this.aliceShipBroken = value;
            }
        }

        public int RevartLongTask = 0;
        public int RevartNormalTask = 0;
        public int RevartCommonTask = 0;

        private RoleAbilityButtonBase aliceShipBroken;

        public Alice(): base(
            ExtremeRoleId.Alice,
            ExtremeRoleType.Neutral,
            ExtremeRoleId.Alice.ToString(),
            ColorPalette.AliceGold,
            true, false, true, true)
        {}

        public void CreateAbility()
        {
            this.CreateAbilityCountButton(
                Helper.Translation.GetString("shipBroken"),
                Loader.CreateSpriteFromResources(
                    Path.AliceShipBroken));
        }

        public override bool IsSameTeam(SingleRoleBase targetRole)
        {
            var multiAssignRole = targetRole as MultiAssignRoleBase;

            if (multiAssignRole != null)
            {
                if (multiAssignRole.AnotherRole != null)
                {
                    return this.IsSameTeam(multiAssignRole.AnotherRole);
                }
            }
            if (OptionHolder.Ship.IsSameNeutralSameWin)
            {
                return this.Id == targetRole.Id;
            }
            else
            {
                return (this.Id == targetRole.Id) && this.IsSameControlId(targetRole);
            }
        }

        public bool IsAbilityUse()
        {
            return this.IsCommonUse();
        }

        public override void RolePlayerKilledAction(
            PlayerControl rolePlayer,
            PlayerControl killerPlayer)
        {
           if (ExtremeRoleManager.GameRole[killerPlayer.PlayerId].IsImpostor())
           {
                this.IsWin = true;
           }
        }

        public bool UseAbility()
        {
            foreach(var player in PlayerControl.AllPlayerControls)
            {

                var role = ExtremeRoleManager.GameRole[player.PlayerId];
                if (!role.HasTask) { continue; }

                List<int> addTaskId = new List<int>();

                for (int i = 0; i < this.RevartLongTask; ++i)
                {
                    addTaskId.Add(Helper.GameSystem.GetRandomLongTask());
                }
                for (int i = 0; i < this.RevartCommonTask; ++i)
                {
                    addTaskId.Add(Helper.GameSystem.GetRandomCommonTaskId());
                }
                for (int i = 0; i < this.RevartNormalTask; ++i)
                {
                    addTaskId.Add(Helper.GameSystem.GetRandomNormalTaskId());
                }

                var shuffled = addTaskId.OrderBy(
                    item => RandomGenerator.Instance.Next()).ToList();

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId,
                        (byte)RPCOperator.Command.AliceShipBroken,
                        Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                writer.Write(player.PlayerId);
                writer.Write(addTaskId.Count);
                foreach (int taskId in shuffled)
                {
                    writer.Write(taskId);
                }
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                ShipBroken(
                    PlayerControl.LocalPlayer.PlayerId,
                    player.PlayerId, addTaskId);
            }

            return true;
        }

        public static void ShipBroken(
            byte callerId, byte targetPlayerId, List<int> addTaskId)
        {

            var alice = ExtremeRoleManager.GetSafeCastedRole<Alice>(callerId);
            if (alice == null) { return; }
            var player = Helper.Player.GetPlayerControlById(targetPlayerId);
            var playerInfo = GameData.Instance.GetPlayerById(
                player.PlayerId);
            
            for (int i = 0; i < playerInfo.Tasks.Count; ++i)
            {
                if (addTaskId.Count == 0) { break; }

                if (playerInfo.Tasks[i].Complete)
                {
                    byte taskId = (byte)addTaskId[0];
                    addTaskId.RemoveAt(0);

                    playerInfo.Tasks[i] = new GameData.TaskInfo(
                        taskId, playerInfo.Tasks[i].Id);

                    NormalPlayerTask normalPlayerTask = 
                        UnityEngine.Object.Instantiate<NormalPlayerTask>(
                            ShipStatus.Instance.GetTaskById(taskId),
                            player.transform);
                    normalPlayerTask.Id = playerInfo.Tasks[i].Id;
                    normalPlayerTask.Owner = player;
                    normalPlayerTask.Initialize();

                    player.myTasks[(int)playerInfo.Tasks[i].Id] = normalPlayerTask;
                }
            }
            GameData.Instance.SetDirtyBit(
                1U << (int)player.PlayerId);

        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            this.CreateAbilityCountOption(
                parentOps, 2, 100);

            CustomOption.Create(
                this.GetRoleOptionId((int)AliceOption.RevartLongTaskNum),
                string.Concat(
                    this.RoleName,
                    AliceOption.RevartLongTaskNum.ToString()),
                1, 0, 15, 1, parentOps);
            CustomOption.Create(
                this.GetRoleOptionId((int)AliceOption.RevartCommonTaskNum),
                string.Concat(
                    this.RoleName,
                    AliceOption.RevartCommonTaskNum.ToString()),
                1, 0, 15, 1, parentOps);
            CustomOption.Create(
                this.GetRoleOptionId((int)AliceOption.RevartNormalTaskNum),
                string.Concat(
                    this.RoleName,
                    AliceOption.RevartNormalTaskNum.ToString()),
                1, 0, 15, 1, parentOps);

        }

        protected override void RoleSpecificInit()
        {
            var allOption = OptionHolder.AllOption;
            this.RevartNormalTask = allOption[
                GetRoleOptionId((int)AliceOption.RevartNormalTaskNum)].GetValue();
            this.RevartLongTask = allOption[
                GetRoleOptionId((int)AliceOption.RevartLongTaskNum)].GetValue();
            this.RevartCommonTask = allOption[
                GetRoleOptionId((int)AliceOption.RevartCommonTaskNum)].GetValue();

            this.RoleAbilityInit();
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }
    }
}
