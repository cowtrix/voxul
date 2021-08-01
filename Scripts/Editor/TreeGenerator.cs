using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul
{
	public class TreeGenerator : EditorWindow
	{
		public int BranchLength = 5;

		private void OnGUI()
		{
			if (GUILayout.Button("Generate"))
			{

			}
		}

		void Generate()
		{
			var go = new GameObject("New Tree");
			var renderer = go.AddComponent<VoxelRenderer>();
			renderer.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();

			var root = Vector3.zero;
			var branchDirection = Vector3.up;
			
			for(var i = root; (i - root).magnitude < BranchLength; i += branchDirection)
			{

			}
		}
	}
}
