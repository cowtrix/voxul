using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[Serializable]
	internal class PaintTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("ClothInspector.PaintTool");
		private double m_lastAdd;
		private VoxelMesh m_previewMesh;
		private SerializableGradient LerpColor { get => EditorPrefUtility.GetPref("voxul_lerpcolor", new SerializableGradient(AddTool.DefaultGradient)); set => EditorPrefUtility.SetPref("voxul_lerpcolor", value); }
		private bool LerpEnabled { get => EditorPrefUtility.GetPref("voxul_lerpenabled", false); set => EditorPrefUtility.SetPref("voxul_lerpenabled", value); }

		public override void OnEnable()
		{
			m_previewMesh = ScriptableObject.CreateInstance<VoxelMesh>();
			base.OnEnable();
		}

		public override void OnDisable()
		{
			GameObject.DestroyImmediate(m_previewMesh);
			m_previewMesh = null;
			base.OnDisable();
		}

		protected override bool GetVoxelDataFromPoint(VoxelPainter voxelPainterTool, VoxelRenderer renderer, MeshCollider collider,
			Vector3 hitPoint, Vector3 hitNorm, int triIndex, out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			var result = base.GetVoxelDataFromPoint(voxelPainterTool, renderer, collider, hitPoint, hitNorm, triIndex, out selection, out hitDir);
			if (result)
			{
				Handles.matrix = renderer.transform.localToWorldMatrix;
				foreach (var s in selection)
				{
					var layerScale = VoxelCoordinate.LayerToScale(s.Layer);
					var dirs = new HashSet<EVoxelDirection>() { hitDir };
					if (Event.current.shift)
					{
						foreach (var d in VoxelExtensions.Directions)
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
			if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
			{
				if (EditorApplication.timeSinceStartup < m_lastAdd + .1f)
				{
					voxulLogger.Warning($"Swallowed double event");
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
				UseEvent(currentEvent);
			}
			return false;
		}

		protected override int GetToolWindowHeight()
		{
			return base.GetToolWindowHeight() + 25;
		}

		protected override void DrawToolLayoutGUI(Rect rect, Event currentEvent, VoxelPainter voxelPainter)
		{
			base.DrawToolLayoutGUI(rect, currentEvent, voxelPainter);
			EditorGUILayout.BeginHorizontal();
			GUI.color = LerpEnabled ? Color.green : Color.white;
			if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreTextureRGB")))
			{
				LerpEnabled = !LerpEnabled;
			}
			GUI.color = Color.white;
			LerpColor = new SerializableGradient(EditorGUILayout.GradientField(LerpColor.ToGradient()));
			EditorGUILayout.EndHorizontal();
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
					vox.Material = CurrentBrush.Copy();
				}
				else
				{
					voxulLogger.Debug($"Set voxel at {brushCoord} ({dir})");
					var surface = CurrentBrush.GetSurface(dir);

					if (LerpEnabled)
					{
						surface.Albedo = LerpColor.ToGradient().Evaluate(UnityEngine.Random.value);
					}

					if (vox.Material.Overrides == null)
					{
						vox.Material.Overrides = new DirectionOverride[0];
					}
					vox.Material.Overrides = vox.Material.Overrides.Where(o => o.Direction != dir).Append(new DirectionOverride
					{
						Direction = dir,
						Surface = surface,
					}).ToArray();
				}
				renderer.Mesh.Voxels[brushCoord] = vox;
			}
			renderer.Invalidate(true, true);
			return true;
		}
	}
}