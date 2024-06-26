﻿using System;


using ExtremeRoles.Module.CustomOption.Factory;
using ExtremeRoles.Module.CustomOption.Interfaces;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.API;


public enum ExtremeRoleType : int
{
    Null = -2,
    Neutral = -1,
    Crewmate = 0,
    Impostor = 1
}
public enum RoleCommonOption
{
	SpawnRate = 20,
	RoleNum,
    AssignWeight,
    HasOtherVision,
    Vision,
    ApplyEnvironmentVisionEffect,
}
public enum KillerCommonOption
{
    HasOtherKillRange = 40,
    KillRange,
    HasOtherKillCool,
    KillCoolDown,
}

public abstract class RoleOptionBase
{
    public bool CanKill = false;

	public abstract IOptionLoader Loader { get; }

	public void Initialize()
    {
        CommonInit();
        RoleSpecificInit();

		//TODO : 消して動くかチェック
		if (this is IRoleAbility ability)
		{
			ability.RoleAbilityInit();
		}
    }

    public void CreateRoleAllOption()
    {
		using var factory = CreateSpawnOption();
		this.CreateRoleSpecificOption(factory);
    }
    public void CreateRoleSpecificOption(
        AutoParentSetOptionCategoryFactory factory, bool ignorePrefix = true)
    {
        CreateVisionOption(factory, ignorePrefix);

        if (this.CanKill)
        {
            CreateKillerOption(factory, ignorePrefix: ignorePrefix);
        }

        CreateSpecificOption(factory);
    }
    protected abstract void CreateKillerOption(
        AutoParentSetOptionCategoryFactory factory,
		IOption parent = null,
		bool ignorePrefix = true);
    protected abstract AutoParentSetOptionCategoryFactory CreateSpawnOption();

    protected abstract void CreateSpecificOption(
        AutoParentSetOptionCategoryFactory factory);
    protected abstract void CreateVisionOption(
        AutoParentSetOptionCategoryFactory factory, bool ignorePrefix = true);

    protected abstract void CommonInit();

    protected abstract void RoleSpecificInit();

    protected static void EnumCheck<T>(T isEnum) where T : struct, IConvertible
    {
        if (!typeof(int).IsAssignableFrom(Enum.GetUnderlyingType(typeof(T))))
        {
            throw new ArgumentException(nameof(T));
        }
    }
}
