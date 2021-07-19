using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	[CustomEditor(typeof(VoxelText), false)]
	internal class VoxelTextEditor : VoxelObjectEditorBase<VoxelText>
	{
		[MenuItem("GameObject/3D Object/voxul/Voxel Text")]
		public static void CreateNewText()
		{
			var go = new GameObject("New Voxel Text");
			var r = go.AddComponent<VoxelText>();
			EditorGUIUtility.PingObject(go);
		}

		protected override void DrawSpecificGUI()
		{
		}
	}
}