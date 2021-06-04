
using System;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[Serializable]
	public class TriangleVoxelMapping : SerializableDictionary<int, TriangleVoxelMapping.InnerMapping>
	{

		[Serializable]
		public class InnerMapping : SerializableDictionary<int, VoxelCoordinateTriangleMapping> { }
	}
}