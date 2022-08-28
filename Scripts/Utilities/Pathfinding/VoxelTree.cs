using System;
using System.Collections.Generic;
using Voxul.Meshing;

namespace Voxul.Utilities
{
    public class VoxelTree : VoxelCoordinateTree<Voxel>
    {
        public VoxelTree(sbyte maxLayer) : base(maxLayer)
        {
        }

        public VoxelTree(sbyte maxLayer, IDictionary<VoxelCoordinate, Voxel> data) : base(maxLayer, data)
        {
        }

		protected override Voxel GetAverage(IEnumerable<Voxel> vals, float minMaterialDistance)
        {
			throw new NotImplementedException();
		}
    }
}
