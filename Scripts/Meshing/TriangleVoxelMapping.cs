
using System;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public struct VoxelFace
	{
		public VoxelFaceCoordinate Coordinate;
		public SurfaceData Surface;
		public Vector2 Size;
		public float Offset;
		public EMaterialMode MaterialMode;
	}

	[Serializable]
	public struct VoxelFaceCoordinate
	{
		public VoxelCoordinate Coordinate;
		public EVoxelDirection Direction;
	}

	/// <summary>
	/// This data structure is used to get a voxel from a collider triangle hit index.
	/// </summary>
	[Serializable]
	public class TriangleVoxelMapping : SerializableDictionary<int, TriangleVoxelMapping.InnerMapping>
	{

		[Serializable]
		public class InnerMapping : SerializableDictionary<int, VoxelFaceCoordinate> { }
	}
}