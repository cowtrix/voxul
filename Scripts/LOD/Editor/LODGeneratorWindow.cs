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
			RenderPlanes
		}

		public bool SnapToGrid = false;
		public sbyte SnapLayer;
		public List<LevelOfDetailBuilder.RenderPlane> RenderPlanes = new List<LevelOfDetailBuilder.RenderPlane>();

		IEnumerable<VoxelRenderer> Targets => Selection.gameObjects.Select(g => g.GetComponent<VoxelRenderer>());
		ELODMode Mode;

		sbyte MaxLayer;

		private void OnEnable()
		{
			SceneView.duringSceneGui += OnSceneView;
		}

		private void OnDisable()
		{
			SceneView.duringSceneGui -= OnSceneView;
		}

		private void OnSceneView(SceneView obj)
		{
			foreach (var target in Targets)
			{
				for (int i = 0; i < RenderPlanes.Count; i++)
				{
					LevelOfDetailBuilder.RenderPlane plane = RenderPlanes[i];
					Handles.matrix = target.transform.localToWorldMatrix;
					HandleExtensions.DrawWireCube(plane.Position, new Vector3(plane.Size.x, 0, plane.Size.y), plane.Rotation, Color.white);

					if(Tools.current == Tool.Move)
						plane.Position = Handles.PositionHandle(plane.Position, plane.Rotation);


					RenderPlanes[i] = plane;
				}
			}

		}

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
			else if (Mode == ELODMode.RenderPlanes)
			{
				EditorGUILayout.BeginVertical("Box");
				EditorGUILayout.LabelField("Planes");
				for (int i = 0; i < RenderPlanes.Count; i++)
				{
					LevelOfDetailBuilder.RenderPlane plane = RenderPlanes[i];
					EditorGUILayout.BeginVertical("Box");
					plane.Position = EditorGUILayout.Vector3Field("Position", plane.Position);
					plane.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", plane.Rotation.eulerAngles));
					plane.Size = EditorGUILayout.Vector2Field("Size", plane.Size);
					EditorGUILayout.EndVertical();
					RenderPlanes[i] = plane;
				}
				if (GUILayout.Button("+"))
				{
					RenderPlanes.Add(new LevelOfDetailBuilder.RenderPlane());
				}
				EditorGUILayout.EndVertical();
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
