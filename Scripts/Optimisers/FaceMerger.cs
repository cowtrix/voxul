using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;
using System;

namespace Voxul.Meshing
{
	[Serializable]
	public class FaceMerger : VoxelOptimiserBase
	{
		[SerializeField]
		public float Test;

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

						var neighbourPlanePosition = (closestPointOnFace + mergeDirectionVector)
							.SwizzleForDir(faceCoord.Direction, out _)
							.RoundToVector2Int();
						var neighbourVoxCoord = new VoxelCoordinate(neighbourPlanePosition.ReverseSwizzleForDir(faceCoord.Depth, faceCoord.Direction), faceCoord.Layer);
						if (!data.Voxels.TryGetValue(neighbourVoxCoord, out var neighbourVoxel))
						{
							continue;
						}

						// Find the first face coord that contains the plane position
						var contains = data.Faces.Where(f =>
								f.Key.Direction == faceCoord.Direction &&
								f.Key.Offset == faceCoord.Offset &&
								f.Key.Layer == faceCoord.Layer &&
								f.Key.Depth == faceCoord.Depth &&
								f.Key.Contains(neighbourPlanePosition)
							).FirstOrDefault();

						if (contains.Value == null || System.Object.ReferenceEquals(faceSurf, contains.Value) || !contains.Value.Equals(faceSurf))
						{
							continue;
						}

						// We need to check if this coordinate is valid - will expanding into it only increase one size parameter?
						{
							var checkMin = new Vector2Int(Mathf.Min(faceCoord.Min.x, contains.Key.Min.x, faceCoord.Max.x, contains.Key.Max.x), Mathf.Min(faceCoord.Min.y, contains.Key.Min.y, faceCoord.Max.y, contains.Key.Max.y));
							var checkMax = new Vector2Int(Mathf.Max(faceCoord.Min.x, contains.Key.Min.x, faceCoord.Max.x, contains.Key.Max.x), Mathf.Max(faceCoord.Min.y, contains.Key.Min.y, faceCoord.Max.y, contains.Key.Max.y));

							var newWidth = checkMax.x - checkMin.x;
							var newHeight = checkMax.y - checkMin.y;

							Debug.LogWarning(checkMin);
							if ((newWidth != faceCoord.Width && newHeight != faceCoord.Height) || (newWidth != contains.Key.Width && newHeight != contains.Key.Height))
							{
								continue;
							}
						}

						data.Faces.Remove(contains.Key);
						data.Faces.Remove(faceCoord);

						var newFace = MergeFaces((faceCoord, faceSurf), (contains.Key, contains.Value));

						data.Faces[newFace.Item1] = newFace.Item2;
						open.Add(newFace.Item1);

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