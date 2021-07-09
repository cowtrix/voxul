using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[CreateAssetMenu]
	public class FaceMerger : VoxelOptimiserBase
	{
		static VoxelFace MergeFaces((VoxelFaceCoordinate, VoxelFace) a, (VoxelFaceCoordinate, VoxelFace) b)
		{
			return null;
		}

		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			return;
			var toUpdate = new Dictionary<VoxelFaceCoordinate, VoxelFace>();
			foreach (var face in data.Faces
				.Where(f => !toUpdate.ContainsKey(f.Key)))
			{
				var faceCoord = face.Key;
				var faceSurf = face.Value;

				var faceOppDir = face.Key.Direction.FlipDirection();

				foreach (var dir in VoxelExtensions.Directions)
				{
					if (dir == faceCoord.Direction || dir == faceOppDir)
					{
						continue;
					}
					var neighbourOffset = VoxelCoordinate.DirectionToCoordinate(dir, faceCoord.Layer)
						.ToVector3();
					var neighbourCoord = new VoxelFaceCoordinate
					{
						Offset = faceCoord.Offset + neighbourOffset,
						Direction = faceCoord.Direction,
					};

					if(!data.Faces.TryGetValue(neighbourCoord, out var neighbourFace)
						|| toUpdate.ContainsKey(neighbourCoord)
						|| neighbourFace.Surface != faceSurf.Surface)
					{
						// No valid neighbour to be merged, we're done!
						DebugHelper.DrawPoint(neighbourCoord.Offset, .2f, Color.gray, 2);
						continue;
					}

					// Merge these two faces
					DebugHelper.DrawPoint(neighbourCoord.Offset, .2f, Color.green, 2);
					toUpdate[neighbourCoord] = MergeFaces((faceCoord, faceSurf), (neighbourCoord, neighbourFace));
				}
			}
			foreach(var face in toUpdate)
			{
				data.Faces[face.Key] = face.Value;
			}
			voxulLogger.Debug($"InternalFaceOptimiser removed {toUpdate.Count} faces");
		}
	}
}