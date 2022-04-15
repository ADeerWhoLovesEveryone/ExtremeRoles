﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Hazel;
using BepInEx.Configuration;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;

namespace ExtremeRoles
{
    public static class OptionHolder
    {

        public const int VanillaMaxPlayerNum = 15;
        public const int MaxImposterNum = 3;

        public static string[] SpawnRate = new string[] {
            "0%", "10%", "20%", "30%", "40%",
            "50%", "60%", "70%", "80%", "90%", "100%" };
        public static string[] OptionPreset = new string[] { 
            "preset1", "preset2", "preset3", "preset4", "preset5",
            "preset6", "preset7", "preset8", "preset9", "preset10" };
        public static string[] Range = new string[] { "short", "middle", "long"};

        public static int OptionsPage = 1;
        public static int SelectedPreset = 0;

        private static IRegionInfo[] defaultRegion;

        public static string ConfigPreset
        {
            get => $"Preset:{SelectedPreset}";
        }

        public enum CommonOptionKey
        {
            PresetSelection = 0,

            UseStrongRandomGen,

            MinCremateRoles,
            MaxCremateRoles,
            MinNeutralRoles,
            MaxNeutralRoles,
            MinImpostorRoles,
            MaxImpostorRoles,

            NumMeating,
            DisableSkipInEmergencyMeeting,
            NoVoteToSelf,
            DesableVent,
            EngineerUseImpostorVent,
            CanKillVentInPlayer,
            ParallelMedBayScans,
            RandomMap,
            IsSameNeutralSameWin,
            DisableNeutralSpecialForceEnd,
            EnableHorseMode
        }

        public static Dictionary<int, CustomOptionBase> AllOption = new Dictionary<int, CustomOptionBase>();

        public static void Create()
        {

            defaultRegion = ServerManager.DefaultRegions;

            CreateConfigOption();

            Roles.ExtremeRoleManager.GameRole.Clear();
            AllOption.Clear();

            CustomOption.Create(
                (int)CommonOptionKey.PresetSelection, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.PresetSelection.ToString()),
                OptionPreset, null, true);

            CustomOption.Create(
                (int)CommonOptionKey.UseStrongRandomGen, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.UseStrongRandomGen.ToString()), true);

