using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public class VoxelObjectEditorBase : Editor
	{
		[MenuItem("GameObject/3D Object/Voxul Object")]
		public static void CreateNew()
		{
			var go = new GameObject("New Voxel Object");
			var r = go.AddComponent<VoxelRenderer>();
			EditorGUIUtility.PingObject(go);
		}

		[MenuItem("GameObject/3D Object/Voxul Text")]
		public static void CreateNewText()
		{
			var go = new GameObject("New Voxel Text");
			var r = go.AddComponent<VoxelText>();
			EditorGUIUtility.PingObject(go);
		}

		[MenuItem("GameObject/3D Object/Voxul Sprite")]
		public static void CreateNewSprite()
		{
			var go = new GameObject("New Voxel Sprite");
			var r = go.AddComponent<VoxelSprite>();
			EditorGUIUtility.PingObject(go);
		}

		public VoxelRenderer Renderer => target as VoxelRenderer;

		public override void OnInspectorGUI()
		{

			base.OnInspectorGUI();
		}
	}
}