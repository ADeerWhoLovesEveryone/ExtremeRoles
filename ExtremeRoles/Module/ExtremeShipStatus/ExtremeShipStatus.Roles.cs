﻿using System.Collections.Generic;

using ExtremeRoles.Module.CustomMonoBehaviour;

using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.Solo.Crewmate;

namespace ExtremeRoles.Module.ExtremeShipStatus;

public sealed partial class ExtremeShipStatus
{
	public bool IsAssassinAssign => isAssignAssassin;
	public bool AssassinMeetingTrigger => this.assassinMeetingTrigger;
	public byte ExiledAssassinId => this.meetingCallAssassin;

	private bool isAssassinateMarin = false;
	private bool isAssignAssassin = false;
	private bool assassinMeetingTrigger = false;
	private byte meetingCallAssassin = byte.MaxValue;
	private byte isTargetPlayerId = byte.MaxValue;

	private Queue<byte> deadedAssassin = new Queue<byte>();

	public void AddGlobalActionRole(SingleRoleBase role)
	{
		switch (role.Id)
		{
			case ExtremeRoleId.Assassin:
				this.isAssignAssassin = true;
				break;
			default:
				break;
		}
	}

	// アサシン周り
	public void AddDeadAssasin(byte playerId)
	{
		this.deadedAssassin.Enqueue(playerId);
	}

	public void AssassinMeetingTriggerOn(byte assassinPlayerId)
	{
		this.meetingCallAssassin = assassinPlayerId;
		this.assassinMeetingTrigger = true;
	}

	public void AssassinMeetingTriggerOff()
	{
		this.assassinMeetingTrigger = false;
	}

	public bool isMarinPlayer(byte playerId) => playerId == this.isTargetPlayerId;

	private void resetGlobalAction()
	{
		this.isAssassinateMarin = false;
		this.isAssignAssassin = false;
		this.assassinMeetingTrigger = false;
		this.meetingCallAssassin = byte.MaxValue;
		this.isTargetPlayerId = byte.MaxValue;
		this.deadedAssassin.Clear();
	}
}
