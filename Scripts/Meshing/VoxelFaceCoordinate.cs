
using System;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public class VoxelFace
	{
		public SurfaceData Surface;
		public EMaterialMode MaterialMode;
		public ERenderMode RenderMode;
	}

	[Serializable]
	public struct VoxelFaceCoordinate
	{
		public sbyte Layer;
		public Vector2 Size;
		public Vector3 Offset;
		public EVoxelDirection Direction;

		public override bool Equals(object obj)
		{
			return obj is VoxelFaceCoordinate coordinate &&
				   Layer == coordinate.Layer &&
				   Size.Equals(coordinate.Size) &&
				   Offset.Approximately(coordinate.Offset) &&
				   Direction == coordinate.Direction;
		}

		public override int GetHashCode()
		{
			int hashCode = -19254490;
			hashCode = hashCode * -1521134295 + Layer.GetHashCode();
			hashCode = hashCode * -1521134295 + Size.GetHashCode();
			hashCode = hashCode * -1521134295 + Offset.Round().GetHashCode();
			hashCode = hashCode * -1521134295 + Direction.GetHashCode();
			return hashCode;
		}

		public override string ToString() => $"{Offset}::{Size}::{Direction}";

		public static bool operator ==(VoxelFaceCoordinate left, VoxelFaceCoordinate right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(VoxelFaceCoordinate left, VoxelFaceCoordinate right)
		{
			return !(left == right);
		}

		public Plane ToPlane() => new Plane(Offset, VoxelCoordinate.DirectionToVector3(Direction));
	}
}