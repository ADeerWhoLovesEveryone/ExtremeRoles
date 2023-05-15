﻿using System.Collections.Generic;

using ExtremeRoles.GhostRoles;
using ExtremeRoles.Roles;

namespace ExtremeRoles.Module.RoleAssign.Model;

public sealed class RoleFilterSetModel
{
    public int AssignNum { get; set; } = 1;

    public Dictionary<int, ExtremeRoleId> FilterNormalId { get; set; }
    public Dictionary<int, CombinationRoleType> FilterCombinationId { get; set; }
    public Dictionary<int, ExtremeGhostRoleId> FilterGhostRole { get; set; }
}
