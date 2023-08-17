using CameraManager;
using HarmonyLib;
using UnityEngine;

namespace VRZoom;

[HarmonyPatch(typeof(LocomotionInputVr), "UpdateFrame")]
static class BindInputVr
{
	private static bool zoomRequested = false;

	static void Postfix(LocomotionInputVr __instance)
	{
		bool wasZoomRequested = zoomRequested;
		zoomRequested = __instance.SwimRequested;
		if (wasZoomRequested != zoomRequested)
		{
			Main.LogDebug?.Invoke("Change in zoom request detected!");
			GameObject playerCamera = PlayerManager.PlayerCamera.gameObject;
			Zoomer zoomer = playerCamera.GetComponentInChildren<Zoomer>();
			if (zoomer == null)
			{
				if (playerCamera.GetComponentInChildren<AmalgamCamera>() is AmalgamCamera amalgamCamera)
				{
					zoomer = Zoomer.CreateComponent(amalgamCamera.gameObject);
				}
				else { return; }
			}
			zoomer.Zoom(zoomRequested);
		}
	}
}
