using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using static UnityModManagerNet.UnityModManager.ModSettings;

namespace VRZoom;

public static class Main
{
#if RELEASE
#pragma warning disable CS0649 // It's expected that LogDebug will never be assigned in Release builds
#endif
	public static Action<string>? LogDebug;
#pragma warning restore CS0649
	public static Settings settings = new Settings();
	public static ModEntry? modEntry { get; private set; }

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(ModEntry modEntry)
	{
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
			Load<Settings>(modEntry);
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
