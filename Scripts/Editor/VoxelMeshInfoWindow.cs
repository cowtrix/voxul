using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit
{
	public class VoxelMeshInfoWindow : EditorWindow
	{
		public VoxelMesh Mesh;

		private Vector2 m_scroll;

		public void SetData(VoxelMesh mesh)
		{
			Mesh = mesh;
		}

		private void OnGUI()
		{
			var helpContent = EditorGUIUtility.IconContent("_Help");
			helpContent.text = Mesh?.name;
			titleContent = helpContent;

			Mesh = (VoxelMesh)EditorGUILayout.ObjectField("Target", Mesh, typeof(VoxelMesh), true);

			EditorGUILayout.BeginScrollView(m_scroll);

			EditorGUILayout.LabelField("Voxels", Mesh.Voxels.Count.ToString());

			EditorGUILayout.LabelField("Submeshes", EditorStyles.boldLabel);
			foreach(var submesh in Mesh.UnityMeshInstances)
			{
				EditorGUILayout.BeginVertical("Box");
				EditorGUILayout.ObjectField("Mesh", submesh.UnityMesh, typeof(Mesh), true);
				EditorGUILayout.LabelField("Vertices", submesh.UnityMesh.vertexCount.ToString());
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndScrollView();
			if(GUILayout.Button("Clean Mesh"))
			{
				Mesh.CleanMesh();
				EditorUtility.SetDirty(Mesh);
			}
		}
	}
}