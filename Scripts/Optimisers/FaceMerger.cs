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

			var mergeCoord = new VoxelFaceCoordinate
			{
				Offset = (coordA.Offset + coordB.Offset) / 2,
				Depth = (coordA.Depth + coordB.Depth) / 2,
				Direction = coordA.Direction,
				Layer = coordA.Layer,
				Min = new Vector2Int(Mathf.Min(coordA.Min.x, coordB.Min.x, coordA.Max.x, coordB.Max.x), Mathf.Min(coordA.Min.y, coordB.Min.y, coordA.Max.y, coordB.Max.y)),
				Max = new Vector2Int(Mathf.Max(coordA.Min.x, coordB.Min.x, coordA.Max.x, coordB.Max.x), Mathf.Max(coordA.Min.y, coordB.Min.y, coordA.Max.y, coordB.Max.y)),
			};

			//Debug.Log($"Merginge coord A: {coordA} and B: {coordB} == {mergeCoord}");

			return (mergeCoord, faceA);
		}

		public override void OnPreFaceStep(IntermediateVoxelMeshData data)
		{
			int OptimiseForDirection(List<KeyValuePair<VoxelFaceCoordinate, VoxelFace>> allVoxels)
			{
				var count = 0;
				var open = new List<KeyValuePair<VoxelFaceCoordinate, VoxelFace>>(allVoxels);
				var offsetLists = new Dictionary<(sbyte, int), List<(VoxelFaceCoordinate, VoxelFace)>>();
				foreach(var kvp in open)
				{
					var keyTuple = (kvp.Key.Layer, kvp.Key.Depth);
					if (!offsetLists.TryGetValue(keyTuple, out var list))
					{
						list = new List<(VoxelFaceCoordinate, VoxelFace)>();
						offsetLists.Add(keyTuple, list);
					}
					list.Add((kvp.Key, kvp.Value));
				}

				while (open.Any())
				{
					bool foundOptimisation = false;
					for (int i = open.Count - 1; i >= 0; i--)
					{
						var faceCoord = open[i].Key;

						var offsetListKey = (faceCoord.Layer, faceCoord.Depth);
						var offsetList = offsetLists[offsetListKey];

						open.RemoveAt(i);    // Remove this face from open list

						if(!data.Faces.TryGetValue(faceCoord, out var faceSurf))
						{
							continue;
						}

						var minVoxelCoord = new VoxelCoordinate(faceCoord.Min.ReverseSwizzleForDir(faceCoord.Depth, faceCoord.Direction), faceCoord.Layer);
						var maxVoxelCoord = new VoxelCoordinate(faceCoord.Max.ReverseSwizzleForDir(faceCoord.Depth, faceCoord.Direction), faceCoord.Layer);
						
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
							var contains = offsetList.Where(f =>
									f.Item1.Direction == faceCoord.Direction &&
									f.Item1.Offset == faceCoord.Offset &&
									f.Item1.Layer == faceCoord.Layer &&
									f.Item1.Depth == faceCoord.Depth &&
									f.Item1.Contains(neighbourPlanePosition)).FirstOrDefault();

							if (contains.Item2 == null || System.Object.ReferenceEquals(faceSurf, contains.Item2) || !contains.Item2.Equals(faceSurf))
							{
								continue;
							}
							var neighbourCoord = contains.Item1;
							var neigbourSurf = contains.Item2;

							// We need to check if this coordinate is valid - will expanding into it only increase one size parameter?
							{
								var checkMin = new Vector2Int(Mathf.Min(faceCoord.Min.x, neighbourCoord.Min.x, faceCoord.Max.x, neighbourCoord.Max.x), Mathf.Min(faceCoord.Min.y, neighbourCoord.Min.y, faceCoord.Max.y, neighbourCoord.Max.y));
								var checkMax = new Vector2Int(Mathf.Max(faceCoord.Min.x, neighbourCoord.Min.x, faceCoord.Max.x, neighbourCoord.Max.x), Mathf.Max(faceCoord.Min.y, neighbourCoord.Min.y, faceCoord.Max.y, neighbourCoord.Max.y));

								var newWidth = checkMax.x - checkMin.x;
								var newHeight = checkMax.y - checkMin.y;

								//Debug.LogWarning(checkMin);
								if ((newWidth != faceCoord.Width && newHeight != faceCoord.Height) || (newWidth != neighbourCoord.Width && newHeight != neighbourCoord.Height))
								{
									continue;
								}
							}

							data.Faces.Remove(neighbourCoord);
							data.Faces.Remove(faceCoord);

							offsetList.Remove(contains);
							offsetList.Remove((faceCoord, faceSurf));

							var newFace = MergeFaces((faceCoord, faceSurf), contains);

							data.Faces[newFace.Item1] = newFace.Item2;
							open.Add(new KeyValuePair<VoxelFaceCoordinate, VoxelFace>(newFace.Item1, newFace.Item2));
							offsetList.Add(newFace);

							count++;
							foundOptimisation = true;
							break;
						}
					}
					if (!foundOptimisation)
					{
						break;
					}
				}
				return count;
			}

			var mergeCount = data.Faces.GroupBy(f => f.Key.Direction)
				.Select(s => OptimiseForDirection(s.ToList()))
				//.AsParallel()
				.Sum();

			voxulLogger.Debug($"FaceMerger merged {mergeCount} faces");
		}
	}
}