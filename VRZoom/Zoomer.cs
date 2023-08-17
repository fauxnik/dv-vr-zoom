using System.Collections;
using CameraManager;
using UnityEngine;
using UnityEngine.XR;

namespace VRZoom;

class Zoomer : MonoBehaviour
{
	/// <summary>
	/// Adds a <c>Zoomer</c> component to the provided game object. The game object must already have an <c>AmalgamCamera</c> component at the time this method is called.
	/// </summary>
	/// <param name="where">A game object with an <c>AmalgamCamera</c> component.</param>
	/// <returns>The <c>Zoomer</c> component instance.</returns>
	public static Zoomer CreateComponent(GameObject where)
	{
		Zoomer zoomer = where.AddComponent<Zoomer>();
		zoomer.camera = where.GetComponent<AmalgamCamera>();
		return zoomer;
	}

	private IEnumerator? zoomCoroutine;
	private AmalgamCamera? camera;
	private float currentZoomVelocity = 0f;

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
		if (camera == null) { yield break; }
		int iteration = 0;
		float targetZoomFactor = zoomIn ? Main.settings.zoomFactor : 1f;
		float zoomDuration = zoomIn ? Main.settings.zoomInDuration : Main.settings.zoomOutDuration;
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");
		yield return new WaitForEndOfFrame();
		while (!Mathf.Approximately(camera.zoomFactor, targetZoomFactor))
		{
			camera.zoomFactor = Mathf.SmoothDamp(camera.zoomFactor, targetZoomFactor, ref currentZoomVelocity, zoomDuration);
			Main.LogDebug?.Invoke($"Zoom coroutine[{++iteration:D6}]: {XRDevice.fovZoomFactor} -> {targetZoomFactor}");
			yield return new WaitForEndOfFrame();
		}
		camera.zoomFactor = targetZoomFactor;
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: complete");
		zoomCoroutine = null;
	}
}
