using System.Collections.Generic;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
    public class VoxelMaterialTree : VoxelCoordinateTree<VoxelMaterial>
	{
		public VoxelMaterialTree(sbyte maxLayer) : base(maxLayer)
		{
		}

		public VoxelMaterialTree(sbyte maxLayer, IDictionary<VoxelCoordinate, VoxelMaterial> data) : base(maxLayer, data)
		{
		}

		protected override VoxelMaterial GetAverage(IEnumerable<VoxelMaterial> vals, float minMaterialDistance)
		{
			return vals.Average(minMaterialDistance);
		}
	}
}
