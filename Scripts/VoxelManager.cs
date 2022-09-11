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
	public class VoxelManager : ScriptableObject
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
					UnityEditor.AssetDatabase.Refresh();
				}
				UnityEditor.AssetDatabase.CreateAsset(CreateInstance<VoxelManager>(), GetVoxelManagerPath());
				UnityEditor.AssetDatabase.Refresh();
				vm = Resources.Load<VoxelManager>(path);
			}
#endif
			return vm;
		}


		public voxulLogger.ELogLevel LogLevel;
		public EThreadingMode DefaultThreadingMode = EThreadingMode.Task;
		public float DefaultMaxCoroutineUpdateTime = 1 / 60f;

		public Material DefaultMaterial;
		public Material DefaultMaterialTransparent;
		public Texture2DArray BaseTextureArray;

		[Range(2, 10)]
		public int LayerRatio = 3;
		[Range(8, 1024)]
		public int SpriteResolution = 32;

		public List<Texture2D> Sprites = new List<Texture2D>();
		[HideInInspector]
		[SerializeField]
		private List<Texture2D> m_spriteCache = new List<Texture2D>();

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
			RegenerateSpritesheet();
		}

		[ContextMenu("Regenerate Spritesheet")]
		public void RegenerateSpritesheet()
		{
			if (Sprites.SequenceEqual(m_spriteCache))
			{
				return;
			}
			m_spriteCache.Clear();
			m_spriteCache.AddRange(Sprites);
#if UNITY_EDITOR
			var texArray = BaseTextureArray;
			var newArray = GenerateArray(Sprites, TextureFormat.ARGB32, SpriteResolution);
			if (newArray != null)
			{
				newArray.filterMode = FilterMode.Point;
				newArray.wrapMode = TextureWrapMode.Repeat;
				var currentPath = texArray ? UnityEditor.AssetDatabase.GetAssetPath(texArray) : $"Assets/Resources/{RESOURCES_FOLDER}/spritesheet.asset";
				var tmpPath = "Assets/tmp.asset";

				try
				{
					UnityEditor.AssetDatabase.CreateAsset(newArray, tmpPath);
					UnityEditor.AssetDatabase.Refresh();
					File.WriteAllBytes(currentPath, File.ReadAllBytes(tmpPath));
					UnityEditor.AssetDatabase.DeleteAsset(tmpPath);
					UnityEditor.AssetDatabase.ImportAsset(currentPath);
				}
				catch { }

				BaseTextureArray = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2DArray>(currentPath);
				DefaultMaterial.SetTexture("AlbedoSpritesheet", BaseTextureArray);
				UnityEditor.EditorUtility.SetDirty(DefaultMaterial);
				DefaultMaterialTransparent.SetTexture("AlbedoSpritesheet", BaseTextureArray);
				UnityEditor.EditorUtility.SetDirty(DefaultMaterialTransparent);
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif
		}

		static Texture2D ResizeTexture(Texture2D texture2D, int targetX, int targetY)
		{
			RenderTexture rt = new RenderTexture(targetX, targetY, 24);
			RenderTexture.active = rt;
			Graphics.Blit(texture2D, rt);
			Texture2D result = new Texture2D(targetX, targetY);
			result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
			result.Apply();
			return result;
		}

		static Texture2DArray GenerateArray(IList<Texture2D> textures, TextureFormat format, int size)
		{
			if (!textures.Any())
			{
				return null;
			}
			var texture2DArray = new Texture2DArray(size, size, textures.Count, format, false);
			for (int i = 0; i < textures.Count; i++)
			{
				var tex = textures[i];
				if (tex.height != size || tex.width != size)
				{
					tex = ResizeTexture(tex, size, size);
				}
				texture2DArray.SetPixels(tex.GetPixels(), i);
			}

			texture2DArray.Apply();
			return texture2DArray;
		}
	}
}