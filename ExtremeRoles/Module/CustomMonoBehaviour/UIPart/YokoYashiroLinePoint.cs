﻿using System;

using TMPro;
using UnityEngine;

namespace ExtremeRoles.Module.CustomMonoBehaviour.UIPart;

public sealed class YokoYashiroLinePoint(IntPtr ptr) : MonoBehaviour(ptr)
{
	public TextMeshPro Text { get; private set; }

	public void Awake()
	{
		this.Text = base.GetComponentInChildren<TextMeshPro>();
	}
}
