using System;
using UnityModManagerNet;

namespace VRZoom;

public class Settings : UnityModManager.ModSettings, IDrawable
{
	public static Action? OnSettingsChanged;

	[Draw("Zoom Factor", DrawType.Slider, Min = 1.25f, Max = 10f)]
	public float zoomFactor = 2f;

	[Draw("Zoom in duration")]
	public float zoomInDuration = 0.1f;

	[Draw("Zoom out duration")]
	public float zoomOutDuration = 0.2f;

	[Draw("Distance from HMD to viewport (m)", Min = 0f)]
	public float viewportMeters = 1f;

	[Draw("Show the zoom camera on the PC monitor? (Enabling this may impact performance.)")]
	public bool showOnPC = false;

	public void OnChange() { OnSettingsChanged?.Invoke(); }

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}
