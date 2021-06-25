
using System;

namespace Voxul.Meshing
{
	[Serializable]
	public struct VoxelCoordinateTriangleMapping
	{
		public VoxelCoordinate Coordinate;
		public EVoxelDirection Direction;
	}
}