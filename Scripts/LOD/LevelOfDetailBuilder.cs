using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.LevelOfDetail
{

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

		public static IEnumerable<Voxel> Cast(IEnumerable<Voxel> data, sbyte layer, float fillReq = .5f, float minMaterialDistance = .25f)
		{
			var bounds = data.GetBounds();
			var step = VoxelCoordinate.LayerToScale(layer);
			var lookup = data.ToDictionary(v => v.Coordinate, v => v);
			foreach (var dir in VoxelExtensions.Directions)
			{
				var min = bounds.min.SwizzleForDir(dir, out _);
				var max = bounds.max.SwizzleForDir(dir, out _);
				bounds.size.SwizzleForDir(dir, out var depthSize);
				var depthVector = VoxelCoordinate.DirectionToVector3(dir);

				for (var x = min.x; x < max.x; x += step)
				{
					for (var y = min.y; y < max.y; y += step)
					{
						// Cast in direction to get voxel at point
						var point = new Vector2(x, y).ReverseSwizzleForDir(depthSize, dir);
						var ray = new Ray(point, -depthVector.normalized);
						if(VoxelExtensions.RaycastLocal(lookup.Keys, ray, depthSize * 2, out var hit))
						{
							yield return lookup[hit];
						}
					}
				}
			}
		}

		public static IEnumerable<Voxel> StripVoxels(IEnumerable<Voxel> data)
		{
			foreach(var v in data)
			{
				var mat = v.Material.Copy();
				if(mat.MaterialMode == EMaterialMode.Transparent)
				{
					mat.MaterialMode = EMaterialMode.Opaque;
				}
				yield return new Voxel(v.Coordinate, mat);
			}
		}

		public static IEnumerable<Voxel> MergeMaterials(IEnumerable<Voxel> voxels, float minMaterialMergeDistance)
		{
			var data = new Dictionary<VoxelCoordinate, Voxel>();
			foreach(var v in voxels)
			{
				data[v.Coordinate] = v;
			}

			const int maxIterations = 20;
			int iterationCounter = 0;
			bool improvementFound;
			do
			{
				improvementFound = false;
				var rnd = new System.Random(data.GetHashCode() * iterationCounter);
				foreach (var vox in data.Values.OrderBy(v => rnd.Next()).ToList())
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
								if(material.Overrides == null)
                                {
									material.Overrides = new List<DirectionOverride>();
                                }
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
