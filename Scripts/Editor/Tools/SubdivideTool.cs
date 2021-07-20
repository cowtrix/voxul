using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	[Serializable]
	internal class SubdivideTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("d_PreMatCube");
		protected override EPaintingTool ToolID => EPaintingTool.Subdivide;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			return false;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
			{
				foreach (var brushCoord in selection)
				{
					var vox = renderer.Mesh.Voxels[brushCoord];
					renderer.Mesh.Voxels.Remove(brushCoord);
					foreach (var sub in selection.SelectMany(s => s.Subdivide()))
					{
						renderer.Mesh.Voxels[sub] = new Voxel(sub, vox.Material.Copy());
					}
				}
				UseEvent(currentEvent);
				return true;
			}
			return false;
		}

		protected override bool GetVoxelDataFromPoint(VoxelPainter painter, VoxelRenderer renderer, MeshCollider collider, Vector3 hitPoint, Vector3 hitNorm, int triIndex,
			out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			if (base.GetVoxelDataFromPoint(painter, renderer, collider, hitPoint, hitNorm, triIndex, out selection, out hitDir))
			{
				foreach (var sub in selection.SelectMany(v => v.Subdivide()))
				{
					var subLayerScale = VoxelCoordinate.LayerToScale(sub.Layer);
					var subPos = renderer.transform.localToWorldMatrix.MultiplyPoint3x4(sub.ToVector3());
					var subScale = renderer.transform.localToWorldMatrix.MultiplyVector(subLayerScale * Vector3.one * .4f);
					HandleExtensions.DrawWireCube(subPos, subScale, renderer.transform.rotation, Color.magenta);
				}
				return true;
			}
			return false;
		}
	}
}