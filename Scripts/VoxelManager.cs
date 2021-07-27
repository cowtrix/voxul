using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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
			return vm;
		}


		public voxulLogger.ELogLevel LogLevel;
		public EThreadingMode DefaultThreadingMode = EThreadingMode.Task;
		public float DefaultMaxCoroutineUpdateTime = 1 / 60f;

		[HideInInspector]
		public Material DefaultMaterial;
		[HideInInspector]
		public Material DefaultMaterialTransparent;
		[HideInInspector]
		public Texture2DArray BaseTextureArray;

		[Range(2, 10)]
		public int LayerRatio = 3;
		[Range(8, 1024)]
		public int SpriteResolution = 32;

		public List<Texture2D> Sprites = new List<Texture2D>();
		[HideInInspector]
		[SerializeField]
		private List<Texture2D> m_spriteCache = new List<Texture2D>();

		public List<VoxelOptimiserBase> DefaultOptimisers = new List<VoxelOptimiserBase>();

		public void OnValidate()
		{
			voxulLogger.InvalidateLogLevel();
			if (DefaultOptimisers == null || DefaultOptimisers.Count == 0)
			{
				DefaultOptimisers = new List<VoxelOptimiserBase>()
				{
					Resources.Load<VoxelOptimiserBase>($"{RESOURCES_FOLDER}/{nameof(InternalFaceOptimiser)}"),
				};
			}
			if (!DefaultMaterial || DefaultMaterial == null)
			{
				DefaultMaterial = new Material(Shader.Find("voxul/DefaultVoxel"));
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