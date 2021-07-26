using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public class InternalFaceOptimiser : VoxelOptimiserBase
	{
		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			var toRemove = new HashSet<VoxelFaceCoordinate>();

			var yFacesDebug = data.Faces.Where(f => f.Key.Direction.ToString().Contains("Y")).ToList();

			foreach (var face in data.Faces.Where(f => !toRemove.Contains(f.Key)))
			{
				var faceCoord = face.Key;
				var faceSurf = face.Value;

				if (faceSurf.RenderMode != ERenderMode.Block)
				{
					continue;
				}

				var inverse = new VoxelFaceCoordinate
				{
					Offset = face.Key.Offset,
					Direction = face.Key.Direction.FlipDirection(),
					Size = face.Key.Size,
					Layer = face.Key.Layer,
				};

				if (!data.Faces.TryGetValue(inverse, out var neighbour))
				{
					continue;
				}
				if (neighbour.RenderMode == ERenderMode.Block
					&& neighbour.MaterialMode == face.Value.MaterialMode)
				{
					toRemove.Add(faceCoord);
					toRemove.Add(inverse);
					continue;
				}
			}
			foreach (var coord in toRemove)
			{
				data.Faces.Remove(coord);
			}
			voxulLogger.Debug($"InternalFaceOptimiser removed {toRemove.Count} faces");
		}
	}
}