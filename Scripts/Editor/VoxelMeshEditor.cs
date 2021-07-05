using UnityEditor;
using Voxul.Meshing;

namespace Voxul.Edit
{
	[CustomEditor(typeof(VoxelMesh))]
	internal class VoxelMeshEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
}