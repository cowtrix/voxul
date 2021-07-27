using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;
using System;

namespace Voxul.Meshing
{
	public class FaceMerger : VoxelOptimiserBase
	{
		static (VoxelFaceCoordinate, VoxelFace) MergeFaces((VoxelFaceCoordinate, VoxelFace) a, (VoxelFaceCoordinate, VoxelFace) b)
		{
			var coordA = a.Item1;
			var faceA = a.Item2;

			var coordB = b.Item1;
			var faceB = b.Item2;

			var mergeCoord = new VoxelFaceCoordinate
			{
				Offset = (coordA.Offset + coordB.Offset) / 2,
				Depth = (coordA.Depth + coordB.Depth) / 2,
				Direction = coordA.Direction,
				Layer = coordA.Layer,
				Min = new Vector2Int(Mathf.Min(coordA.Min.x, coordB.Min.x, coordA.Max.x, coordB.Max.x), Mathf.Min(coordA.Min.y, coordB.Min.y, coordA.Max.y, coordB.Max.y)),
				Max = new Vector2Int(Mathf.Max(coordA.Min.x, coordB.Min.x, coordA.Max.x, coordB.Max.x), Mathf.Max(coordA.Min.y, coordB.Min.y, coordA.Max.y, coordB.Max.y)),
			};

			Debug.Log($"Merginge coord A: {coordA} and B: {coordB} == {mergeCoord}");

			return (mergeCoord, faceA);
		}

		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			var mergeCount = 0;
			while (true)
			{
				var open = data.Faces.Keys.ToList();
				bool foundOptimisation = false;
				for (int i = open.Count - 1; i >= 0; i--)
				{
					VoxelFaceCoordinate faceCoord = open[i];
					var minVoxelCoord = new VoxelCoordinate(faceCoord.Min.ReverseSwizzleForDir(faceCoord.Depth, faceCoord.Direction), faceCoord.Layer);
					var maxVoxelCoord = new VoxelCoordinate(faceCoord.Max.ReverseSwizzleForDir(faceCoord.Depth, faceCoord.Direction), faceCoord.Layer);
					open.RemoveAt(i);    // Remove this face from open list
					if (!data.Faces.TryGetValue(faceCoord, out var faceSurf))
					{
						continue;
					}
					var faceOppDir = faceCoord.Direction.FlipDirection();

					foreach (var mergeDirection in VoxelExtensions.Directions
						.Where(d => d != faceCoord.Direction && d != faceOppDir))
					{
						var mergeDirectionVector = VoxelCoordinate.DirectionToVector3(mergeDirection);
						var mergeDirectionScaled = (mergeDirectionVector + Vector3.one) / 2f;
						var closestPointOnFace = new Vector3Int(
							(int)Mathf.Lerp(minVoxelCoord.X, maxVoxelCoord.X, mergeDirectionScaled.x),
							(int)Mathf.Lerp(minVoxelCoord.Y, maxVoxelCoord.Y, mergeDirectionScaled.y),
							(int)Mathf.Lerp(minVoxelCoord.Z, maxVoxelCoord.Z, mergeDirectionScaled.z));

						var neighbourCoord = (closestPointOnFace + mergeDirectionVector)
							.SwizzleForDir(faceCoord.Direction, out _)
							.RoundToVector2Int();
						var neighbourFaceCoord = new VoxelFaceCoordinate
						{
							Depth = faceCoord.Depth,
							Direction = faceCoord.Direction,
							Layer = faceCoord.Layer,
							Offset = faceCoord.Offset,
							Max = neighbourCoord,
							Min = neighbourCoord,
						};
						if(!data.Faces.TryGetValue(neighbourFaceCoord, out var neighbourFace)
							|| !neighbourFace.Equals(faceSurf))
						{
							continue;
						}

						data.Faces.Remove(neighbourFaceCoord);
						data.Faces.Remove(faceCoord);

						var newFace = MergeFaces((faceCoord, faceSurf), (neighbourFaceCoord, neighbourFace));

						data.Faces[newFace.Item1] = newFace.Item2;

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
			voxulLogger.Debug($"FaceMerger merged {mergeCount} faces");
		}
	}
}