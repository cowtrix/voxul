using UnityEditor;
using UnityEngine;
using Voxul.Edit;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(RenderPlaneVoxelLOD))]
	public class RenderPlaneVoxeLODEditor : Editor
	{

		public bool SnapToGrid
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainterLOD_SnapToGrid", true);
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainterLOD_SnapToGrid", value);
			}
		}
		public sbyte SnapLayer
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainterLOD_SnapLayer", (sbyte)0);
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainterLOD_SnapLayer", value);
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			SnapToGrid = EditorGUILayout.Toggle("Snap to Grid", SnapToGrid);
			if (SnapToGrid)
			{
				SnapLayer = (sbyte)EditorGUILayout.IntSlider("Snap Layer", SnapLayer, -10, 10);
			}
		}

		void OnSceneGUI()
		{
			Tools.hidden = true;
			var t = (RenderPlaneVoxelLOD)target;
			for (int i = 0; i < t.RenderPlanes.Count; i++)
			{
				RenderPlaneVoxelLOD.RenderPlane plane = t.RenderPlanes[i];
				Handles.matrix = t.transform.localToWorldMatrix;

				var layerScale = VoxelCoordinate.LayerToScale(SnapLayer);

				var rot = VoxelCoordinate.DirectionToQuaternion(plane.Direction);
				var castDist = plane.CastDepth * layerScale;
				HandleExtensions.DrawWireCube(plane.Position + rot * Vector3.forward * castDist * .5f, new Vector3(plane.Size.x, castDist, plane.Size.y), rot, Color.white);

				var reverseSwizzleSize = plane.Size.ReverseSwizzleForDir(0, plane.Direction);

				if (Tools.current == Tool.Move)
					plane.Position = Handles.PositionHandle(plane.Position, rot);
				if (Tools.current == Tool.Scale)
					reverseSwizzleSize = Handles.DoScaleHandle(reverseSwizzleSize, plane.Position, t.transform.rotation * rot, 2);

				plane.Size = reverseSwizzleSize.SwizzleForDir(plane.Direction, out _);

				if (SnapToGrid)
				{
					plane.Position = plane.Position.FloorToIncrement(layerScale);
					plane.Size = plane.Size.FloorToIncrement(VoxelCoordinate.LayerToScale(SnapLayer));
				}

				if (t.RenderPlanes[i] != plane)
				{
					Undo.RecordObject(target, "Edit Render Plane");
					t.RenderPlanes[i] = plane;
				}
			}
		}
	}
}