using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul
{
	public class LazyReference<T> where T : UnityEngine.Object
	{
		public T Value 
		{ 
			get
			{
				if(!m_initialized)
				{
					m_initialized = true;
					m_value = m_getter.Invoke();
				}
				return m_value;
			}
		}
		private readonly Func<T> m_getter;
		private T m_value;
		private bool m_initialized = false;
		public LazyReference(Func<T> getter)
		{
			m_getter = getter;
		}
	}

	[ExecuteAlways]
	public class VoxelManager : Singleton<VoxelManager>
	{
		public const string RESOURCES_FOLDERS = "voxul";
		public static LazyReference<Material> DefaultMaterial = new LazyReference<Material>(
			() => Resources.Load<Material>($"{RESOURCES_FOLDERS}/{DefaultMaterial}"));
		public static LazyReference<Material> DefaultMaterialTransparent => new LazyReference<Material>(
			() => Resources.Load<Material>($"{RESOURCES_FOLDERS}/{DefaultMaterialTransparent}"));
		public Texture2DArray BaseTextureArray;
		public List<Texture2D> Sprites;
		public Mesh CubeMesh;
		public Material LODMaterial;

		[ContextMenu("Regenerate Spritesheet")]
		public void RegenerateSpritesheet()
		{/*
#if UNITY_EDITOR
			var texArray = BaseTextureArray;
			var newArray = Texture2DArrayGenerator.Generate(Sprites, TextureFormat.ARGB32);
			newArray.filterMode = FilterMode.Point;
			newArray.wrapMode = TextureWrapMode.Repeat;
			var currentPath = AssetDatabase.GetAssetPath(texArray);
			var tmpPath = AssetCreationHelper.CreateAssetInCurrentDirectory(newArray, "tmp.asset");
			File.WriteAllBytes(currentPath, File.ReadAllBytes(tmpPath));
			AssetDatabase.DeleteAsset(tmpPath);
			AssetDatabase.ImportAsset(currentPath);
			DefaultMaterial.SetTexture("AlbedoSpritesheet", texArray);
			DefaultMaterialTransparent.SetTexture("AlbedoSpritesheet", texArray);
#endif*/
		}
	}
}