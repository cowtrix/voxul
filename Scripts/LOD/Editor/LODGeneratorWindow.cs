using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Edit;
using Voxul.Meshing;

namespace Voxul.LevelOfDetail
{
	public class LODGeneratorWindow : EditorWindow
	{
		[MenuItem("Tools/Voxul/LOD Generator")]
		public static void OpenWindow() => EditorWindow.GetWindow<LODGeneratorWindow>();

		public enum ELODMode
		{
			Retarget,
		}

		IEnumerable<VoxelRenderer> Targets => Selection.gameObjects.Select(g => g.GetComponent<VoxelRenderer>());
		ELODMode Mode;

		sbyte MaxLayer;


		private void OnGUI()
		{
			titleContent = new GUIContent("LOD Generator");
			var targets = Targets.ToList();
			if (!targets.Any())
			{
				EditorGUILayout.HelpBox("No VoxelRenderers in selection", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField("Selected Renderers:", targets.Count.ToString());

			Mode = (ELODMode)EditorGUILayout.EnumPopup(Mode);

			if (Mode == ELODMode.Retarget)
			{
				MaxLayer = (sbyte)EditorGUILayout.IntField("May Layer", MaxLayer);
			}

			if (GUILayout.Button("Generate"))
			{
				foreach (var t in targets)
				{
					if (Mode == ELODMode.Retarget)
					{
						var newLOD = new GameObject()
							.AddComponent<VoxelRenderer>();
						newLOD.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
						newLOD.Mesh.Voxels = new VoxelMapping(LevelOfDetailBuilder.RetargetToLayer(t.Mesh.Voxels.Values, MaxLayer));
						newLOD.Mesh.Invalidate();
						newLOD.Invalidate(false, false);
					}
				}

			}
		}


	}
}
