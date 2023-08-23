using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using static UnityModManagerNet.UnityModManager.ModSettings;

namespace VRZoom;

public static class Main
{
	public static Action<string> Log = (_) => {};
	public static Action<string> LogWarning = (message) => { Log($"[Warning] {message}"); };
	public static Action<string> LogError = (message) => { Log($"[Error] {message}"); };
	public static Action<string> LogDebug = (_) => {};

	public static Settings settings = new Settings();
	public static ModEntry? modEntry { get; private set; }

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(ModEntry modEntry)
	{
		Log = modEntry.Logger.Log;
#if DEBUG
		LogDebug = modEntry.Logger.Log;
#endif
		Harmony? harmony = null;
		Main.modEntry = modEntry;

		ModEntry? cameraManagerEntry = FindMod("CameraManager");
		if (cameraManagerEntry == null || cameraManagerEntry.Active == false)
		{
			modEntry.Logger.LogException(new Exception("VR Zoom requires Camera Manager, but it either isn't installed or isn't active."));
			return false;
		}
		ModEntry? thirdEyeEntry = FindMod("ThirdEye");
		if (thirdEyeEntry == null || thirdEyeEntry.Active == false)
		{
			modEntry.Logger.LogException(new Exception("VR Zoom requires Third Eye, but it either isn't installed or isn't active."));
			return false;
		}

		try
		{
			settings = Load<Settings>(modEntry);
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load settings for {modEntry.Info.DisplayName}:", ex);
		}
		modEntry.OnGUI = settings.Draw;
		modEntry.OnSaveGUI = settings.Save;

		try
		{
			LogDebug?.Invoke($"Patching assembly…");
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll(modEntry.Info.Id);
			return false;
		}

		return true;
	}
}
