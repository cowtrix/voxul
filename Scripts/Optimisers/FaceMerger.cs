using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;
using System;

namespace Voxul.Meshing
{
	public class FaceMerger : VoxelOptimiserBase
	{
		private static Vector2 SwizzleForDir(Vector3 vec, EVoxelDirection dir, out float discard)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
				case EVoxelDirection.XPos:
					discard = vec.x;
					return new Vector2(vec.y, vec.z);
				case EVoxelDirection.YNeg:
				case EVoxelDirection.YPos:
					discard = vec.y;
					return new Vector2(vec.x, vec.z);
				case EVoxelDirection.ZNeg:
				case EVoxelDirection.ZPos:
					discard = vec.z;
					return new Vector2(vec.x, vec.y);
				default:
					throw new ArgumentException($"Invalid direction {dir}");
			}
		}

		private static Vector3 ReverseSwizzleForDir(Vector2 vec, float input, EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
				case EVoxelDirection.XPos:
					return new Vector3(input, vec.y, vec.y);
				case EVoxelDirection.YNeg:
				case EVoxelDirection.YPos:
					return new Vector3(vec.x, input, vec.y);
				case EVoxelDirection.ZNeg:
				case EVoxelDirection.ZPos:
					return new Vector3(vec.x, vec.y, input);
				default:
					throw new ArgumentException($"Invalid direction {dir}");
			}
		}

		static (VoxelFaceCoordinate, VoxelFace) MergeFaces((VoxelFaceCoordinate, VoxelFace) a, (VoxelFaceCoordinate, VoxelFace) b)
		{
			var dir = a.Item1.Direction;

			var faceCoordA = a.Item1;
			var faceCoordB = b.Item1;

			var rectA = new Rect(SwizzleForDir(faceCoordA.Offset, dir, out var discardA), faceCoordA.Size);
			var rectB = new Rect(SwizzleForDir(faceCoordB.Offset, dir, out var discardB), faceCoordB.Size);

			var combinedRect = Rect.MinMaxRect(
				Mathf.Min(rectA.xMin, rectB.xMin),
				Mathf.Min(rectA.yMin, rectB.yMin),
				Mathf.Max(rectA.xMax, rectB.xMax),
				Mathf.Max(rectA.yMax, rectB.yMax));

			return (new VoxelFaceCoordinate
			{
				Direction = a.Item1.Direction,
				Offset = ReverseSwizzleForDir(combinedRect.center, discardA, dir),
				Layer = a.Item1.Layer,
				Size = combinedRect.size,
			}, a.Item2);
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
					open.RemoveAt(i);    // Remove this face from open list
					if (!data.Faces.TryGetValue(faceCoord, out var faceSurf))
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
						if(neighbour.Value == null || neighbour.Value == faceSurf)
						{
							continue;
						}

						// Merge these two faces
						DebugHelper.DrawPoint(neighbour.Key.Offset, .2f, Color.green, 2);
						data.Faces.Remove(neighbour.Key);
						data.Faces.Remove(faceCoord);

						var newFace = MergeFaces((faceCoord, faceSurf), (neighbour.Key, neighbour.Value));
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
			
			voxulLogger.Debug($"InternalFaceOptimiser removed {mergeCount} faces");
		}
	}
}