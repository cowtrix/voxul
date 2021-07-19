using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	[CustomEditor(typeof(VoxelSprite), false)]
	internal class VoxelSpriteEditor : VoxelObjectEditorBase<VoxelSprite>
	{
		[MenuItem("GameObject/3D Object/voxul/Voxel Sprite")]
		public static void CreateNewSprite()
		{
			var go = new GameObject("New Voxel Sprite");
			var r = go.AddComponent<VoxelSprite>();
			EditorGUIUtility.PingObject(go);
		}

		protected override void DrawSpecificGUI()
		{

		}
	}
}