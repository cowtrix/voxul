using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul
{
	[ExecuteAlways]
	public class VoxelManager : Singleton<VoxelManager>
	{
		public const string RESOURCES_FOLDERS = "voxul";
		public static Material DefaultMaterial => Resources.Load<Material>($"{RESOURCES_FOLDERS}/{DefaultMaterial}");
		public static Material DefaultMaterialTransparent => Resources.Load<Material>($"{RESOURCES_FOLDERS}/{DefaultMaterialTransparent}");
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