using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
    public static class LevelOfDetailBuilder
    {
        

        public static IEnumerable<Voxel> RetargetToLayer(IEnumerable<Voxel> data, sbyte layer)
		{
            var bounds = data.GetBounds();
            var step = VoxelCoordinate.LayerToScale(layer);
            var tree = new VoxelTree<VoxelMaterial>(layer, data.ToDictionary(kvp => kvp.Coordinate, kvp => kvp.Material));
            for(var x = bounds.min.x + step/2f; x <= bounds.max.x - step/2f; x += step)
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
								if (!allChildren.Any() || allChildren.Count < ratio * ratio * ratio * .5f)
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
            }
		}

        
    }
}
