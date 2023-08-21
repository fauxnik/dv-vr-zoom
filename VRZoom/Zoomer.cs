using CameraManager;
using static CameraManager.CameraType;
using System;
using System.Collections;
using System.IO;
using ThirdEye;
using UnityEngine;
using static UnityEngine.Object;
using UnityEngine.Rendering;

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
		zoomer.Setup();
		return zoomer;
	}

	private const int viewportLayer = 31;
	private IEnumerator? zoomCoroutine;
	// private Camera? overlayCamera;
	private GameObject? quad;
	private MeshRenderer? meshRenderer;
	private float currentZoomVelocity = 0f;
	private bool isSetup = false;

	private bool Setup()
	{
		if (Main.modEntry == null) { throw new InvalidOperationException("Unexpected null mod entry while setting up Zoomer component."); }

		if (isSetup)
		{
			Main.modEntry.Logger.Log("[Warning] Trying to set up a Zoomer component that's already been set up.");
			return false;
		}

		if (ThirdEye.Main.camera == null)
		{
			Main.modEntry.Logger.Log("[Warning] Third Eye camera unavailable during Zoomer setup.");
			return false;
		}

		AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(new string [] { Main.modEntry.Path, "vr-zoom" }));
		Shader viewportShader = bundle.LoadAsset<Shader>("Assets/Shaders/TextureOverlay.shader");
		bundle.Unload(false);
		if (viewportShader == null)
		{
			Main.modEntry.Logger.Log("[Error] The viewport shader couldn't be loaded.");
			return false;
		}

		AmalgamCamera camera = ThirdEye.Main.camera;

		quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		DestroyImmediate(quad.GetComponent<Collider>());
		quad.transform.SetParent(camera.gameObject.transform, false);
		quad.layer = viewportLayer;
		// TODO: can we raise this above the World camera so it always appears in front of objects in the scene?
		camera.cullingMask &= ~(1 << quad.layer);
		CameraAPI.GetCamera(World).cullingMask |= 1 << quad.layer;
		// // Camera.onPostRender += OnPostRender;
		// overlayCamera = new GameObject().AddComponent<Camera>();
		// // overlayCamera.gameObject.transform.SetParent(CameraAPI.GetCamera(World).gameObject.transform, false);
		// overlayCamera.cullingMask = 1 << quad.layer;
		// overlayCamera.clearFlags = CameraClearFlags.Depth;
		// // overlayCamera.clearFlags = CameraClearFlags.Color;
		// // overlayCamera.backgroundColor = Color.clear;
		// // overlayCamera.depth = CameraAPI.GetCamera(Effects).depth + 1;
		// // CameraAPI.GetCamera(UI).depth++;
		// // CameraAPI.GetCamera(Effects).depth++;
		// // overlayCamera.enabled = false;

		meshRenderer = quad.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.shader = viewportShader;
		meshRenderer.sharedMaterial.mainTexture = ThirdEye.Main.renderTexture;
		meshRenderer.sharedMaterial.renderQueue = (int)RenderQueue.Overlay;
		// meshRenderer.sharedMaterial.shader = Shader.Find("FX/Flare") ?? meshRenderer.sharedMaterial.shader;
		// meshRenderer.sharedMaterial.shader = Shader.Find("Unlit/Texture") ?? meshRenderer.sharedMaterial.shader;
		// meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		// meshRenderer.receiveShadows = false;
		meshRenderer.enabled = false;

		Settings.OnSettingsChanged += OnSettingsChanged;
		OnSettingsChanged();

		return isSetup = true;
	}

	private void OnSettingsChanged()
	{
		Main.LogDebug?.Invoke($"Settings changed {{\n\tzoomFactor: {Main.settings.zoomFactor}\n\tzoomInDuration: {Main.settings.zoomInDuration}\n\tzoomOutDuration: {Main.settings.zoomOutDuration}\n\tviewportMeters: {Main.settings.viewportMeters}\n\tviewportHeight: {Main.settings.viewportHeight}\n}}");

		if (quad != null)
		{
			quad.transform.localPosition = new Vector3(0, 0, Main.settings.viewportMeters);
			quad.transform.localScale = new Vector3(Main.settings.viewportHeight * Screen.width / Screen.height, Main.settings.viewportHeight, quad.transform.localScale.z);
		}
	}

	// private void OnPostRender(Camera camera)
	// {
	// 	if (!(meshRenderer?.enabled ?? false) || camera != CameraAPI.GetCamera(World)) { return; }

	// 	// backup
	// 	// int cullingMask = camera.cullingMask;
	// 	// CameraClearFlags clearFlags = camera.clearFlags;

	// 	// modify & render
	// 	// camera.cullingMask = 1 << quad.layer;
	// 	// camera.clearFlags = CameraClearFlags.Nothing;
	// 	overlayCamera?.Render();

	// 	// restore
	// 	// camera.cullingMask = cullingMask;
	// 	// camera.clearFlags = clearFlags;
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
		if (!isSetup && !Setup()) { yield break; }

		AmalgamCamera? camera = ThirdEye.Main.camera;
		if (camera == null || meshRenderer == null || Main.modEntry == null) { yield break; }

		int iteration = 0;
		float targetZoomFactor = zoomIn ? Main.settings.zoomFactor : 1f;
		float zoomDuration = zoomIn ? Main.settings.zoomInDuration : Main.settings.zoomOutDuration;

		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");

		yield return null;

		ThirdEye.Main.RequestRender(Main.modEntry.Info.Id, true);
		meshRenderer.enabled = zoomIn;

		yield return null;

		while (!Mathf.Approximately(camera.zoomFactor, targetZoomFactor))
		{
			camera.zoomFactor = Mathf.SmoothDamp(camera.zoomFactor, targetZoomFactor, ref currentZoomVelocity, zoomDuration);
			Main.LogDebug?.Invoke($"Zoom coroutine[{++iteration:D6}]: {camera.zoomFactor} -> {targetZoomFactor}");
			yield return null;
		}

		camera.zoomFactor = targetZoomFactor;
		if (!zoomIn) { ThirdEye.Main.RequestRender(Main.modEntry.Info.Id, false); }

		Main.LogDebug?.Invoke($"Zoom coroutine [{iteration:D6}]: complete");

		zoomCoroutine = null;
	}
}
