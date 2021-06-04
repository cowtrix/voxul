using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public static class HandleExtensions
	{
		public static void DrawWireCube(Vector3 origin, Vector3 extents, Quaternion rotation, Color color, float thickness = 0f)
		{
			var verts = new[]
			{
				// Top square
				origin + rotation*new Vector3(extents.x, extents.y, extents.z),
				origin + rotation*new Vector3(-extents.x, extents.y, extents.z),
				origin + rotation*new Vector3(extents.x, extents.y, -extents.z),
				origin + rotation*new Vector3(-extents.x, extents.y, -extents.z),

				// Bottom square
				origin + rotation*new Vector3(extents.x, -extents.y, extents.z),
				origin + rotation*new Vector3(-extents.x, -extents.y, extents.z),
				origin + rotation*new Vector3(extents.x, -extents.y, -extents.z),
				origin + rotation*new Vector3(-extents.x, -extents.y, -extents.z)
			};

			Handles.color = color;

			// top square
			Handles.DrawLine(verts[0], verts[2], thickness);
			Handles.DrawLine(verts[1], verts[3], thickness);
			Handles.DrawLine(verts[1], verts[0], thickness);
			Handles.DrawLine(verts[2], verts[3], thickness);

			// bottom square
			Handles.DrawLine(verts[4], verts[6], thickness);
			Handles.DrawLine(verts[5], verts[7], thickness);
			Handles.DrawLine(verts[5], verts[4], thickness);
			Handles.DrawLine(verts[6], verts[7], thickness);

			// connections
			Handles.DrawLine(verts[0], verts[4], thickness);
			Handles.DrawLine(verts[1], verts[5], thickness);
			Handles.DrawLine(verts[2], verts[6], thickness);
			Handles.DrawLine(verts[3], verts[7], thickness);

			Handles.color = Color.white;
		}

		public static void DrawSpline(Vector3 start, Vector3 end)
		{
			Handles.DrawLine(start, end);
		}

		private static Vector3[] _vertBuffer;

		public static void DrawXZCell(Vector3 position, float size, Color fillColor)
		{
			DrawXZCell(position, Vector2.one * size, Quaternion.identity, fillColor);
		}

		public static void DrawXZCell(Vector3 position, Vector2 size, Quaternion rotation, Color fillColor)
		{
			if (_vertBuffer == null || _vertBuffer.Length != 4)
			{
				_vertBuffer = new Vector3[4];
			}
			_vertBuffer[0] = position + rotation * new Vector3(size.x / 2, 0, size.y / 2);
			_vertBuffer[1] = position + rotation * new Vector3(size.x / 2, 0, -size.y / 2);
			_vertBuffer[2] = position + rotation * new Vector3(-size.x / 2, 0, -size.y / 2);
			_vertBuffer[3] = position + rotation * new Vector3(-size.x / 2, 0, size.y / 2);
			var prevColor = Handles.color;
			Handles.color = fillColor;
			Handles.DrawAAConvexPolygon(_vertBuffer[0], _vertBuffer[1], _vertBuffer[2], _vertBuffer[3]);
			Handles.color = prevColor;
		}
	}
}