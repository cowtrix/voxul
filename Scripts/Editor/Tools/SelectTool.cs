using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[Serializable]
	internal class SelectTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("Selectable Icon");
		public Bounds SelectionBounds;
		protected override EPaintingTool ToolID => EPaintingTool.Select;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			var result = base.DrawInspectorGUI(voxelPainter);
			if (GUILayout.Button("Apply Material To Selection (SHIFT + M)") ||
				(Event.current.isKey && Event.current.shift && Event.current.keyCode == KeyCode.M))
			{
				voxulLogger.Debug($"Applying material");
				foreach (var v in voxelPainter.CurrentSelection)
				{
					voxelPainter.Renderer.Mesh.Voxels[v] = new Voxel(v, CurrentBrush.Copy());
				}
				return true;
			}
			if (GUILayout.Button("Set Voxels For Selection (SHIFT + F)") ||
				(Event.current.isKey && Event.current.shift && Event.current.keyCode == KeyCode.F))
			{
				voxulLogger.Debug($"Setting voxels material");
				var bounds = voxelPainter.CurrentSelection.GetBounds();
				foreach (var vox in voxelPainter.Renderer.Mesh.GetVoxels(bounds))
				{
					var coord = vox.Coordinate;
					DebugHelper.DrawPoint(voxelPainter.Renderer.transform.localToWorldMatrix.MultiplyPoint3x4(coord.ToVector3()), .1f, Color.white, 5);
					voxelPainter.Renderer.Mesh.Voxels[coord] = new Voxel(coord, CurrentBrush.Copy());
				}
				return true;
			}
			if (GUILayout.Button("Subdivide Selection (SHIFT + S)") ||
				(Event.current.isKey && Event.current.shift && Event.current.keyCode == KeyCode.S))
			{
				voxulLogger.Debug($"Subdividing");
				var newSelection = new HashSet<VoxelCoordinate>();
				foreach (var v in voxelPainter.CurrentSelection)
				{
					var vox = voxelPainter.Renderer.Mesh.Voxels[v];
					voxelPainter.Renderer.Mesh.Voxels.Remove(v);
					foreach (var subV in v.Subdivide())
					{
						voxelPainter.Renderer.Mesh.Voxels[subV] = new Voxel(subV, vox.Material.Copy());
						newSelection.Add(subV);
					}
				}
				voxelPainter.SetSelection(newSelection);
				return true;
			}
			return result;
		}
		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer, Event currentEvent,
			List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				if (!currentEvent.shift)
				{
					voxelPainter.SetSelection(null);
				}

				if (selection == null)
				{
					return false;
				}

				foreach (var c in selection)
				{
					voxelPainter.AddSelection(c);
				}
				SelectionBounds = voxelPainter.CurrentSelection.First().ToBounds();
				foreach (var p in voxelPainter.CurrentSelection.Skip(1))
				{
					SelectionBounds.Encapsulate(p.ToBounds());
				}

				if (currentEvent.control)
				{
					foreach (var v in renderer.Mesh.Voxels)
					{
						if (SelectionBounds.Contains(v.Key.ToVector3()))
						{
							voxelPainter.AddSelection(v.Key);
						}
					}
				}

				UseEvent(currentEvent);
			}

			Handles.matrix = renderer.transform.localToWorldMatrix;
			HandleExtensions.DrawWireCube(SelectionBounds.center, SelectionBounds.extents, Quaternion.identity, Color.magenta);

			if (voxelPainter.CurrentSelection.Any() && currentEvent.isKey && currentEvent.keyCode == KeyCode.Delete)
			{
				foreach (var c in voxelPainter.CurrentSelection)
				{
					renderer.Mesh.Voxels.Remove(c);
				}
				voxelPainter.SetSelection(null);
				SelectionBounds = default;
				return true;
			}

			return false;
		}
	}
}