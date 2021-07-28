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
			CreateNewInScene();
		}

		protected override void DrawSpecificGUI()
		{

		}
	}
}