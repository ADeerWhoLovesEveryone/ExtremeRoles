﻿using System;
using UnityEngine;

using ExtremeRoles.Performance;

namespace ExtremeRoles.Module.CustomMonoBehaviour;

[Il2CppRegister]
public sealed class VentInPlayerPosSyncer : MonoBehaviour
{
	private float timer = 0.0f;

	private Vent vent;
	private VentilationSystem ventilationSystem;
	private PlayerControl localPlayer;

	public VentInPlayerPosSyncer(IntPtr ptr) : base(ptr) { }

	public void Awake()
	{
		this.timer = 0.0f;

		this.vent = base.gameObject.GetComponent<Vent>();
		this.localPlayer = CachedPlayerControl.LocalPlayer;
		setSystem();
	}

	public void FixedUpdate()
	{
		this.timer += Time.fixedDeltaTime;

		if (this.timer < 0.15f ||
			AmongUsClient.Instance.IsGameOver ||
			!this.ventilationSystem.PlayersInsideVents.TryGetValue(
				this.localPlayer.PlayerId, out byte ventId) ||
			this.vent.Id != ventId) { return; }

		Vector2 pos = this.vent.transform.position;
		pos -= this.localPlayer.Collider.offset;

		this.localPlayer.transform.position = pos;
		this.vent.SetButtons(true);
	}

	private void setSystem()
	{
		if (!CachedShipStatus.Instance.Systems.TryGetValue(
				SystemTypes.Ventilation, out ISystemType systemType))
		{
			return;
		}
		this.ventilationSystem = systemType.Cast<VentilationSystem>();
	}
}
