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

		protected override VoxelMaterial GetAverage(IEnumerable<VoxelMaterial> vals)
		{
            return vals.Average();
		}
	}

	public static class LevelOfDetailBuilder
    {
        

        public static IEnumerable<Voxel> RetargetToLayer(IEnumerable<Voxel> data, sbyte layer, float fillReq = .5f)
		{
            var bounds = data.GetBounds();
            var step = VoxelCoordinate.LayerToScale(layer);
            var tree = new VoxelMaterialTree(layer, data.ToDictionary(kvp => kvp.Coordinate, kvp => kvp.Material));

            foreach(var coord in tree.IterateLayer(layer))
			{
                yield return new Voxel
                {
                    Coordinate = coord.Item1,
                    Material = coord.Item2,
                };
            }

            /*for(var x = bounds.min.x + step/2f; x <= bounds.max.x - step/2f; x += step)
			{
                for (var y = bounds.min.x + step / 2f; y <= bounds.max.x - step / 2f; y += step)
                {
                    for (var z = bounds.min.x + step / 2f; z <= bounds.max.x - step / 2f; z += step)
                    {
                        var stepPoint = new Vector3(x, y, z);
                        var stepCoord = VoxelCoordinate.FromVector3(stepPoint, layer);
                        if(tree.TryGetValue(stepCoord, out VoxelTree<VoxelMaterial>.Node n))
						{
                            if(n is VoxelTree<VoxelMaterial>.LeafNode leaf)
							{
                                yield return new Voxel
                                {
                                    Coordinate = stepCoord,
                                    Material = leaf.Value,
                                };
                            }
							else
							{
                                var allChildren = n.GetAllDescendants().ToList();
                                var ratio = VoxelCoordinate.LayerRatio;
								if (!allChildren.Any() || allChildren.Count < ratio * ratio * ratio * fillReq)
								{
                                    continue;
								}
                                var mat = allChildren.Select(c => c.Item2).Average();
                                yield return new Voxel
                                {
                                    Coordinate = stepCoord,
                                    Material = mat,
                                };
                            }
						}
                    }
                }
            }*/
		}

        
    }
}
