﻿namespace ExtremeRoles.Roles.API.Systems;

public static class Common
{
	public static bool IsForceInfoBlockRole(SingleRoleBase role)
		=> role.IsImpostor() || isForceInfoBlockRoleIds(role.Id);

	public static bool IsForceInfoBlockRoleWithoutAssassin(SingleRoleBase role)
		=>
		(role.IsImpostor() && role.Id != ExtremeRoleId.Assassin) ||
		isForceInfoBlockRoleIds(role.Id);

	private static bool isForceInfoBlockRoleIds(ExtremeRoleId checkId)
		=> checkId is ExtremeRoleId.Madmate or ExtremeRoleId.Doll;
}
