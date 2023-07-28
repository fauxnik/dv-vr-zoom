using HarmonyLib;

namespace VRZoom;

[HarmonyPatch(typeof(LocomotionInputVr), "UpdateFrame")]
static class BindInputVr
{
	private static bool zoomRequested = false;
	private static Zoomer? zoomer;

	static void Postfix(LocomotionInputVr __instance)
	{
		bool wasZoomRequested = zoomRequested;
		zoomRequested = __instance.SwimRequested;
		if (wasZoomRequested != zoomRequested)
		{
			Main.LogDebug?.Invoke("Change in zoom request detected!");
			zoomer ??= Zoomer.CreateComponent(PlayerManager.PlayerCamera.gameObject, PlayerManager.PlayerCamera.fieldOfView);
			zoomer.Zoom(zoomRequested);
		}
	}
}
