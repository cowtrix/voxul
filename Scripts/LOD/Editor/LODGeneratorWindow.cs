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

		float FillRequirment = .5f;
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
				FillRequirment = EditorGUILayout.Slider("Fill Requirement", FillRequirment, 0, 1);
			}

			if (GUILayout.Button("Generate"))
			{
				foreach (var t in targets)
				{
					if (Mode == ELODMode.Retarget)
					{
						var lodVoxels = LevelOfDetailBuilder.RetargetToLayer(t.Mesh.Voxels.Values, MaxLayer, FillRequirment).ToList();
						if(!lodVoxels.Any())
						{
							Debug.LogWarning($"No voxels were selected for VoxelRenderer {t}", t);
							continue;
						}
						var newLOD = new GameObject()
							.AddComponent<VoxelRenderer>();
						newLOD.transform.SetParent(t.transform);
						newLOD.transform.localPosition = Vector3.zero;
						newLOD.transform.localRotation = Quaternion.identity;
						newLOD.transform.localScale = Vector3.one;
						newLOD.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
						newLOD.Mesh.Voxels = new VoxelMapping(lodVoxels);
						newLOD.Mesh.Invalidate();
						newLOD.Invalidate(false, false);
					}
				}

			}
		}


	}
}
