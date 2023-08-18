using System.Collections;
using CameraManager;
using UnityEngine;

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
		zoomer.Setup();
		return zoomer;
	}

	private const int viewportLayer = 31;
	private IEnumerator? zoomCoroutine;
	private AmalgamCamera? camera;
	private RenderTexture? renderTexture;
	private GameObject? quad;
	private MeshRenderer? meshRenderer;
	private float currentZoomVelocity = 0f;

	private void Setup()
	{
		if (camera == null)
		{
			// TODO: log error
			return;
		}

		int resWidth = Screen.width;
		int resHeight = Screen.height;
		renderTexture = new RenderTexture(resWidth, resHeight, 24);
		camera.targetTexture = renderTexture;

		quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		DestroyImmediate(quad.GetComponent<Collider>());
		quad.transform.SetParent(camera.gameObject.transform, false);
		quad.layer = viewportLayer;
		camera.cullingMask &= ~(1 << quad.layer);

		meshRenderer = quad.GetComponent<MeshRenderer>();
		meshRenderer.enabled = false;
		meshRenderer.sharedMaterial.mainTexture = renderTexture;

		Settings.OnSettingsChanged += OnSettingsChanged;
		OnSettingsChanged();
	}

	void OnSettingsChanged()
	{
		if (camera != null)
		{
			if (zoomCoroutine == null) { camera.enabled = Main.settings.showOnPC; }
		}
		if (quad != null)
		{
			quad.transform.localPosition = new Vector3(0, 0, Main.settings.viewportMeters);
		}
	}

	void OnPreRender()
	{
		if (camera == null || renderTexture == null || !(Main.modEntry?.Active ?? false)) { return; }
	}

	void OnPostRender()
	{
		if (camera == null || renderTexture == null || !(Main.modEntry?.Active ?? false)) { return; }

		if (Main.settings.showOnPC)
		{
			Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
		}
	}

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
		if (camera == null || meshRenderer == null) { yield break; }
		int iteration = 0;
		float targetZoomFactor = zoomIn ? Main.settings.zoomFactor : 1f;
		float zoomDuration = zoomIn ? Main.settings.zoomInDuration : Main.settings.zoomOutDuration;
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");
		yield return new WaitForEndOfFrame();
		camera.enabled = true;
		meshRenderer.enabled = true;
		while (!Mathf.Approximately(camera.zoomFactor, targetZoomFactor))
		{
			camera.zoomFactor = Mathf.SmoothDamp(camera.zoomFactor, targetZoomFactor, ref currentZoomVelocity, zoomDuration);
			Main.LogDebug?.Invoke($"Zoom coroutine[{++iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");
			yield return new WaitForEndOfFrame();
		}
		camera.zoomFactor = targetZoomFactor;
		if (!Main.settings.showOnPC) { camera.enabled = false; }
		meshRenderer.enabled = false;
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: complete");
		zoomCoroutine = null;
	}
}
