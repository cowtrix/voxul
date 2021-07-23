using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Voxul.Utilities
{
	public static class VectorExtensions
	{
		public static bool Approximately(this Vector3 a, Vector3 b)
		{
			return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
		}

		public static IEnumerable<Vector3> EnumerateVertices(this Bounds bounds)
		{
			var w = bounds.size.x / 2f;
			var h = bounds.size.y / 2f;
			var l = bounds.size.z / 2f;
			var c = bounds.center;
			yield return c + new Vector3(w, h, l);
			yield return c + new Vector3(w, -h, l);
			yield return c + new Vector3(w, h, -l);
			yield return c + new Vector3(w, -h, -l);
			yield return c + new Vector3(-w, h, l);
			yield return c + new Vector3(-w, -h, l);
			yield return c + new Vector3(-w, h, -l);
			yield return c + new Vector3(-w, -h, -l);
		}

		public static Vector3 QuadLerp(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
		{
			Vector3 abu = Vector3.Lerp(a, b, u);
			Vector3 dcu = Vector3.Lerp(d, c, u);
			return Vector3.Lerp(abu, dcu, v);
		}

		public static float GetBilinear(this float[,] array, float x, float y)
		{
			var width = array.GetLength(0);
			var fracX = x * (width - 1);
			var lowX = Mathf.FloorToInt(fracX);
			var highX = Mathf.Min(Mathf.CeilToInt(fracX), width - 1);
			fracX -= lowX;

			var height = array.GetLength(1);
			var fracY = y * (height - 1);
			var lowY = Mathf.FloorToInt(fracY);
			var highY = Mathf.Min(Mathf.CeilToInt(fracY), height - 1);
			fracY -= lowY;

			var p1 = array[lowX, highY];
			var p2 = array[highX, highY];
			var p3 = array[lowX, lowY];
			var p4 = array[highX, lowY];

			float abu = Mathf.Lerp(p1, p2, fracX);
			float dcu = Mathf.Lerp(p3, p4, fracX);
			return Mathf.Lerp(abu, dcu, 1 - fracY);
		}

		public static Vector2 QuadLerp(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u, float v)
		{
			// Given a (u,v) coordinate that defines a 2D local position inside a planar quadrilateral, find the
			// absolute 3D (x,y,z) coordinate at that location.
			//
			//  0 <----u----> 1
			//  a ----------- b    0
			//  |             |   /|\
			//  |             |    |
			//  |             |    v
			//  |  *(u,v)     |    |
			//  |             |   \|/
			//  d------------ c    1
			//
			// a, b, c, and d are the vertices of the quadrilateral. They are assumed to exist in the
			// same plane in 3D space, but this function will allow for some non-planar error.
			//
			// Variables u and v are the two-dimensional local coordinates inside the quadrilateral.
			// To find a point that is inside the quadrilateral, both u and v must be between 0 and 1 inclusive.  
			// For example, if you send this function u=0, v=0, then it will return coordinate "a".  
			// Similarly, coordinate u=1, v=1 will return vector "c". Any values between 0 and 1
			// will return a coordinate that is bi-linearly interpolated between the four vertices.
			Vector2 abu = Vector2.Lerp(a, b, u);
			Vector2 dcu = Vector2.Lerp(d, c, u);
			return Vector2.Lerp(abu, dcu, v);
		}

		public static Bounds EncapsulateAll(this IEnumerable<Bounds> bounds)
		{
			if(bounds == null || !bounds.Any())
			{
				return default;
			}
			var first = bounds.First();
			foreach(var b in bounds.Skip(1))
			{
				first.Encapsulate(b);
			}
			return first;
		}
		public static Vector3 Inverse(this Vector3 obj)
		{
			return new Vector3(1 / obj.x, 1 / obj.y, 1 / obj.z);
		}
		public static Vector3 Abs(this Vector3 obj)
		{
			return new Vector3(Mathf.Abs(obj.x), Mathf.Abs(obj.y), Mathf.Abs(obj.z));
		}
		public static Vector2 Abs(this Vector2 obj)
		{
			return new Vector3(Mathf.Abs(obj.x), Mathf.Abs(obj.y));
		}
		public static Vector2 xy(this Vector3 obj)
		{
			return new Vector2(obj.x, obj.y);
		}
		public static Vector3 xzy(this Vector3 obj)
		{
			return new Vector3(obj.x, obj.z, obj.y);
		}
		public static Vector3 zyx(this Vector3 obj)
		{
			return new Vector3(obj.z, obj.y, obj.x);
		}
		public static Vector2 yx(this Vector3 obj)
		{
			return new Vector2(obj.y, obj.x);
		}
		public static Vector2 zy(this Vector3 obj)
		{
			return new Vector2(obj.z, obj.y);
		}
		public static Vector2 xz(this Vector3 obj)
		{
			return new Vector2(obj.x, obj.z);
		}
		public static Vector2 zx(this Vector3 obj)
		{
			return new Vector2(obj.z, obj.x);
		}
		public static Vector2 yz(this Vector3 obj)
		{
			return new Vector2(obj.y, obj.z);
		}
		public static Vector3 ClampMagnitude(this Vector3 obj, float magnitude)
		{
			if (obj.sqrMagnitude > magnitude * magnitude)
			{
				return obj.normalized * magnitude;
			}
			return obj;
		}
		public static Vector3 Clamp(this Vector3 obj, Vector3 min, Vector3 max)
		{
			return new Vector3(Mathf.Clamp(obj.x, min.x, max.x), Mathf.Clamp(obj.y, min.y, max.y), Mathf.Clamp(obj.z, min.z, max.z));
		}
		public static Vector3 Round(this Vector3 vec)
		{
			return new Vector3(Mathf.Round(vec.x), Mathf.Round(vec.y), Mathf.Round(vec.z));
		}
		public static Vector3 Floor(this Vector3 vec)
		{
			return new Vector3(Mathf.Floor(vec.x), Mathf.Floor(vec.y), Mathf.Floor(vec.z));
		}
		public static Vector3 Ceil(this Vector3 vec)
		{
			return new Vector3(Mathf.Ceil(vec.x), Mathf.Ceil(vec.y), Mathf.Ceil(vec.z));
		}
		public static Vector3 ToRadian(this Vector3 obj)
		{
			return Mathf.Deg2Rad * obj;
		}
		public static Vector3 Clamp360Ranges(this Vector3 obj)
		{
			if (obj.x < -180)
			{
				obj.x += 360;
			}

			if (obj.x > 180)
			{
				obj.x -= 360;
			}

			if (obj.y < -180)
			{
				obj.y += 360;
			}

			if (obj.y > 180)
			{
				obj.y -= 360;
			}

			if (obj.z < -180)
			{
				obj.z += 360;
			}

			if (obj.z > 180)
			{
				obj.z -= 360;
			}

			return obj;
		}

		public static Vector4 ToVector4(this Vector3 v, float w)
		{
			return new Vector4(v.x, v.y, v.z, w);
		}

		public static Vector3 xyz(this Vector4 w)
		{
			return new Vector3(w.x * w.w, w.y * w.w, w.z * w.w);
		}

		public static Vector3 x0z(this Vector2 w, float y = 0)
		{
			return new Vector3(w.x, y, w.y);
		}

		public static Vector3 xy0(this Vector2 w, float z = 0)
		{
			return new Vector3(w.x, w.y, z);
		}

		public static Vector3 Flatten(this Vector3 obj)
		{
			return new Vector3(obj.x, 0, obj.z);
		}

		public static Vector2 RemoveNans(this Vector2 n)
		{
			return new Vector2(float.IsNaN(n.x) ? 0 : n.x, float.IsNaN(n.y) ? 0 : n.y);
		}

		public static Vector4 RemoveNans(this Vector4 n)
		{
			return new Vector4(
				float.IsNaN(n.x) ? 0 : n.x, 
				float.IsNaN(n.y) ? 0 : n.y,
				float.IsNaN(n.x) ? 0 : n.z,
				float.IsNaN(n.x) ? 0 : n.w);
		}

		public static Vector3 RandomNormalized()
		{
			return new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
		}

		public static Vector3 RoundToIncrement(this Vector3 v, float snapValue)
		{
			return new Vector3
			(
				snapValue * Mathf.Round(v.x / snapValue),
				snapValue * Mathf.Round(v.y / snapValue),
				snapValue * Mathf.Round(v.z / snapValue)
			);
		}

		public static Vector3 FloorToIncrement(this Vector3 v, float snapValue)
		{
			return new Vector3
			(
				snapValue * Mathf.Floor(v.x / snapValue),
				snapValue * Mathf.Floor(v.y / snapValue),
				snapValue * Mathf.Floor(v.z / snapValue)
			);
		}

		public static Vector2 RoundToIncrement(this Vector2 v, float snapValue)
		{
			return new Vector3
			(
				snapValue * Mathf.Round(v.x / snapValue),
				snapValue * Mathf.Round(v.y / snapValue)
			);
		}

		public static Vector2 FloorToIncrement(this Vector2 v, float snapValue)
		{
			return new Vector3
			(
				snapValue * Mathf.Floor(v.x / snapValue),
				snapValue * Mathf.Floor(v.y / snapValue)
			);
		}

		public static float ManhattenDistance(this Vector3 a, Vector3 b)
		{
			return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
		}
		public static bool IsOnAxis(this Vector3 vec)
		{
			vec = vec.normalized;
			return Mathf.Abs(Vector3.Dot(vec, Vector3.up)) == 1 ||
				Mathf.Abs(Vector3.Dot(vec, Vector3.right)) == 1 ||
				Mathf.Abs(Vector3.Dot(vec, Vector3.forward)) == 1;
		}

		public static Vector3 ClosestAxisNormal(this Vector3 vec)
		{
			var sign = new Vector3(Mathf.Sign(vec.x), Mathf.Sign(vec.y), Mathf.Sign(vec.z));
			vec = vec.Abs();
			return vec.x > vec.y ? (vec.x > vec.z ? Vector3.right * sign.x : Vector3.forward * sign.z)
				: (vec.y > vec.z ? Vector3.up * sign.y : Vector3.forward * sign.z);
		}
	}
}