﻿using System;
using ExtremeRoles.Resources;
using UnityEngine;

namespace ExtremeRoles.Test.Lobby.Asset;

public class CommonAssetLoadRunner
	: AssetLoadRunner
{
	public CommonAssetLoadRunner()
	{
		this.IsDebugOnly = true;
	}

	public override IEnumerator Run()
	{
		Log.LogInfo($"----- Unit:SingleImgLoad Test -----");
#if DEBUG
		imgTest(ObjectPath.Bomb);
		imgTest(ObjectPath.Meeting);

		yield break;
	}

	private void imgTest(string img)
	{
		try
		{
			var sprite = UnityObjectLoader.GetUnityObjectFromExRResources<Sprite>(img);
			Log.LogInfo($"Img Loaded: {img}");
			NullCheck(sprite);
		}
		catch (Exception ex)
		{
			Log.LogError(
				$"Img: {img} not load   {ex.Message}");
		}
# elif RELEASE
		yield break;
#endif
	}
}
