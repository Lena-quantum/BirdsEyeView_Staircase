using UnityEngine;
using UnityEditor;

namespace Editor
{
	/// <summary>
	/// Creates a black skybox material and assigns it to RenderSettings.
	/// This script runs automatically when Unity loads or when the scene is opened.
	/// </summary>
	[InitializeOnLoad]
	public static class BlackSkyboxCreator
	{
		private const string SKYBOX_MATERIAL_PATH = "Assets/Materials/BlackSkyboxMaterial.mat";
		
		static BlackSkyboxCreator()
		{
			// Delay execution to ensure Unity is fully initialized
			EditorApplication.delayCall += CreateBlackSkybox;
		}
		
		[MenuItem("Tools/Create Black Skybox")]
		private static void CreateBlackSkybox()
		{
			Material skyboxMaterial = CreateOrLoadBlackSkyboxMaterial();
			
			if (skyboxMaterial != null)
			{
				RenderSettings.skybox = skyboxMaterial;
				Debug.Log("Black Skybox Material created and assigned to RenderSettings.");
			}
			else
			{
				Debug.LogError("Failed to create black skybox material.");
			}
		}
		
		private static Material CreateOrLoadBlackSkyboxMaterial()
		{
			// Try to load existing material first
			Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(SKYBOX_MATERIAL_PATH);
			if (existingMaterial != null)
			{
				// Verify it's a skybox material
				if (existingMaterial.shader.name.Contains("Skybox"))
				{
					return existingMaterial;
				}
			}
			
			// Create new skybox material
			Material skyboxMaterial = new Material(Shader.Find("Skybox/6 Sided"));
			
			if (skyboxMaterial == null)
			{
				// Fallback: Try Procedural skybox
				skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
			}
			
			if (skyboxMaterial == null)
			{
				Debug.LogError("Could not find Skybox shader. Make sure you're using a Unity version that supports Skybox shaders.");
				return null;
			}
			
			// Set all sides to black
			Color blackColor = Color.black;
			
			// For 6 Sided skybox, set all textures to black
			if (skyboxMaterial.shader.name == "Skybox/6 Sided")
			{
				// Create a 1x1 black texture
				Texture2D blackTexture = new Texture2D(1, 1);
				blackTexture.SetPixel(0, 0, blackColor);
				blackTexture.Apply();
				
				skyboxMaterial.SetTexture("_FrontTex", blackTexture);
				skyboxMaterial.SetTexture("_BackTex", blackTexture);
				skyboxMaterial.SetTexture("_LeftTex", blackTexture);
				skyboxMaterial.SetTexture("_RightTex", blackTexture);
				skyboxMaterial.SetTexture("_UpTex", blackTexture);
				skyboxMaterial.SetTexture("_DownTex", blackTexture);
			}
			// For Procedural skybox, set the tint color to black
			else if (skyboxMaterial.shader.name == "Skybox/Procedural")
			{
				skyboxMaterial.SetColor("_Tint", blackColor);
				skyboxMaterial.SetFloat("_Exposure", 0f);
				skyboxMaterial.SetFloat("_SunSize", 0f);
			}
			
			skyboxMaterial.name = "BlackSkyboxMaterial";
			
			// Ensure the Materials directory exists
			string materialsDir = "Assets/Materials";
			if (!AssetDatabase.IsValidFolder(materialsDir))
			{
				AssetDatabase.CreateFolder("Assets", "Materials");
			}
			
			// Save the material
			AssetDatabase.CreateAsset(skyboxMaterial, SKYBOX_MATERIAL_PATH);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			return skyboxMaterial;
		}
	}
}

