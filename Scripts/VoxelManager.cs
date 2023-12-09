using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	public class VoxelManager : ScriptableObject, ISpriteSheetProvider
    {
		public const string RESOURCES_FOLDER = "voxul";

		public static VoxelManager Instance => m_instance.Value;
		private static LazyReference<VoxelManager> m_instance = new LazyReference<VoxelManager>(BuildVoxelManager);

		public static string GetVoxelManagerPath() => $"Assets/Resources/{RESOURCES_FOLDER}/{nameof(VoxelManager)}.asset";

		static VoxelManager BuildVoxelManager()
		{
			var path = $"{RESOURCES_FOLDER}/{nameof(VoxelManager)}";
			var vm = Resources.Load<VoxelManager>(path);
#if UNITY_EDITOR
			if (!vm)
			{
				var rscPath = $"{Application.dataPath}/Resources/{RESOURCES_FOLDER}";
				if (!Directory.Exists(rscPath))
				{
					Directory.CreateDirectory(rscPath);
					//UnityEditor.AssetDatabase.Refresh();
				}
				UnityEditor.AssetDatabase.CreateAsset(CreateInstance<VoxelManager>(), GetVoxelManagerPath());
				//UnityEditor.AssetDatabase.Refresh();
				vm = Resources.Load<VoxelManager>(path);
			}
#endif
			return vm;
		}


		public voxulLogger.ELogLevel LogLevel;
		public EThreadingMode DefaultThreadingMode = EThreadingMode.Task;
		public float DefaultMaxCoroutineUpdateTime = 1 / 60f;
		public SpriteSheet DefaultSpriteSheet;
		public Material DefaultMaterial;
		public Material DefaultMaterialTransparent;

		[Range(2, 10)]
		public int LayerRatio = 3;

		public VoxelMeshOptimiserList DefaultOptimisers = new VoxelMeshOptimiserList
		{
			Data = new List<VoxelOptimiserBase>
			{
				new InternalFaceOptimiser(),
				new FaceMerger()
			}
		};

		public void OnValidate()
		{
            if (!GraphicsSettings.currentRenderPipeline)
            {
				Debug.LogError("Scriptable Render Pipeline not found - Voxul does not work with in-built RP. Please install URP/HDRP from Package Manager.");
				return;
            }
			voxulLogger.InvalidateLogLevel();
			if (!DefaultMaterial || DefaultMaterial == null)
			{
				var shader = Shader.Find("voxul/DefaultVoxel");
                if (!shader)
                {
					Debug.LogError("Couldn't find shader voxul/DefaultVoxul. Do you have ShaderGraph installed?");
					return;
                }
				DefaultMaterial = new Material(shader);
#if UNITY_EDITOR
				UnityEditor.AssetDatabase.AddObjectToAsset(DefaultMaterial, GetVoxelManagerPath());
				Debug.Log(UnityEditor.AssetDatabase.GetAssetPath(DefaultMaterial));
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
			if (!DefaultMaterialTransparent || DefaultMaterialTransparent == null)
			{
				DefaultMaterialTransparent = new Material(Shader.Find("voxul/DefaultVoxelTransparent"));
#if UNITY_EDITOR
				UnityEditor.AssetDatabase.AddObjectToAsset(DefaultMaterialTransparent, GetVoxelManagerPath());
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
		}

        public SpriteSheet GetSpriteSheet()
        {
			if (!DefaultSpriteSheet)
			{
				DefaultSpriteSheet = ScriptableObject.CreateInstance<SpriteSheet>();
				DefaultSpriteSheet.name = "Default Spritesheet";
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.AddObjectToAsset(DefaultSpriteSheet, GetVoxelManagerPath());
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
			return DefaultSpriteSheet;
        }
    }
}