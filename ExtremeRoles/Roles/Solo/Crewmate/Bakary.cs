﻿using ExtremeRoles.Module;
using ExtremeRoles.Module.CustomOption;
using ExtremeRoles.Module.SystemType;
using ExtremeRoles.Module.SystemType.Roles;
using ExtremeRoles.Roles.API;

namespace ExtremeRoles.Roles.Solo.Crewmate;

public sealed class Bakary : SingleRoleBase
{
    public enum BakaryOption
    {
        ChangeCooking,
        GoodBakeTime,
        BadBakeTime,
    }

    public Bakary() : base(
        ExtremeRoleId.Bakary,
        ExtremeRoleType.Crewmate,
        ExtremeRoleId.Bakary.ToString(),
        ColorPalette.BakaryWheatColor,
        false, true, false, false)
    { }

    protected override void CreateSpecificOption(
        IOptionInfo parentOps)
    {
        var changeCooking = CreateBoolOption(
            BakaryOption.ChangeCooking,
            true, parentOps);

        CreateFloatOption(
            BakaryOption.GoodBakeTime,
            60.0f, 45.0f, 75.0f, 0.5f,
            changeCooking, format: OptionUnit.Second,
            invert: true, enableCheckOption: parentOps);
        CreateFloatOption(
            BakaryOption.BadBakeTime,
            120.0f, 105.0f, 135.0f, 0.5f,
            changeCooking, format: OptionUnit.Second,
            invert: true, enableCheckOption: parentOps);
    }

    protected override void RoleSpecificInit()
    {
		var allOpt = OptionManager.Instance;

		ExtremeSystemTypeManager.Instance.TryAdd(
			ExtremeSystemType.BakeryReport,
			new BakerySystem(
				allOpt.GetValue<float>(
					GetRoleOptionId(BakaryOption.GoodBakeTime)),
				allOpt.GetValue<float>(
					GetRoleOptionId(BakaryOption.BadBakeTime)),
				allOpt.GetValue<bool>(
					GetRoleOptionId(BakaryOption.ChangeCooking))));
	}
}
