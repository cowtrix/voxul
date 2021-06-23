using UnityEngine;
namespace Voxul.Utilities
{
	public static class VectorExtensions
	{
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