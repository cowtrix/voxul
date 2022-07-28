#if UNITY_EDITOR
using System.Linq;
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
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Rebake All"))
			{
				foreach (var t in targets)
				{
					var l = t as RenderPlaneVoxelLOD;
					foreach (var p in l.RenderPlanes)
					{
						l.RebakePlane(p, false);
					}
					l.RefreshAtlas();
				}
			}
			base.OnInspectorGUI();
		}

		void OnSceneGUI()
		{
			void DoVoxelCoordinateSceneView(ref Vector2Int coord, ref int offset, Quaternion rot, sbyte layer, EVoxelDirection dir)
			{
				var scale = VoxelCoordinate.LayerToScale(layer);
				var planePos = coord.ReverseSwizzleForDir(offset, dir).ToVector3() * scale;
				planePos = Handles.PositionHandle(planePos, rot);
				coord = (planePos / scale)
					.RoundToVector3Int()
					.SwizzleForDir(dir, out var offsetFloat)
					.RoundToVector2Int();
				offset = (int)offsetFloat;
			}

			Tools.hidden = true;
			var t = (RenderPlaneVoxelLOD)target;
			Handles.matrix = t.transform.localToWorldMatrix;
			for (int i = 0; i < t.RenderPlanes.Count; i++)
			{
				RenderPlaneVoxelLOD.RenderPlane plane = t.RenderPlanes[i];
				Handles.matrix = t.transform.localToWorldMatrix;

				foreach (var duplicate in t.RenderPlanes.Where(p => p.Albedo == plane.Albedo && p != plane))
				{
					duplicate.Albedo = null;
				}
				if (plane.CastDepth <= 1)
				{
					plane.CastDepth = 1;
				}
				if (plane.Min.x > plane.Max.x)
				{
					var tmp = plane.Min.x;
					plane.Min.x = plane.Max.x;
					plane.Max.x = tmp;
				}
				if (plane.Min.y > plane.Max.y)
				{
					var tmp = plane.Min.y;
					plane.Min.y = plane.Max.y;
					plane.Max.y = tmp;
				}

				DoVoxelCoordinateSceneView(ref plane.Min, ref plane.Offset, t.transform.localRotation, plane.Layer, plane.Direction);
				DoVoxelCoordinateSceneView(ref plane.Max, ref plane.Offset, t.transform.localRotation, plane.Layer, plane.Direction);

				var b = new VoxelCoordinate(plane.MinVec3, plane.Layer).ToBounds();
				b.Encapsulate(new VoxelCoordinate(plane.MaxVec3, plane.Layer).ToBounds());

				var dirVec = VoxelCoordinate.DirectionToVector3(plane.Direction) * VoxelCoordinate.LayerToScale(plane.Layer) * .5f;
				HandleExtensions.DrawWireCube(b.center + dirVec, b.extents.ReverseSwizzleForDir(0, plane.Direction), Quaternion.identity, Color.white);

				if (t.RenderPlanes[i] != plane)
				{
					Undo.RecordObject(target, "Edit Render Plane");
					t.RenderPlanes[i] = plane;
				}
			}
		}
	}
}
#endif