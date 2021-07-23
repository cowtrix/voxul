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
        [Serializable]
        public class RenderPlane
		{
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector2 Size;
		}

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
                            var allChildren = n.GetAllDescendants();
                            var mat = AverageMaterials(allChildren.Select(c => c.Item2));
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

        public static IEnumerable<SurfaceData> GetAllSurfacesWithDirection(this IEnumerable<VoxelMaterial> materials, EVoxelDirection dir)
		{
            foreach(var mat in materials)
			{
                yield return mat.GetSurface(dir);
			}
		}

        public static SurfaceData AverageSurfaces(this IEnumerable<SurfaceData> surfaces)
        {
            return new SurfaceData
            {
                Albedo = surfaces.Select(s => s.Albedo).AverageColor(),
                Metallic = surfaces.Average(s => s.Metallic),
                Smoothness = surfaces.Average(s => s.Smoothness),
                TextureFade = surfaces.Max(s => s.TextureFade),
                Texture = surfaces.GroupBy(s => s.Texture)
                    .OrderByDescending(g => g.Count())
                    .First().Key,
                UVMode = surfaces.GroupBy(s => s.UVMode)
                    .OrderByDescending(g => g.Count())
                    .First().Key,
            };
        }

        public static VoxelMaterial AverageMaterials(IEnumerable<VoxelMaterial> materials)
		{
            var mat = new VoxelMaterial
            {
                Overrides = new DirectionOverride[6],
                MaterialMode = EMaterialMode.Opaque,
                RenderMode = ERenderMode.Block,
            };
			for (int i = 0; i < VoxelExtensions.Directions.Length; i++)
			{
				EVoxelDirection dir = VoxelExtensions.Directions[i];
				var allSurfaces = materials.GetAllSurfacesWithDirection(dir);
                var averageSurface = allSurfaces.AverageSurfaces();
                mat.Overrides[i] = new DirectionOverride { Direction = dir, Surface = averageSurface };
			}
            return mat;
		}
    }
}
