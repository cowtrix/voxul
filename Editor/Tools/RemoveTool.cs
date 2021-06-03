using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxulEngine.Painter
{
	[Serializable]
	internal class RemoveTool : VoxelPainterTool
	{
		protected override EPaintingTool ToolID => EPaintingTool.Remove;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			return false;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				foreach (var brushCoord in selection)
				{
					renderer.Mesh.Voxels.Remove(brushCoord);
				}
				return true;
			}
			return false;
		}
	}
}