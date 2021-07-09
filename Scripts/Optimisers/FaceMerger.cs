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
			return a.Item2;
		}

		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			var mergeCount = 0;
			var keyCopy = data.Faces.Keys.ToList();
			while (true)
			{
				bool foundOptimisation = false;
				foreach (var faceCoord in keyCopy)
				{
					if (faceCoord.Offset != Vector3.zero)
					{
						return;	// REMOVE ME
					}
					if(!data.Faces.TryGetValue(faceCoord, out var faceSurf))
					{
						continue;
					}
					var faceOppDir = faceCoord.Direction.FlipDirection();
					foreach (var dir in VoxelExtensions.Directions)
					{
						if (dir == faceCoord.Direction || dir == faceOppDir)
						{
							continue;
						}
						var neighbourOffset = faceCoord.Offset + VoxelCoordinate.DirectionToCoordinate(dir, faceCoord.Layer)
							.ToVector3();

						var neighbour = data.Faces.SingleOrDefault(s => s.Key.Offset == neighbourOffset && s.Key.Direction == faceCoord.Direction);
						if(neighbour.Value == null)
						{
							continue;
						}

						// Merge these two faces
						DebugHelper.DrawPoint(neighbour.Key.Offset, .2f, Color.green, 2);
						data.Faces.Remove(neighbour.Key);
						data.Faces[faceCoord] = MergeFaces((faceCoord, faceSurf), (neighbour.Key, neighbour.Value));
						mergeCount++;
						foundOptimisation = true;
						break;
					}
				}
				if (!foundOptimisation)
				{
					break;
				}
			}
			
			voxulLogger.Debug($"InternalFaceOptimiser removed {mergeCount} faces");
		}
	}
}