#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit
{
	public class VoxelMeshInfoWindow : EditorWindow
	{
		public VoxelMesh Mesh;

		class GUIState
		{
			public int Toolbar, VoxelsPage;
			public Vector2 SubmeshScroll, VoxelsScroll;
		}

		string[] Toolbars => new[]
		{
			"Voxels", "Submeshes",
		};
		string StateKey => $"{nameof(Voxul)}.{nameof(VoxelMeshInfoWindow)}";
		GUIState CurrentState
		{
			get
			{
				return EditorPrefUtility.GetPref<GUIState>(StateKey, new GUIState());
			}
			set
			{
				EditorPrefUtility.SetPref(StateKey, value);
			}
		}

		public void SetData(VoxelMesh mesh)
		{
			Mesh = mesh;
		}

		private void OnGUI()
		{
			var state = CurrentState;
			var helpContent = EditorGUIUtility.IconContent("_Help");
			helpContent.text = Mesh?.name;
			titleContent = helpContent;

			Mesh = (VoxelMesh)EditorGUILayout.ObjectField("Target", Mesh, typeof(VoxelMesh), true);

			state.Toolbar = GUILayout.Toolbar(state.Toolbar, Toolbars);
			switch (Toolbars[state.Toolbar])
			{
				case "Voxels":
					DrawVoxelsTab(state);
					break;
				case "Submeshes":
					DrawSubmeshTab(state);
					break;
			}
			EditorGUILayout.EndScrollView();
			if(GUILayout.Button("Clean Mesh"))
			{
				Mesh.CleanMesh();
				EditorUtility.SetDirty(Mesh);
			}
			CurrentState = state;
		}

		void DrawVoxelsTab(GUIState state)
		{
			const int PageSize = 20;
			int startIndex = state.VoxelsPage * PageSize;
			if(startIndex > Mesh.Voxels.Count)
			{
				startIndex = Mathf.Max(0, Mesh.Voxels.Count - PageSize);
			}
			EditorGUILayout.BeginScrollView(state.VoxelsScroll);

			EditorGUILayout.LabelField("Voxels:", Mesh.Voxels.Count.ToString());
			var chunk = Mesh.Voxels
				.OrderByDescending(v => v.Key.ToVector3().sqrMagnitude)
				.Skip(startIndex).Take(PageSize).ToList();
			for (int i = 0; i < chunk.Count; i++)
			{
				KeyValuePair<VoxelCoordinate, Voxel> vox = chunk[i];
				EditorGUILayout.BeginHorizontal("Box");
				GUILayout.Label($"{i}\t{vox.Key}");
				if (GUILayout.Button("Delete"))
				{
					Mesh.Voxels.Remove(vox.Key);
					Mesh.Invalidate();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("<"))
			{
				state.VoxelsPage--;
			}
			EditorGUILayout.LabelField($"{state.VoxelsPage}/{Mathf.CeilToInt(Mesh.Voxels.Count / PageSize)}");
			if (GUILayout.Button(">"))
			{
				state.VoxelsPage++;
			}
			EditorGUILayout.EndHorizontal();
		}

		void DrawSubmeshTab(GUIState state)
		{
			EditorGUILayout.BeginScrollView(state.SubmeshScroll);
			EditorGUILayout.LabelField("Submeshes", EditorStyles.boldLabel);
			foreach (var submesh in Mesh.UnityMeshInstances)
			{
				EditorGUILayout.BeginVertical("Box");
				if (submesh.UnityMesh)
				{
					EditorGUILayout.ObjectField("Mesh", submesh.UnityMesh, typeof(Mesh), true);
					EditorGUILayout.LabelField("Vertices", submesh.UnityMesh.vertexCount.ToString());
				}
				else
				{
					EditorGUILayout.LabelField("Null submesh! Try rebaking.");
				}
				EditorGUILayout.EndVertical();
			}
		}
	}
}
#endif