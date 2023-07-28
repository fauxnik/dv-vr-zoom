using UnityModManagerNet;

namespace VRZoom;

// TODO: is there an easier way to proxy the properties and methods?
public class Settings
{
	public static Settings Instance { get { return instance ??= new Settings(); } }
	private static Settings? instance;

	private Settings() { internalSettings = new VRZoomSettings(); }
	private readonly VRZoomSettings internalSettings;

	public float ZoomedFOV
	{
		get { return internalSettings.zoomedFOV; }
		set { internalSettings.zoomedFOV = value;}
	}

	public float ZoomInDuration
	{
		get { return internalSettings.zoomInDuration; }
		set { internalSettings.zoomInDuration = value;}
	}

	public float ZoomOutDuration
	{
		get { return internalSettings.zoomOutDuration; }
		set { internalSettings.zoomOutDuration = value;}
	}

	public void Draw(UnityModManager.ModEntry modEntry) { internalSettings.Draw(modEntry); }
	public void Save(UnityModManager.ModEntry modEntry) { internalSettings.Save(modEntry); }

	public class VRZoomSettings : UnityModManager.ModSettings, IDrawable
	{

		[Draw("Zoomed FOV", DrawType.Slider, Min = 20, Max = 60)]
		public float zoomedFOV = 40f;

		[Draw("Zoom in duration")]
		public float zoomInDuration = 0.1f;

		[Draw("Zoom out duration")]
		public float zoomOutDuration = 0.2f;

		public void OnChange() {}

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}
	}
}
