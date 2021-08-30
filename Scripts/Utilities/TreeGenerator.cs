using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	public class TreeGenerator : DynamicVoxelGenerator
	{
		public enum EGenerationMode
		{
			Trunk,
			Leaf,
			Root,
		}

		[Range(0, 1)]
		public float Gravity = .75f;
		public AnimationCurve Growth = AnimationCurve.Linear(1, 1, 1, 1);
		[Range(0, 1)]
		public float BranchProbability = .1f;
		[Range(0, 1)]
		public float LeafProbability = .1f;

		public int MaxVoxels = 1000;

		public VoxelBrush BranchMaterial, LeafMaterial;

		protected override void SetVoxels(VoxelRenderer renderer)
		{
			var gravDir = EVoxelDirection.YPos;
			var branchDirs = new[] { EVoxelDirection.XNeg, EVoxelDirection.XPos, EVoxelDirection.ZNeg, EVoxelDirection.ZPos };
			var leafDirs = new[] { EVoxelDirection.YNeg,
				EVoxelDirection.YPos, EVoxelDirection.XNeg, EVoxelDirection.XPos, EVoxelDirection.ZNeg, EVoxelDirection.ZPos,
				EVoxelDirection.YPos, EVoxelDirection.XNeg, EVoxelDirection.XPos, EVoxelDirection.ZNeg, EVoxelDirection.ZPos };

			var branches = new Queue<(EGenerationMode, VoxelCoordinate)>();
			branches.Enqueue(default);
			while (branches.Any() && renderer.Mesh.Voxels.Count < MaxVoxels)
			{
				var current = branches.Dequeue();

				switch (current.Item1)
				{
					case EGenerationMode.Trunk:
						{
							// Roll branch
							if (Random.value < BranchProbability)
							{
								var branchPos = current.Item2 + VoxelCoordinate.DirectionToCoordinate(branchDirs.Random(), current.Item2.Layer);
								if (Random.value < LeafProbability)
								{
									branches.Enqueue((EGenerationMode.Leaf, branchPos));
								}
								else
								{
									branches.Enqueue((EGenerationMode.Trunk, branchPos));
								}
							}
						}
						{
							// Grow branch
							var nextDir = Random.value > Gravity ? gravDir : branchDirs.Random();
							if (Random.value > Growth.Evaluate(renderer.Mesh.Voxels.Count / (float)MaxVoxels))
							{
								continue;
							}
							var nextCoord = current.Item2 + VoxelCoordinate.DirectionToCoordinate(nextDir, current.Item2.Layer);
							branches.Enqueue((EGenerationMode.Trunk, nextCoord));
						}
						renderer.Mesh.Voxels.AddSafe(new Voxel(current.Item2, BranchMaterial.Generate(Random.value)));
						break;
					case EGenerationMode.Leaf:
						foreach (var dir in leafDirs)
						{
							if (Random.value > LeafProbability)
							{
								continue;
							}
							branches.Enqueue((EGenerationMode.Leaf, current.Item2 + VoxelCoordinate.DirectionToCoordinate(dir, current.Item2.Layer)));
						}
						renderer.Mesh.Voxels.AddSafe(new Voxel(current.Item2, LeafMaterial.Generate(Random.value)));
						break;
				}

			}
		}
	}
}
