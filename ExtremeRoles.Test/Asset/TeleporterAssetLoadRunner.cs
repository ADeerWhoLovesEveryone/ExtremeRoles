﻿using ExtremeRoles.Resources;
using ExtremeRoles.Roles;
using UnityEngine;

namespace ExtremeRoles.Test.Asset;

internal sealed class TeleporterAssetLoadRunner
	: AssetLoadRunner
{
	public override void Run()
	{
		Log.LogInfo($"----- Unit:TeleporterImgLoad Test -----");

		LoadFromExR(ExtremeRoleId.Teleporter, ObjectPath.TeleporterNoneActivatePortal);
		LoadFromExR(ExtremeRoleId.Teleporter, ObjectPath.TeleporterFirstPortal);
		LoadFromExR(ExtremeRoleId.Teleporter, ObjectPath.TeleporterSecondPortal);
		LoadFromExR(ExtremeRoleId.Teleporter, ObjectPath.TeleporterPortalBase);
	}
}
