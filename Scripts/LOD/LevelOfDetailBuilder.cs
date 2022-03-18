using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
	public class VoxelMaterialTree : VoxelTree<VoxelMaterial>
	{
		public VoxelMaterialTree(sbyte maxLayer) : base(maxLayer)
		{
		}

		public VoxelMaterialTree(sbyte maxLayer, IDictionary<VoxelCoordinate, VoxelMaterial> data) : base(maxLayer, data)
		{
		}

		protected override VoxelMaterial GetAverage(IEnumerable<VoxelMaterial> vals, float minMaterialDistance)
		{
			return vals.Average(minMaterialDistance);
		}
	}

	public static class LevelOfDetailBuilder
	{
		public static IEnumerable<Voxel> RetargetToLayer(IEnumerable<Voxel> data, sbyte layer, float fillReq = .5f, float minMaterialDistance = .25f)
		{
			var bounds = data.GetBounds();
			var step = VoxelCoordinate.LayerToScale(layer);
			var tree = new VoxelMaterialTree(layer, data.ToDictionary(kvp => kvp.Coordinate, kvp => kvp.Material));

			foreach (var coord in tree.IterateLayer(layer, fillReq, minMaterialDistance))
			{
				yield return new Voxel
				{
					Coordinate = coord.Item1,
					Material = coord.Item2,
				};
			}
		}

		public static IEnumerable<Voxel> MergeMaterials(IDictionary<VoxelCoordinate, Voxel> data, float minMaterialMergeDistance)
		{
			const int maxIterations = 20;
			int iterationCounter = 0;
			bool improvementFound;
			do
			{
				improvementFound = false;
				UnityEngine.Random.InitState(data.GetHashCode() * iterationCounter);
				foreach (var vox in data.Values.OrderBy(v => UnityEngine.Random.value).ToList())
				{
					var material = vox.Material.Copy();
					foreach (var neighbourCoord in vox.Coordinate.GetNeighbours())
					{
						if (!data.TryGetValue(neighbourCoord, out var neighbourVox))
						{
							continue;
						}
						foreach (var neighbourSurface in neighbourVox.Material.GetSurfaces())
						{
							var thisSurface = material.GetSurface(neighbourSurface.Item1);
							var otherDir = neighbourSurface.Item1;
							var otherSurface = neighbourSurface.Item2;
							var distance = VoxelExtensions.DistanceBetweenSurfaces(thisSurface, otherSurface);
							if (distance < minMaterialMergeDistance && distance > .001f)
							{
								var existingIndex = material.Overrides.FindIndex(s => s.Direction == neighbourSurface.Item1);
								var directionOverride = new DirectionOverride { Direction = neighbourSurface.Item1, Surface = otherSurface };
								if (existingIndex >= 0)
								{
									material.Overrides[existingIndex] = directionOverride;
								}
								else
								{
									material.Overrides.Add(directionOverride);
								}
								improvementFound = true;
							}
						}
					}
					data[vox.Coordinate] = new Voxel(vox.Coordinate, material);
				}
				iterationCounter++;
			}
			while (improvementFound && iterationCounter < maxIterations);
			return data.Values;
		}
	}
}
