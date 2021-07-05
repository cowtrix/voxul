using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[CreateAssetMenu]
	public class DefaultFaceOptimiser : VoxelOptimiserBase
	{
		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			var toRemove = new HashSet<VoxelFaceCoordinate>();
			foreach(var face in data.Faces.Where(f => !toRemove.Contains(f.Key)))
			{
				var faceCoord = face.Key;
				var faceSurf = face.Value;

				var offset = VoxelCoordinate.DirectionToCoordinate(faceCoord.Direction, faceCoord.Coordinate.Layer);
				var inverse = new VoxelFaceCoordinate
				{
					Coordinate = face.Key.Coordinate + offset,
					Direction = face.Key.Direction.FlipDirection(),
				};
				if (data.Faces.TryGetValue(inverse, out var neighbour)
					&& neighbour.Offset == face.Value.Offset)
				{
					toRemove.Add(faceCoord);
					toRemove.Add(inverse);
					continue;
				}
			}
			foreach(var coord in toRemove)
			{
				data.Faces.Remove(coord);
			}
			voxulLogger.Debug($"InternalFaceOptimiser removed {toRemove.Count} faces");
		}
	}
}