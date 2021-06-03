using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Voxul
{
	[Serializable]
	public struct VoxelCoordinate
	{
		public const int LayerRatio = 3;
		public sbyte Layer;
		public int X, Y, Z;

		public VoxelCoordinate(int x, int y, int z, sbyte layer)
		{
			X = x;
			Y = y;
			Z = z;
			Layer = layer;
		}

		public static VoxelCoordinate DirectionToCoordinate(EVoxelDirection dir, sbyte layer)
		{
			switch (dir)
			{
				case EVoxelDirection.XPos:
					return new VoxelCoordinate(1, 0, 0, layer);
				case EVoxelDirection.XNeg:
					return new VoxelCoordinate(-1, 0, 0, layer);
				case EVoxelDirection.YPos:
					return new VoxelCoordinate(0, 1, 0, layer);
				case EVoxelDirection.YNeg:
					return new VoxelCoordinate(0, -1, 0, layer);
				case EVoxelDirection.ZPos:
					return new VoxelCoordinate(0, 0, 1, layer);
				case EVoxelDirection.ZNeg:
					return new VoxelCoordinate(0, 0, -1, layer);
			}
			throw new NotSupportedException($"{dir} not supported");
		}

		public static Quaternion DirectionToQuaternion(EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XPos:
					return Quaternion.LookRotation(Vector3.up, Vector3.right);
				case EVoxelDirection.XNeg:
					return Quaternion.LookRotation(Vector3.up, Vector3.left);
				case EVoxelDirection.YPos:
					return Quaternion.LookRotation(Vector3.right, Vector3.up);
				case EVoxelDirection.YNeg:
					return Quaternion.LookRotation(Vector3.right, Vector3.down);
				case EVoxelDirection.ZPos:
					return Quaternion.LookRotation(Vector3.up, Vector3.forward);
				case EVoxelDirection.ZNeg:
					return Quaternion.LookRotation(Vector3.up, Vector3.back);
			}
			throw new NotSupportedException($"{dir} not supported");
		}

		public static bool VectorToDirection(Vector3 hitNorm, out EVoxelDirection dir)
		{
			hitNorm = hitNorm.normalized;
			dir = EVoxelDirection.XNeg;
			bool success = false;
			if (hitNorm.x == 1)
			{
				dir = EVoxelDirection.XPos;
				success = true;
			}
			else if (hitNorm.x == -1)
			{
				dir = EVoxelDirection.XNeg;
				success = true;
			}
			if (hitNorm.y == 1)
			{
				dir = EVoxelDirection.YPos;
				success = true;
			}
			if (hitNorm.y == -1)
			{
				dir = EVoxelDirection.YNeg;
				success = true;
			}
			if (hitNorm.z == 1)
			{
				dir = EVoxelDirection.ZPos;
				success = true;
			}
			if (hitNorm.z == -1)
			{
				dir = EVoxelDirection.ZNeg;
				success = true;
			}
			return success;
		}

		public static float LayerToScale(int layer) => 1 / Mathf.Pow(LayerRatio, layer);

		public Vector3 ToVector3()
		{
			var scale = GetScale();
			return new Vector3(X, Y, Z) * scale;// + Vector3.one * .5f * scale;
		}

		public static VoxelCoordinate FromVector3(Vector3 point, sbyte layer)
		{
			var scale = LayerToScale(layer);
			point *= 1 / scale;
			//point -= Vector3.one * .5f * scale;
			return new VoxelCoordinate(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), Mathf.RoundToInt(point.z), layer);
		}

		public float GetScale() => LayerToScale(Layer);

		public Bounds ToBounds() => new Bounds(ToVector3(), GetScale() * Vector3.one);

		public override bool Equals(object obj)
		{
			return obj is VoxelCoordinate coordinate &&
				   Layer == coordinate.Layer &&
				   X == coordinate.X &&
				   Y == coordinate.Y &&
				   Z == coordinate.Z;
		}

		public override int GetHashCode()
		{
			int hashCode = 75725182;
			hashCode = hashCode * -1521134295 + Layer.GetHashCode();
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			hashCode = hashCode * -1521134295 + Z.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(VoxelCoordinate left, VoxelCoordinate right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(VoxelCoordinate left, VoxelCoordinate right)
		{
			return !(left == right);
		}

		public VoxelCoordinate ChangeLayer(sbyte newLayer)
		{
			if (newLayer == Layer)
			{
				return this;
			}
			var vec3 = ToVector3();
			return FromVector3(vec3, newLayer);
		}

		public static VoxelCoordinate operator +(VoxelCoordinate left, VoxelCoordinate right)
		{
			if (left.Layer != right.Layer)
			{
				throw new Exception("Layer mismatch");
			}
			return new VoxelCoordinate(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.Layer);
		}

		public static VoxelCoordinate operator -(VoxelCoordinate left, VoxelCoordinate right)
		{
			if (left.Layer != right.Layer)
			{
				throw new Exception("Layer mismatch");
			}
			return new VoxelCoordinate(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.Layer);
		}

		public override string ToString() => $"{{ {X}, {Y}, {Z} }} :: {Layer}";

		public IEnumerable<VoxelCoordinate> Subdivide()
		{
			var newLayer = (sbyte)(Layer + 1);
			var centerCoord = ChangeLayer(newLayer) - new VoxelCoordinate(0, 0, -1, newLayer);
			for (var x = -1; x <= 1; ++x)
			{
				for (var y = -1; y <= 1; ++y)
				{
					for (var z = -2; z <= 0; ++z)
					{
						var coord = centerCoord + new VoxelCoordinate(x, y, z, newLayer);
						yield return coord;
					}
				}
			}
		}
	}
}