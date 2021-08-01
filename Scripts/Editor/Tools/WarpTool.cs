using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[Serializable]
	internal class WarpTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("SpriteCollider Icon");

		protected override bool DrawSceneGUIWithNoSelection => true;

		public Vector3 CurrentWarpPosition;
		public Vector3 Offset;

		protected override EPaintingTool ToolID => EPaintingTool.Warp;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			var renderer = voxelPainter.Renderer;
			GUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Warp Data:", renderer.Mesh.PointMapping.Count.ToString());
			GUILayout.EndVertical();
			if(GUILayout.Button("Clear Warp Data"))
			{
				Offset = default;
				
				renderer.Mesh.PointMapping.Clear();
				renderer.Mesh.Invalidate();
				renderer.Invalidate(false, false);
			}
			var newOffset = EditorGUILayout.Vector3Field("Offset", Offset);
			if(newOffset != Offset)
			{
				Offset = newOffset;
				renderer.Mesh.PointMapping[CurrentWarpPosition] = Offset;
				renderer.Mesh.Invalidate();
				renderer.Invalidate(false, false);
			}
			return false;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter painter, VoxelRenderer Renderer, Event currentEvent, HashSet<VoxelCoordinate> selection, EVoxelDirection hitDir, Vector3 hitPos)
		{
			var localHitPos = Renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPos);
			var firstVox = selection?.FirstOrDefault();

			//Tools.current = Tool.Custom;
			Handles.matrix = Renderer.transform.localToWorldMatrix;

			if (firstVox.HasValue && currentEvent.shift)
			{
				var closestVert = firstVox.Value.ToBounds()
					.EnumerateVertices()
					.OrderBy(p => Vector3.Distance(p, localHitPos))
					.First();
				Handles.DrawWireCube(closestVert, Vector3.one * firstVox.Value.GetScale() * .1f);
				Handles.color = Color.grey;
				Renderer.Mesh.PointMapping.TryGetValue(CurrentWarpPosition, out var prevOffset);
				Handles.DrawLine(closestVert, closestVert + prevOffset);
				Handles.color = Color.white;
				if (currentEvent.isMouse && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
				{
					// Select point
					CurrentWarpPosition = closestVert;
					UseEvent(currentEvent);
					return false;
				}
			}

			if(CurrentWarpPosition == default)
			{
				return false;
			}

			if (Renderer.Mesh.PointMapping.TryGetValue(CurrentWarpPosition, out var currentOffset))
			{
				Offset = currentOffset;
			}
			else
			{
				Offset = Vector3.zero;
			}
			var newOffset = Handles.PositionHandle(CurrentWarpPosition + Offset, Quaternion.identity) - CurrentWarpPosition;
			Handles.DrawLine(CurrentWarpPosition, CurrentWarpPosition + newOffset);
			if (Offset != newOffset)
			{
				Offset = newOffset;
				Renderer.Mesh.PointMapping[CurrentWarpPosition] = Offset;
				Renderer.Mesh.Invalidate();
				Renderer.Invalidate(false, false);
			}

			return true;
		}
	}
}