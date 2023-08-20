using CameraManager;
using static CameraManager.CameraType;
using System.Collections;
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
		// camera.targetTexture = renderTexture;
		camera.enabled = false; //Main.settings.showOnPC;

		quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		DestroyImmediate(quad.GetComponent<Collider>());
		quad.transform.SetParent(camera.gameObject.transform, false);
		quad.transform.localScale = new Vector3(quad.transform.localScale.y * resWidth / resHeight, quad.transform.localScale.y, quad.transform.localScale.z);
		quad.layer = viewportLayer;
		// TODO: can we raise this above the World camera so it always appears in front of objects in the scene?
		camera.cullingMask &= ~(1 << quad.layer);
		CameraAPI.GetCamera(World).cullingMask |= 1 << quad.layer;

		meshRenderer = quad.GetComponent<MeshRenderer>();
		meshRenderer.enabled = false;
		meshRenderer.sharedMaterial.mainTexture = renderTexture;

		Settings.OnSettingsChanged += OnSettingsChanged;
		OnSettingsChanged();

		// Camera.onPreRender += OnPreRender;
		// Camera.onPostRender += OnPostRender;

		StartCoroutine(UpdateScreen());
	}

	private void OnSettingsChanged()
	{
		Main.LogDebug?.Invoke($"Settings changed {{\n\tzoomFactor: {Main.settings.zoomFactor}\n\tzoomInDuration: {Main.settings.zoomInDuration}\n\tzoomOutDuration: {Main.settings.zoomOutDuration}\n\tviewportMeters: {Main.settings.viewportMeters}\n\tshowOnPC: {Main.settings.showOnPC}\n}}");

		if (camera != null)
		{
			if (zoomCoroutine == null) { camera.enabled = Main.settings.showOnPC; }
		}
		if (quad != null)
		{
			quad.transform.localPosition = new Vector3(0, 0, Main.settings.viewportMeters);
		}
	}

	public void Update()
	{
		Main.LogDebug?.Invoke($"Update {{\n\tcamera: {(camera == null ? "null" : camera)}\n\trenderTexture: {(renderTexture == null ? "null" : renderTexture)}\n\tActive: {Main.modEntry?.Active ?? false}\n\tshowOnPC: {Main.settings.showOnPC}}}");

		if (camera == null || renderTexture == null || meshRenderer == null || !(Main.modEntry?.Active ?? false)) { return; }

		if (meshRenderer.enabled || Main.settings.showOnPC)
		{
			camera.targetTexture = renderTexture;
			camera.Render();
			camera.targetTexture = null;
		}
	}

	IEnumerator UpdateScreen()
	{
		yield return new WaitForEndOfFrame();

		if (Main.settings.showOnPC && renderTexture != null)
		{
			Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
		}
	}

	// public void OnPreRender(Camera renderCamera)
	// {
	// 	if (camera == null || renderTexture == null || !(Main.modEntry?.Active ?? false) || renderCamera != CameraAPI.GetCamera(World)) { return; }
	// }

	// public void OnPostRender(Camera renderCamera)
	// {
	// 	Main.LogDebug?.Invoke($"OnPostRender {{\n\trender camera: {renderCamera}\n\tcamera: {(camera == null ? "null" : camera)}\n\trenderTexture: {(renderTexture == null ? "null" : renderTexture)}\n\tActive: {Main.modEntry?.Active ?? false}\n\tshowOnPC: {Main.settings.showOnPC}}}");

	// 	if (camera == null || renderTexture == null || !(Main.modEntry?.Active ?? false) || renderCamera != CameraAPI.GetCamera(Effects)) { return; }

	// 	if (Main.settings.showOnPC)
	// 	{
	// 		Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
	// 	}
	// }

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
		// camera.enabled = true;
		meshRenderer.enabled = zoomIn;
		yield return new WaitForEndOfFrame();
		while (!Mathf.Approximately(camera.zoomFactor, targetZoomFactor))
		{
			camera.zoomFactor = Mathf.SmoothDamp(camera.zoomFactor, targetZoomFactor, ref currentZoomVelocity, zoomDuration);
			Main.LogDebug?.Invoke($"Zoom coroutine[{++iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");
			yield return new WaitForEndOfFrame();
		}
		camera.zoomFactor = targetZoomFactor;
		// if (!zoomIn && !Main.settings.showOnPC) { camera.enabled = false; }
		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: complete");
		zoomCoroutine = null;
	}
}
