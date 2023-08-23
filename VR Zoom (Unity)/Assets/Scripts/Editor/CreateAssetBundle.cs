using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundle
{
	[MenuItem("Assets/Build Asset Bundles")]
    private static void BuildAllAssetBundles()
	{
		string bundleOutputPath = Path.Combine(new string[] { Application.dataPath, "..", "..", "AssetBundles" });

		try
		{
			BuildPipeline.BuildAssetBundles(bundleOutputPath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
			string assetBundlesFilePath = Path.Combine(new string[] { bundleOutputPath, "AssetBundles" });
			if (File.Exists(assetBundlesFilePath)) { File.Delete(assetBundlesFilePath); }
			if (File.Exists(assetBundlesFilePath + ".manifest")) { File.Delete(assetBundlesFilePath + ".manifest"); }
			Debug.Log("Asset bundles built successfully.");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
}
