using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using VRTK;
using UnityModManagerNet;
using Harmony12;
using System.Reflection;

namespace VRCameraZoomMod
{
    public class Main
    {
		private static UnityModManager.ModEntry thisModEntry;
		private static bool isModBroken = false;

		static void Load(UnityModManager.ModEntry modEntry)
        {
			var harmony = HarmonyInstance.Create(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			modEntry.OnToggle = OnToggle;
			thisModEntry = modEntry;
        }

		static bool OnToggle(UnityModManager.ModEntry modEntry, bool isTogglingOn)
        {
			if (isModBroken)
            {
				return !isTogglingOn;
            }
			return true;
        }

		static void OnCriticalFailure()
		{
			isModBroken = true;
			thisModEntry.Active = false;
        }

		[HarmonyPatch(typeof(CameraZoom), "ZoomMechanic")]
		class CameraZoom_ZoomMechanic_Patch
        {
			static void Postfix(ref float ___currentZoomVelocity, float ___zoomInTime, float ___zoomOutTime, float ___normalFOV, float ___zoomedFOV)
            {
				try
				{
					if (thisModEntry.Active && VRManager.IsVREnabled())
					{
						VRTK_ControllerEvents secondaryEvents = VRTK_DeviceFinder.GetControllerRightHand(false).GetComponent<VRTK_ControllerEvents>();
						Vector2 axis = secondaryEvents.GetAxis(
							VRTK_DeviceFinder.GetHeadsetType() == SDK_BaseHeadset.HeadsetType.WindowsMixedReality
							? VRTK_ControllerEvents.Vector2AxisAlias.TouchpadTwo
							: VRTK_ControllerEvents.Vector2AxisAlias.Touchpad
						);
						float zoomedFactor = ___normalFOV / ___zoomedFOV;
						if (axis.y < 0.625f && XRDevice.fovZoomFactor > 1f)
						{
							XRDevice.fovZoomFactor = Mathf.SmoothDamp(XRDevice.fovZoomFactor, 1.0f, ref ___currentZoomVelocity, ___zoomOutTime);
						}
						if (axis.y > 0.625f && XRDevice.fovZoomFactor < zoomedFactor)
						{
							XRDevice.fovZoomFactor = Mathf.SmoothDamp(XRDevice.fovZoomFactor, zoomedFactor, ref ___currentZoomVelocity, ___zoomInTime);
						}
					}
				}
				catch (Exception e)
                {
					Debug.LogError(string.Format("Exception thrown during CameraZoom.ZoomMechanic postfix patch:\n{0}", e.Message));
					OnCriticalFailure();
				}
			}
        }

		[HarmonyPatch(typeof(PlayerManager), "SetPlayer")]
		class PlayerManager_SetPlayer_Patch
        {
			static void Postfix(Transform player, Camera camera)
            {
                try
                {
					// don't skip even if the mod is inactive; this setup only runs once!
					if (VRManager.IsVREnabled() && player != null && camera != null)
                    {
						player.gameObject.AddComponent<CameraZoom>().cam = camera;
					}
                }
				catch (Exception e)
                {
					Debug.LogError(string.Format("Exception thrown during PlayerManager.SetPlayer postfix patch:\n{0}", e.Message));
					OnCriticalFailure();
				}
            }
        }
    }
}
