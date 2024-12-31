﻿using System.Collections.Generic;

using ExtremeRoles.Helper;
using ExtremeRoles.Module.CustomMonoBehaviour;
using ExtremeRoles.Performance.Il2Cpp;

using Player = NetworkedPlayerInfo;

#nullable enable

namespace ExtremeRoles.Module.GameResult;

public sealed class ExtremeGameResultManager : NullableSingleton<ExtremeGameResultManager>
{
	public readonly record struct TaskInfo(int CompletedTask, int TotalTask);

	public WinnerTempData.Result Winner => winner.Convert();
	public IReadOnlyList<FinalSummary.PlayerSummary> PlayerSummaries { get; private set; }

	private readonly int winGameControlId;
	private readonly Dictionary<byte, TaskInfo> playerTaskInfo = new Dictionary<byte, TaskInfo>();
	private WinnerTempData winner;

	public ExtremeGameResultManager()
	{
		this.winner = new WinnerTempData();
		this.PlayerSummaries = new List<FinalSummary.PlayerSummary>();
		this.winGameControlId = ExtremeRolesPlugin.ShipState.WinGameControlId;
	}

	public void CreateTaskInfo()
	{
		foreach (Player playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
		{
			var (completedTask, totalTask) = GameSystem.GetTaskInfo(playerInfo);
			this.playerTaskInfo.Add(
				playerInfo.PlayerId,
				new TaskInfo(completedTask, totalTask));
			this.winner.AddPool(playerInfo);
		}
	}

	public void CreateEndGameManagerResult()
	{
		this.winner.SetWinner();

		var builder = new WinnerBuilder(this.winGameControlId, this.winner, this.playerTaskInfo);
		this.PlayerSummaries = builder.Build();
	}
}
