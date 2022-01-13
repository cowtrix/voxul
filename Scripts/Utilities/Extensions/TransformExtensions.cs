using UnityEngine;
namespace Voxul.Utilities
{
	public static class TransformExtensions
	{
		public static int GetHierarchyDepth(this Transform transform)
		{
			int depth = 0;
			MarchUpHierarchy(transform, ref depth);
			return depth;
		}

		private static void MarchUpHierarchy(Transform t, ref int count)
		{
			if (t.parent == null)
			{
				return;
			}
			count++;
			MarchUpHierarchy(t.parent, ref count);
		}

		public static Vector3 LocalForward(this Transform t) => t.parent.worldToLocalMatrix.MultiplyVector(t.forward);
		public static Vector3 LocalUp(this Transform t) => t.parent.worldToLocalMatrix.MultiplyVector(t.up);
		public static Vector3 LocalRight(this Transform t) => t.parent.worldToLocalMatrix.MultiplyVector(t.right);

		//Breadth-first search
		public static Transform FindDeepChild(this Transform aParent, string aName)
		{
			if (aName == aParent.name)
			{
				return aParent;
			}
			var result = aParent.Find(aName);
			if (result != null)
				return result;
			foreach (Transform child in aParent)
			{
				result = child.FindDeepChild(aName);
				if (result != null)
					return result;
			}
			return null;
		}

		public static void ApplyTRSMatrix(this Transform transform, Matrix4x4 matrix)
		{
			transform.localScale = matrix.GetScale();
			transform.rotation = matrix.GetRotation();
			transform.position = matrix.GetPosition();
		}

		public static void ApplyLocalTRSMatrix(this Transform transform, Matrix4x4 matrix)
		{
			transform.localScale = matrix.GetScale();
			transform.localRotation = matrix.GetRotation();
			transform.localPosition = matrix.GetPosition();
		}

		public static Matrix4x4 GetGlobalTRS(this Transform transform)
		{
			return Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		}

		public static Matrix4x4 GetLocalTRS(this Transform transform)
		{
			return Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
		}

		public static Quaternion GetRotation(this Matrix4x4 m)
		{
			return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
		}

		public static Vector3 GetPosition(this Matrix4x4 matrix)
		{
			var x = matrix.m03;
			var y = matrix.m13;
			var z = matrix.m23;

			return new Vector3(x, y, z);
		}

		public static Vector3 GetScale(this Matrix4x4 m)
		{
			return new Vector3(m.GetColumn(0).magnitude,
								m.GetColumn(1).magnitude,
								m.GetColumn(2).magnitude);
		}

		public static void SetLayerRecursive(this Transform t, int layer)
		{
			t.gameObject.layer = layer;
			foreach (Transform child in t)
			{
				child.SetLayerRecursive(layer);
			}
		}

		public static void Reset(this Transform t, bool noScale = false)
		{
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			if(!noScale)
				t.localScale = Vector3.one;
		}
	}
}
