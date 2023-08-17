using System;
using UnityModManagerNet;

namespace VRZoom;

public class Settings : UnityModManager.ModSettings, IDrawable
{
	public static Action? OnSettingsChagned;

	[Draw("Zoom Factor", DrawType.Slider, Min = 1.25f, Max = 10f)]
	public float zoomFactor = 2f;

	[Draw("Zoom in duration")]
	public float zoomInDuration = 0.1f;

	[Draw("Zoom out duration")]
	public float zoomOutDuration = 0.2f;

	public void OnChange() { OnSettingsChagned?.Invoke(); }

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}
