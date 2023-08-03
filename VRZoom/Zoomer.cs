using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRZoom;

class Zoomer : MonoBehaviour
{
	public static Zoomer CreateComponent(GameObject where, float fieldOfView)
	{
		Zoomer zoomer = where.AddComponent<Zoomer>();
		zoomer.hmdFieldOfView = fieldOfView;
		return zoomer;
	}

	private IEnumerator? zoomCoroutine;
	private float currentZoomVelocity = 0f;
	private float hmdFieldOfView;

	public void Zoom(bool zoomIn)
	{
		if (zoomCoroutine != null)
		{
			Main.LogDebug?.Invoke("Stopping zoom coroutine");
			StopCoroutine(zoomCoroutine);
		}
		Main.LogDebug?.Invoke($"Starting zoom coroutine with zoom={(zoomIn ? "in" : "out")}");
		zoomCoroutine = StartZoomCoroutine(zoomIn);
		StartCoroutine(zoomCoroutine);
	}

	private IEnumerator StartZoomCoroutine(bool zoomIn)
	{
		int iteration = 0;
		float targetZoomFactor = zoomIn ? hmdFieldOfView / Settings.Instance.ZoomedFOV : 1f;
		float zoomDuration = zoomIn ? Settings.Instance.ZoomInDuration : Settings.Instance.ZoomOutDuration;
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: {XRDevice.fovZoomFactor} -> {targetZoomFactor}");
		while (!Mathf.Approximately(XRDevice.fovZoomFactor, targetZoomFactor))
		{
			XRDevice.fovZoomFactor = Mathf.SmoothDamp(XRDevice.fovZoomFactor, targetZoomFactor, ref currentZoomVelocity, zoomDuration);
			Main.LogDebug?.Invoke($"Zoom coroutine[{++iteration:D6}]: {XRDevice.fovZoomFactor} -> {targetZoomFactor}");
			yield return null;
		}
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: complete");
		zoomCoroutine = null;
	}
}