            CustomOption.Create(
                (int)CommonOptionKey.MinCremateRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinCremateRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 1) * 2, 1, null, true);
            CustomOption.Create(
                (int)CommonOptionKey.MaxCremateRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxCremateRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 1) * 2, 1);

            CustomOption.Create(
                (int)CommonOptionKey.MinNeutralRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinNeutralRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 2) * 2, 1);
            CustomOption.Create(
                (int)CommonOptionKey.MaxNeutralRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxNeutralRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 2) * 2, 1);

            CustomOption.Create(
                (int)CommonOptionKey.MinImpostorRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinImpostorRoles.ToString()),
                0, 0, MaxImposterNum * 2, 1);
            CustomOption.Create(
                (int)CommonOptionKey.MaxImpostorRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxImpostorRoles.ToString()),
                0, 0, MaxImposterNum * 2, 1);

            CustomOption.Create(
                (int)CommonOptionKey.NumMeating,
                CommonOptionKey.NumMeating.ToString(),
                10, 0, 100, 1, null, true);

            var blockMeating = CustomOption.Create(
                (int)CommonOptionKey.DisableSkipInEmergencyMeeting,
                CommonOptionKey.DisableSkipInEmergencyMeeting.ToString(),
                false);
            CustomOption.Create(
                (int)CommonOptionKey.NoVoteToSelf,
                CommonOptionKey.NoVoteToSelf.ToString(),
                false, blockMeating);

            var ventOption = CustomOption.Create(
               (int)CommonOptionKey.DesableVent,
               CommonOptionKey.DesableVent.ToString(),
               false);
            CustomOption.Create(
                (int)CommonOptionKey.CanKillVentInPlayer,
                CommonOptionKey.CanKillVentInPlayer.ToString(),
                false, ventOption, invert: true);
            CustomOption.Create(
                (int)CommonOptionKey.EngineerUseImpostorVent,
                CommonOptionKey.EngineerUseImpostorVent.ToString(),
                false, ventOption, invert: true);

            CustomOption.Create(
                (int)CommonOptionKey.ParallelMedBayScans,
                CommonOptionKey.ParallelMedBayScans.ToString(), false);
            CustomOption.Create(
                (int)CommonOptionKey.RandomMap,
                CommonOptionKey.RandomMap.ToString(), false);

            CustomOption.Create(
                (int)CommonOptionKey.IsSameNeutralSameWin,
                CommonOptionKey.IsSameNeutralSameWin.ToString(),
                true);
            CustomOption.Create(
                (int)CommonOptionKey.DisableNeutralSpecialForceEnd,
                CommonOptionKey.DisableNeutralSpecialForceEnd.ToString(),
                false);
            
            CustomOption.Create(
                (int)CommonOptionKey.EnableHorseMode,
                CommonOptionKey.EnableHorseMode.ToString(),
                false);

            int offset = 50;

            Roles.ExtremeRoleManager.CreateNormalRoleOptions(
                offset);

            offset = 5000;
            Roles.ExtremeRoleManager.CreateCombinationRoleOptions(
                offset);
        }

        public static void CreateConfigOption()
        {
            var config = ExtremeRolesPlugin.Instance.Config;

            ConfigParser.StreamerMode = config.Bind(
                "ClientOption", "StreamerMode", false);
            ConfigParser.GhostsSeeTasks = config.Bind(
                "ClientOption", "GhostCanSeeRemainingTasks", true);
            ConfigParser.GhostsSeeRoles = config.Bind(
                "ClientOption", "GhostCanSeeRoles", true);
            ConfigParser.GhostsSeeVotes = config.Bind(
                "ClientOption", "GhostCanSeeVotes", true);
            ConfigParser.ShowRoleSummary = config.Bind(
                "ClientOption", "IsShowRoleSummary", true);
            ConfigParser.HideNamePlate = config.Bind(
                "ClientOption", "IsHideNamePlate", false);

            ConfigParser.StreamerModeReplacementText = config.Bind(
                "ClientOption",
                "ReplacementRoomCodeText",
                "\n\nPlaying with Extreme Roles");

            ConfigParser.Ip = config.Bind(
                "ClientOption", "CustomServerIP", "127.0.0.1");
            ConfigParser.Port = config.Bind(
                "ClientOption", "CustomServerPort", (ushort)22023);
        }

        public static void Load()
        {

            Ship.MaxNumberOfMeeting = Mathf.RoundToInt(
                AllOption[(int)CommonOptionKey.NumMeating].GetValue());
            Ship.AllowParallelMedBayScan = AllOption[
                (int)CommonOptionKey.ParallelMedBayScans].GetValue();
            Ship.BlockSkippingInEmergencyMeeting = AllOption[
                (int)CommonOptionKey.DisableSkipInEmergencyMeeting].GetValue();
            Ship.DisableVent = AllOption[
                (int)CommonOptionKey.DesableVent].GetValue();
            Ship.CanKillVentInPlayer = AllOption[
                (int)CommonOptionKey.CanKillVentInPlayer].GetValue();
            Ship.EngineerUseImpostorVent = AllOption[
                (int)CommonOptionKey.EngineerUseImpostorVent].GetValue();
            Ship.NoVoteIsSelfVote = AllOption[
                (int)CommonOptionKey.NoVoteToSelf].GetValue();
            Ship.IsSameNeutralSameWin = AllOption[
                (int)CommonOptionKey.IsSameNeutralSameWin].GetValue();
            Ship.DisableNeutralSpecialForceEnd = AllOption[
                (int)CommonOptionKey.DisableNeutralSpecialForceEnd].GetValue();

            Client.StreamerMode = ConfigParser.StreamerMode.Value;
            Client.GhostsSeeRole = ConfigParser.GhostsSeeRoles.Value;
            Client.GhostsSeeTask = ConfigParser.GhostsSeeTasks.Value;
            Client.GhostsSeeVote = ConfigParser.GhostsSeeVotes.Value;
            Client.ShowRoleSummary = ConfigParser.ShowRoleSummary.Value;
            Client.HideNamePlate = ConfigParser.HideNamePlate.Value;
        }


        public static void SwitchPreset(int newPreset)
        {
            SelectedPreset = newPreset;
            foreach (CustomOptionBase option in AllOption.Values)
            {
                if (option.Id == 0) { continue; }

                option.Entry = ExtremeRolesPlugin.Instance.Config.Bind(
                    ConfigPreset,
                    option.CleanedName,
                    option.DefaultSelection);
                option.CurSelection = Mathf.Clamp(
                    option.Entry.Value, 0,
                    option.Selections.Length - 1);

                if (option.Behaviour != null && option.Behaviour is StringOption stringOption)
                {
                    stringOption.oldValue = stringOption.Value = option.CurSelection;
                    stringOption.ValueText.text = option.GetString();
                }
            }
        }

        public static void ShareOptionSelections()
        {
            if (PlayerControl.AllPlayerControls.Count <= 1 ||
                AmongUsClient.Instance?.AmHost == false &&
                PlayerControl.LocalPlayer == null) { return; }

            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(
                PlayerControl.LocalPlayer.NetId,
                (byte)RPCOperator.Command.ShareOption, Hazel.SendOption.Reliable);
            messageWriter.WritePacked((uint)AllOption.Count);
            
            foreach (var(id, option) in AllOption)
            {
                messageWriter.WritePacked((uint)id);
                messageWriter.WritePacked(Convert.ToUInt32(option.CurSelection));
            }
            
            messageWriter.EndMessage();
        }
        public static void ShareOption(int numberOfOptions, MessageReader reader)
        {
            try
            {
                for (int i = 0; i < numberOfOptions; i++)
                {
                    uint optionId = reader.ReadPackedUInt32();
                    uint selection = reader.ReadPackedUInt32();
                    AllOption[(int)optionId].UpdateSelection((int)selection);
                }
            }
            catch (Exception e)
            {
                Logging.Error($"Error while deserializing options:{e.Message}");
            }
        }

        public static void UpdateRegion()
        {
            ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
            IRegionInfo[] regions = defaultRegion;

            var CustomRegion = new DnsRegionInfo(
                ConfigParser.Ip.Value,
                "custom",
                StringNames.NoTranslation,
                ConfigParser.Ip.Value,
                ConfigParser.Port.Value,
                false);
            regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
            ServerManager.DefaultRegions = regions;
            serverManager.AvailableRegions = regions;
        }

        public static class ConfigParser
        {
            public static ConfigEntry<bool> GhostsSeeTasks { get; set; }
            public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
            public static ConfigEntry<bool> GhostsSeeVotes { get; set; }
            public static ConfigEntry<bool> ShowRoleSummary { get; set; }
            public static ConfigEntry<bool> StreamerMode { get; set; }
            public static ConfigEntry<bool> HideNamePlate { get; set; }
            public static ConfigEntry<string> StreamerModeReplacementText { get; set; }
            public static ConfigEntry<string> Ip { get; set; }
            public static ConfigEntry<ushort> Port { get; set; }
        }

        public static class Client
        {
            public static bool GhostsSeeRole = true;
            public static bool GhostsSeeTask = true;
            public static bool GhostsSeeVote = true;
            public static bool ShowRoleSummary = true;
            public static bool StreamerMode = false;
            public static bool HideNamePlate = false;
        }

        public static class Ship
        {
            public static int MaxNumberOfMeeting = 100;
            public static bool AllowParallelMedBayScan = false;
            public static bool BlockSkippingInEmergencyMeeting = false;
            public static bool DisableVent = false;
            public static bool EngineerUseImpostorVent = false;
            public static bool CanKillVentInPlayer = false;
            public static bool NoVoteIsSelfVote = false;
            public static bool IsSameNeutralSameWin = true;
            public static bool DisableNeutralSpecialForceEnd = false;
        }
    }
}
