﻿using System;
using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using ExtremeRoles.Module.Interface;
using ExtremeRoles.Module.Ability.Behavior;
using ExtremeRoles.Helper;

namespace ExtremeRoles.Module.Ability;

#nullable enable

public class ExtremeMultiModalAbilityButton : ExtremeAbilityButton
{
	public int MultiModalAbilityNum => allAbility.Count;
	private readonly List<BehaviorBase> allAbility;
	private int curIndex = 0;

	public ExtremeMultiModalAbilityButton(
		List<BehaviorBase> behaviorors,
		IButtonAutoActivator activator,
		KeyCode hotKey) : base(
			behaviorors[0],
			activator,
			hotKey)
	{
		this.allAbility = behaviorors.ToList();
	}

	public ExtremeMultiModalAbilityButton(
		IButtonAutoActivator activator,
		KeyCode hotKey,
		params BehaviorBase[] behaviorors) : this(
			behaviorors.ToList(),
			activator,
			hotKey)
	{ }

	public void Add(BehaviorBase behavior)
	{
		this.allAbility.Add(behavior);
	}
	public void Remove(int index)
	{
		var targetAbility = this.allAbility[index];
		checkForRemove(targetAbility);
		this.allAbility.RemoveAt(index);
	}
	public void Remove(in BehaviorBase behavior)
	{
		checkForRemove(behavior);
		this.allAbility.Remove(behavior);
	}

	public void ClearAndAnd(BehaviorBase behavior)
	{
		foreach (var item in this.allAbility)
		{
			if (this.Behavior == item)
			{
				continue;
			}
			Remove(item);
		}
		Add(behavior);
		var curAbility = this.Behavior;
		switchAbility(false);
		Remove(curAbility);
	}

	protected override void UpdateImp()
	{
		if (this.MultiModalAbilityNum > 1 &&
			this.State is not AbilityState.Activating)
		{
			if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				switchAbility(false);
				return;
			}
			else if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				switchAbility(true);
				return;
			}
		}
		base.UpdateImp();
	}

	private void checkForRemove(in BehaviorBase removeBehaviour)
	{
		if (this.allAbility.Count == 1)
		{
			throw new IndexOutOfRangeException("Can't remove only ability!!");
		}

		if (removeBehaviour != this.Behavior)
		{
			return;
		}
		switchAbility(false);
	}

	private void switchAbility(bool isInvert)
	{
		int curAbilityNum = this.MultiModalAbilityNum;
		int rowIndex = isInvert ? this.curIndex - 1 : this.curIndex + 1;
		int newIndex = (rowIndex + curAbilityNum) % curAbilityNum;

		float curMaxCoolTime = this.Behavior.CoolTime;
		this.Behavior = this.allAbility[newIndex];
		this.curIndex = newIndex;

		ExtremeRolesPlugin.Logger.LogInfo($"Switched to {this.Behavior.GetType().Name}, Index:{newIndex}");

		// クールタイム詐称を防ぐ(CT長いの使う => 短いのに切り替え => CTがカットされる => 長いのに切り替えて詐称)
		// 短いのから長いのに切り替えた瞬間にクールタイムが発生するようにする
		float diff = this.Behavior.CoolTime - curMaxCoolTime;
		if (diff > 0.0f)
		{
			this.AddTimerOffset(diff);
		}
	}
}
