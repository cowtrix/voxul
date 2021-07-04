
using System;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[Serializable]
	public struct VoxelCoordinateTriangleMapping
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
		public class InnerMapping : SerializableDictionary<int, VoxelCoordinateTriangleMapping> { }
	}
}