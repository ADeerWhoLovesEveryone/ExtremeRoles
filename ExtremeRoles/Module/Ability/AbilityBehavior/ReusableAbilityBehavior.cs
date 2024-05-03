﻿using System;
using UnityEngine;

namespace ExtremeRoles.Module.Ability.AbilityBehavior;

public sealed class ReusableAbilityBehavior : AbilityBehaviorBase
{
	private Func<bool> ability;
	private Func<bool> canUse;
	private Func<bool> canActivating;
	private Action forceAbilityOff;
	private Action abilityOff;

	public ReusableAbilityBehavior(
		string text, Sprite img,
		Func<bool> canUse,
		Func<bool> ability,
		Func<bool> canActivating = null,
		Action abilityOff = null,
		Action forceAbilityOff = null) : base(text, img)
	{
		this.ability = ability;
		this.canUse = canUse;

		this.abilityOff = abilityOff;
		this.forceAbilityOff = forceAbilityOff ?? abilityOff;

		this.canActivating = canActivating ?? new Func<bool>(() => { return true; });
	}

	public override void Initialize(ActionButton button)
	{
		return;
	}

	public override void AbilityOff()
	{
		abilityOff?.Invoke();
	}

	public override void ForceAbilityOff()
	{
		forceAbilityOff?.Invoke();
	}

	public override bool IsCanAbilityActiving() => canActivating.Invoke();

	public override bool IsUse() => canUse.Invoke();

	public override bool TryUseAbility(
		float timer, AbilityState curState, out AbilityState newState)
	{
		newState = curState;

		if (timer > 0 || curState != AbilityState.Ready)
		{
			return false;
		}

		if (!ability.Invoke())
		{
			return false;
		}

		newState = ActiveTime <= 0.0f ?
			AbilityState.CoolDown : AbilityState.Activating;

		return true;
	}

	public override AbilityState Update(AbilityState curState)
	{
		return curState;
	}
}
