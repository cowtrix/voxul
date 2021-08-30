
using System;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public class VoxelFace
	{
		public SurfaceData Surface;
		public EMaterialMode MaterialMode;
		public ERenderMode RenderMode;
		public ENormalMode NormalMode;

		public override bool Equals(object obj)
		{
			return obj is VoxelFace face &&
				   EqualityComparer<SurfaceData>.Default.Equals(Surface, face.Surface) &&
				   MaterialMode == face.MaterialMode &&
				   RenderMode == face.RenderMode &&
				   NormalMode == face.NormalMode;
		}

		public override int GetHashCode()
		{
			int hashCode = 575944098;
			hashCode = hashCode * -1521134295 + Surface.GetHashCode();
			hashCode = hashCode * -1521134295 + MaterialMode.GetHashCode();
			hashCode = hashCode * -1521134295 + RenderMode.GetHashCode();
			hashCode = hashCode * -1521134295 + NormalMode.GetHashCode();
			return hashCode;
		}
	}

	[Serializable]
	public struct VoxelFaceCoordinate
	{
		public sbyte Layer;
		public Vector2Int Min, Max;
		public int Depth;
		public EVoxelDirection Direction;
		public float Offset;

		public int Width => Max.x - Min.x;
		public int Height => Max.y - Min.y;

		public bool Contains(Vector2Int position)
		{
			return position.x >= Min.x && position.x <= Max.x && position.y >= Min.y && position.y <= Max.y;
		}

		public override bool Equals(object obj)
		{
			return obj is VoxelFaceCoordinate coordinate &&
				   Layer == coordinate.Layer &&
				   Min.Equals(coordinate.Min) &&
				   Max.Equals(coordinate.Max) &&
				   Depth == coordinate.Depth &&
				   Direction == coordinate.Direction &&
				   Offset == coordinate.Offset;
		}

		public override int GetHashCode()
		{
			int hashCode = -1938225720;
			hashCode = hashCode * -1521134295 + Layer.GetHashCode();
			hashCode = hashCode * -1521134295 + Min.GetHashCode();
			hashCode = hashCode * -1521134295 + Max.GetHashCode();
			hashCode = hashCode * -1521134295 + Depth.GetHashCode();
			hashCode = hashCode * -1521134295 + Direction.GetHashCode();
			hashCode = hashCode * -1521134295 + Offset.GetHashCode();
			return hashCode;
		}

		public override string ToString() => $"{Min}-{Max}::{Layer}::{Direction}::{Offset}";
	}
}