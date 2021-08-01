using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	[Serializable]
	internal class RemoveTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("TreeEditor.Trash");
		protected override EPaintingTool ToolID => EPaintingTool.Remove;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			return false;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, HashSet<VoxelCoordinate> selection, EVoxelDirection hitDir, Vector3 hitPos)
		{
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				foreach (var brushCoord in selection)
				{
					renderer.Mesh.Voxels.Remove(brushCoord);
				}
				voxelPainter.SetSelection(null);
				UseEvent(currentEvent);
				return true;
			}
			return false;
		}
	}
}