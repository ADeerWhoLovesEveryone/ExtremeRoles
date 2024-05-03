﻿namespace ExtremeRoles.Module.Ability.Behavior.Interface;

public interface ICountBehavior
{
	public const string DefaultButtonCountText = "buttonCountText";
	public int AbilityCount { get; }

	public void SetAbilityCount(int newAbilityNum);

	public void SetButtonTextFormat(string newTextFormat);
}
