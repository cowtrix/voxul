using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[CreateAssetMenu]
	public class InternalFaceOptimiser : VoxelOptimiserBase
	{
		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			var toRemove = new HashSet<VoxelFaceCoordinate>();
			foreach(var face in data.Faces.Where(f => !toRemove.Contains(f.Key)))
			{
				var faceCoord = face.Key;
				var faceSurf = face.Value;
				
				var offset = VoxelCoordinate.DirectionToCoordinate(faceCoord.Direction, faceCoord.Layer)
					.ToVector3();
				var inverse = new VoxelFaceCoordinate
				{
					Offset = face.Key.Offset,
					Direction = face.Key.Direction.FlipDirection(),
					Size = face.Key.Size,
					Layer = face.Key.Layer,
				};
				if (data.Faces.TryGetValue(inverse, out var neighbour))
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