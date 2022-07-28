#if UNITY_EDITOR
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
			CreateNewInScene();
		}

		protected override void DrawSpecificGUI()
		{
		}
	}
}
#endif