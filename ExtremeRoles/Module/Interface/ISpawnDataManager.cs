﻿using System;

using ExtremeRoles.GameMode.RoleSelector;
using ExtremeRoles.Module.CustomOption;

namespace ExtremeRoles.Module.Interface;

public interface ISpawnDataManager
{
	protected static int ComputeSpawnNum(
		RoleSpawnOption minSpawnKey,
		RoleSpawnOption maxSpawnKey)
	{
		var allOption = OptionManager.Instance;

		int minSpawnNum = allOption.GetValue<int>((int)minSpawnKey);
		int maxSpawnNum = allOption.GetValue<int>((int)maxSpawnKey);

		// 最大値が最小値より小さくならないように
		maxSpawnNum = Math.Clamp(maxSpawnNum, minSpawnNum, int.MaxValue);

		return RandomGenerator.Instance.Next(minSpawnNum, maxSpawnNum + 1);
	}

	protected static int ComputePercentage(IValueOption<int> self)
		=> (int)decimal.Multiply(self.GetValue(), self.ValueCount);
}
