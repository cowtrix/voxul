using System;
using System.Collections.Generic;

namespace Voxul
{
	[Serializable]
	public struct Voxel
	{
		/// <summary>
		/// Defines how the voxel is rendered.
		/// </summary>
		public VoxelMaterial Material;
		/// <summary>
		/// Where the voxel is in local 3D voxel-space.
		/// </summary>
		public VoxelCoordinate Coordinate;

		public Voxel(VoxelCoordinate coord, VoxelMaterial surfaces)
		{
			Coordinate = coord;
			Material = surfaces;
		}
	}
}