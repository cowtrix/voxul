using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit
{
	[Serializable]
	internal class PaintTool : VoxelPainterTool
	{
		private double m_lastAdd;
		private VoxelMesh m_previewMesh;
		private Color LerpColor { get => EditorPrefUtility.GetPref("voxul_lerpcolor", Color.white); set => EditorPrefUtility.SetPref("voxul_lerpcolor", value); }
		public bool LerpEnabled { get => EditorPrefUtility.GetPref("voxul_lerpenabled", false); set => EditorPrefUtility.SetPref("voxul_lerpenabled", value); }

		public override void OnEnable()
		{
			m_previewMesh = ScriptableObject.CreateInstance<VoxelMesh>();
			base.OnEnable();
		}

		public override void OnDisable()
		{
			GameObject.DestroyImmediate(m_previewMesh);
			m_previewMesh = null;
		}

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			LerpEnabled = EditorGUILayout.Toggle("Enable color lerp", LerpEnabled);
			LerpColor = EditorGUILayout.ColorField("Lerp Color", LerpColor);
			return base.DrawInspectorGUI(voxelPainter);
		}

		protected override bool GetVoxelDataFromPoint(VoxelPainter voxelPainterTool, VoxelRenderer renderer, MeshCollider collider,
			Vector3 hitPoint, Vector3 hitNorm, int triIndex, sbyte layer, out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			var result = base.GetVoxelDataFromPoint(voxelPainterTool, renderer, collider, hitPoint, hitNorm, triIndex, layer, out selection, out hitDir);
			if (result)
			{
				Handles.matrix = renderer.transform.localToWorldMatrix;
				foreach (var s in selection)
				{
					var layerScale = VoxelCoordinate.LayerToScale(s.Layer);
					var dirs = new HashSet<EVoxelDirection>() { hitDir };
					if (Event.current.shift)
					{
						foreach (var d in VoxelMesh.Directions)
						{
							dirs.Add(d);
						}
					}
					foreach (var d in dirs)
					{
						var rot = VoxelCoordinate.DirectionToQuaternion(d);
						var pos = s.ToVector3() + rot * (layerScale * .5f * Vector3.up);
						HandleExtensions.DrawWireCube(pos, new Vector3(layerScale / 2f, layerScale * .05f, layerScale / 2f), rot, Color.magenta);
					}

				}
			}
			return result;
		}

		protected override EPaintingTool ToolID => EPaintingTool.Paint;

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				if (EditorApplication.timeSinceStartup < m_lastAdd + .1f)
				{
					Debug.LogWarning($"Swallowed double event");
					return false;
				}
				m_lastAdd = EditorApplication.timeSinceStartup;
				var creationList = new HashSet<VoxelCoordinate>(selection);
				/*if (currentEvent.control && currentEvent.shift)
				{
					var bounds = voxelPainter.CurrentSelection.GetBounds();
					bounds.Encapsulate(brushCoord.ToBounds());
					foreach (VoxelCoordinate coord in renderer.Mesh.GetVoxelCoordinates(bounds, voxelPainter.CurrentLayer))
					{
						creationList.Add(coord);
					}
				}*/
				if (SetVoxelSurface(creationList, renderer, hitDir, currentEvent))
				{
					voxelPainter.SetSelection(creationList);
				}
			}
			return false;
		}

		private bool SetVoxelSurface(IEnumerable<VoxelCoordinate> coords, VoxelRenderer renderer, EVoxelDirection dir, Event currentEvent)
		{
			var coordList = coords.ToList();
			foreach (var brushCoord in coordList)
			{
				if (!renderer.Mesh.Voxels.TryGetValue(brushCoord, out var vox))
				{
					continue;
				}
				if (currentEvent.shift)
				{
					vox.Material = CurrentBrush;
				}
				else
				{
					Debug.Log($"Set voxel at {brushCoord} ({dir})");
					var surface = CurrentBrush.GetSurface(dir);

					if (LerpEnabled)
					{
						surface.Albedo = Color.Lerp(surface.Albedo, LerpColor, UnityEngine.Random.value);
					}

					vox.Material = vox.Material.SetSurface(dir, surface);
				}
				renderer.Mesh.Voxels[brushCoord] = vox;
			}
			renderer.Invalidate(true);
			return true;
		}
	}
}