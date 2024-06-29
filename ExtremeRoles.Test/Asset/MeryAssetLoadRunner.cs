﻿using ExtremeRoles.Resources;
using ExtremeRoles.Roles;
using UnityEngine;

namespace ExtremeRoles.Test.Asset;

internal sealed class MeryAssetLoadRunner
	: AssetLoadRunner
{
	public override void Run()
	{
		Log.LogInfo($"----- Unit:MeryImgLoad Test -----");

		for (int index = 0; index < 18; ++index)
		{
			LoadFromExR(ExtremeRoleId.Mery, $"{index}");
		}

		LoadFromExR(
			ExtremeRoleId.Mery,
			ObjectPath.MeryNoneActive);

		LoadFromExR(ExtremeRoleId.Mery);
	}
}
