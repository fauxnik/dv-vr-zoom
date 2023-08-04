using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;

namespace VRZoom;

public static class Main
{
#if RELEASE
#pragma warning disable CS0649 // It's expected that LogDebug will never be assigned in Release builds
#endif
	public static Action<string>? LogDebug;
#pragma warning restore CS0649

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
#if DEBUG
		LogDebug = modEntry.Logger.Log;
#endif
		Harmony? harmony = null;

		try
		{
			LogDebug?.Invoke($"Patching assembly…");
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			modEntry.OnGUI = Settings.Instance.Draw;
			modEntry.OnSaveGUI = Settings.Instance.Save;
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
