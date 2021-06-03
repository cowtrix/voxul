using System;
using System.Collections.Generic;

[Serializable]
public struct Voxel
{
	public VoxelMaterial Material;
	public VoxelCoordinate Coordinate;

	public Voxel(VoxelCoordinate coord, VoxelMaterial surfaces)
	{
		Coordinate = coord;
		Material = surfaces;
	}
}
