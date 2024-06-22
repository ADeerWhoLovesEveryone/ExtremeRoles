﻿using System.Text.RegularExpressions;

using OptionUnit = ExtremeRoles.Module.CustomOption.OptionUnit;

using ExtremeRoles.Module.NewOption.Interfaces;

#nullable enable

namespace ExtremeRoles.Module.NewOption.Implemented;

public sealed class OptionInfo(
	int id, string name,
	OptionUnit format = OptionUnit.None,
	bool hidden = false) : IOptionInfo
{
	public int Id { get; } = id;
	public string Name { get; } = name;

	public string CodeRemovedName => nameCleaner.Replace(Name, string.Empty);
	public string Format { get; } = format == OptionUnit.None ? string.Empty : format.ToString();

	public bool IsHidden { get; } = hidden;

	public override string ToString()
		=> $"Name:{Name}, Format:{Format} -- ({Id})";

	private static readonly Regex nameCleaner = new Regex(@"(\|)|(<.*?>)|(\\n)", RegexOptions.Compiled);
}
