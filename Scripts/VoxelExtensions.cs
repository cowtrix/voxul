using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	public static class VoxelExtensions
	{
		/// <summary>
		/// Stored for quick lookup.
		/// </summary>
		public static readonly EVoxelDirection[] Directions = Enum.GetValues(typeof(EVoxelDirection)).Cast<EVoxelDirection>().ToArray();

		public static bool PointIsOnVoxelGrid(this Vector3 point, sbyte layer)
		{
			var voxelCoordinate = VoxelCoordinate.FromVector3(point, layer);
			return voxelCoordinate.ToVector3() == point;
		}

		public static bool PointIsOnVoxelGrid(this Vector3 point)
		{
			for(var layer = VoxelCoordinate.MIN_LAYER; layer < VoxelCoordinate.MAX_LAYER; layer++)
			{
				if (point.PointIsOnVoxelGrid(layer))
				{
					return true;
				}
			}
			return false;
		}

		public static Vector3Int ToRawVector3Int(this VoxelCoordinate vec) => new Vector3Int(vec.X, vec.Y, vec.Z);

		public static Vector2 SwizzleForDir(this Vector3Int vec, EVoxelDirection dir, out float discard) =>
			SwizzleForDir(vec.ToVector3(), dir, out discard);

		public static Vector2 SwizzleForDir(this Vector3 vec, EVoxelDirection dir, out float discard)
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

		public static Vector3Int ReverseSwizzleForDir(this Vector2Int vec, float input, EVoxelDirection dir) =>
			ReverseSwizzleForDir((Vector2)vec, input, dir).RoundToVector3Int();

		public static Vector3 ReverseSwizzleForDir(this Vector2 vec, float input, EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
				case EVoxelDirection.XPos:
					return new Vector3(input, vec.x, vec.y);
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

		public static Vector3Int ReverseSwizzleForDir(this Vector3Int vec, float input, EVoxelDirection dir) => 
			ReverseSwizzleForDir((Vector3)vec, input, dir).RoundToVector3Int();

		public static Vector3 ReverseSwizzleForDir(this Vector3 vec, float input, EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
				case EVoxelDirection.XPos:
					return new Vector3(input, vec.y, vec.z);
				case EVoxelDirection.YNeg:
				case EVoxelDirection.YPos:
					return new Vector3(vec.x, input, vec.z);
				case EVoxelDirection.ZNeg:
				case EVoxelDirection.ZPos:
					return new Vector3(vec.x, vec.y, input);
				default:
					throw new ArgumentException($"Invalid direction {dir}");
			}
		}

		public static IEnumerable<SurfaceData> GetAllSurfacesWithDirection(this IEnumerable<VoxelMaterial> materials, EVoxelDirection dir)
		{
			foreach (var mat in materials)
			{
				yield return mat.GetSurface(dir);
			}
		}

		public static SurfaceData AverageSurfaces(this IEnumerable<SurfaceData> surfaces)
		{
			if(surfaces == null || !surfaces.Any())
			{
				return default;
			}
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

		public static VoxelMaterial Average(this IEnumerable<VoxelMaterial> materials, float minMaterialDistance)
		{
			var mat = new VoxelMaterial
			{
				Overrides = new List<DirectionOverride>(),
				MaterialMode = EMaterialMode.Opaque,
				RenderMode = ERenderMode.Block,
			};
			for (int i = 0; i < VoxelExtensions.Directions.Length; i++)
			{
				EVoxelDirection dir = VoxelExtensions.Directions[i];
				var allSurfaces = materials.GetAllSurfacesWithDirection(dir);
				var averageSurface = allSurfaces.AverageSurfaces();
				if (i == 0)
				{
					mat.Default = averageSurface;
				}
				else if(VoxelExtensions.DistanceBetweenSurfaces( mat.Default, averageSurface) < minMaterialDistance)
				{
					mat.Overrides.Add(new DirectionOverride { Direction = dir, Surface = averageSurface });
				}
			}
			return mat;
		}

		public static IEnumerable<VoxelCoordinate> GetNeighbours(this VoxelCoordinate coord)
		{
			foreach(var dir in Directions)
			{
				var scaledDir = VoxelCoordinate.DirectionToCoordinate(dir, coord.Layer);
				yield return coord + scaledDir;
			}
		}

		public static int EstimateVertexCount(this ERenderMode mode)
		{
			switch (mode)
			{
				case ERenderMode.Block:
				case ERenderMode.FullCross:
					return 8;
				case ERenderMode.XYCross:
				case ERenderMode.XZCross:
				case ERenderMode.ZYCross:
					return 6;
				case ERenderMode.XPlane:
				case ERenderMode.YPlane:
				case ERenderMode.ZPlane:
					return 4;
			}
			return 0;
		}

		public static IEnumerable<Voxel> GetVoxels(this VoxelMesh mesh, Bounds bounds)
		{
			return mesh?.Voxels
				.Where(v => bounds.Contains(v.Key.ToVector3()))
				.Select(v => v.Value);
		}

		public static IEnumerable<Voxel> GetVoxels(this VoxelMesh mesh, Vector3 localPos, float radius)
		{
			return mesh?.Voxels
				.Where(v => (v.Key.ToVector3() - localPos).sqrMagnitude < (radius * radius))
				.Select(v => v.Value);
		}

		public static bool IsNegative(this EVoxelDirection vox)
		{
			switch (vox)
			{
				case EVoxelDirection.XNeg:
				case EVoxelDirection.YNeg:
				case EVoxelDirection.ZNeg:
					return true;
				default:
					return false;
			}
		}

		public static IEnumerable<VoxelCoordinate> GetVoxelCoordinates(this Bounds bounds, sbyte currentLayer)
		{
			var layerScale = VoxelCoordinate.LayerToScale(currentLayer);
			var halfVox = layerScale * .5f * Vector3.one;
			var minCoord = VoxelCoordinate.FromVector3(bounds.min + halfVox, currentLayer);
			var maxCoord = VoxelCoordinate.FromVector3(bounds.max - halfVox, currentLayer);

			for (var x = minCoord.X; x <= maxCoord.X; ++x)
			{
				for (var y = minCoord.Y; y <= maxCoord.Y; ++y)
				{
					for (var z = minCoord.Z; z <= maxCoord.Z; ++z)
					{
						yield return new VoxelCoordinate(x, y, z, currentLayer);
					}
				}
			}
		}

		public static VoxelMapping Finalise(this IEnumerable<Voxel> voxels)
		{
			var result = new VoxelMapping();
			foreach (var v in voxels)
			{
				result[v.Coordinate] = v;
			}
			return result;
		}

		public static IEnumerable<Voxel> Offset(this IEnumerable<Voxel> voxels, Vector3 offset)
		{
			return voxels.Transform(v => v + VoxelCoordinate.FromVector3(offset, v.Layer));
		}

		public static IEnumerable<Voxel> Rotate(this IEnumerable<Voxel> voxels, Quaternion angle, Vector3 rotationCenter)
		{
			return voxels.Transform(v =>
			{
				var newPos = angle * (v.ToVector3() - rotationCenter) + rotationCenter;
				return VoxelCoordinate.FromVector3(newPos, v.Layer);
			});
		}

		public static IEnumerable<Voxel> Transform(this IEnumerable<Voxel> voxels, Func<VoxelCoordinate, VoxelCoordinate> func)
		{
			return voxels.Select(v =>
			{
				var newCoord = func(v.Coordinate);
				return new Voxel(newCoord, v.Material.Copy());
			});
		}

		public static EVoxelDirection FlipDirection(this EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
					return EVoxelDirection.XPos;
				case EVoxelDirection.XPos:
					return EVoxelDirection.XNeg;
				case EVoxelDirection.YNeg:
					return EVoxelDirection.YPos;
				case EVoxelDirection.YPos:
					return EVoxelDirection.YNeg;
				case EVoxelDirection.ZNeg:
					return EVoxelDirection.ZPos;
				case EVoxelDirection.ZPos:
					return EVoxelDirection.ZNeg;
				default:
					throw new ArgumentException($"Invalid voxel direction {dir}");
			}
		}

		public static IEnumerable<Voxel> FlipSurface(this IEnumerable<Voxel> voxels, EVoxelDirection dir)
		{
			var dAxis = dir.ToString()[0];
			return voxels.Select(v =>
			{
				v.Material = v.Material.Copy();
				for (int i = 0; i < v.Material.Overrides?.Count; i++)
				{
					var o = v.Material.Overrides[i];
					var oAxis = o.Direction.ToString()[0];
					if (oAxis != dAxis)
					{
						continue;
					}
					o.Direction = o.Direction.FlipDirection();
					v.Material.Overrides[i] = o;
				}
				return v;
			});
		}

		public static Bounds GetBounds(this IEnumerable<VoxelCoordinate> voxels)
		{
			if (voxels == null || !voxels.Any())
			{
				return default;
			}
			var b = voxels.First().ToBounds();
			foreach (var b2 in voxels.Skip(1))
			{
				b.Encapsulate(b2.ToBounds());
			}
			return b;
		}

		public static Bounds GetBounds(this IEnumerable<Voxel> voxels)
		{
			if (voxels == null || !voxels.Any())
			{
				return default;
			}
			var b = voxels.First().Coordinate.ToBounds();
			foreach (var b2 in voxels.Skip(1))
			{
				b.Encapsulate(b2.Coordinate.ToBounds());
			}
			return b;
		}

		public static bool RaycastWorld(this VoxelRenderer renderer, Ray ray, float maxDistance, out VoxelCoordinate hit)
		{
			var origin = renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
			var dir = renderer.transform.worldToLocalMatrix.MultiplyVector(ray.direction);
			var localRay = new Ray(origin, dir);
			return RaycastLocal(renderer.Mesh.Voxels.Keys, localRay, maxDistance, out hit);
		}

		public static bool RaycastLocal(this IEnumerable<VoxelCoordinate> coords, Ray localRay, float maxDistance, out VoxelCoordinate hit)
		{
			var cast = coords
				.Where(v =>
				{
					var vPos = v.ToVector3();
					if(Vector3.Distance(localRay.origin, vPos) > maxDistance)
					{
						return false;
					}
					if(Vector3.Dot(localRay.direction.normalized, (localRay.origin - vPos).normalized) < 0)
					{
						//return false;
					}
					return v.ToBounds().IntersectRay(localRay);
				})
				.OrderBy(v => Vector3.Distance(localRay.origin, v.ToVector3()))
				.ToList();
			if(cast.Count == 0)
			{
				hit = default;
				return false;
			}
			hit = cast.First();
			return true;
		}

		public static IEnumerable<Voxel> Optimise(this IEnumerable<Voxel> voxels)
		{
			var allVoxels = voxels.ToList();
			for (int i = allVoxels.Count - 1; i >= 0; i--)
			{
				var voxel = allVoxels[i];
				var parentCoord = voxel.Coordinate.ChangeLayer((sbyte)(voxel.Coordinate.Layer - 1));
			}
			return voxels;
		}

		public static bool CollideCheck(this IEnumerable<VoxelCoordinate> voxels, VoxelCoordinate coord, out VoxelCoordinate collision)
		{
			if (voxels.Contains(coord))
			{
				collision = coord;
				return true;
			}
			var delta = VoxelCoordinate.LayerToScale(coord.Layer) * .01f;
			var b1 = coord.ToBounds();
			b1.Expand(-delta);
			foreach (var vox in voxels)
			{
				var b2 = vox.ToBounds();
				b2.Expand(-delta);
				if (b1.Intersects(b2))
				{
					collision = vox;
					return true;
				}
			};
			collision = default;
			return false;
		}

		public static bool CollideCheck(this IEnumerable<VoxelCoordinate> voxels, Bounds b1)
		{
			foreach (var vox in voxels)
			{
				var b2 = vox.ToBounds();
				if (b1.Intersects(b2))
				{
					return true;
				}
			};
			return false;
		}

		public static IEnumerable<IEnumerable<Voxel>> Chunk(this IEnumerable<Voxel> inVoxels, int chunkSize)
		{
			List<List<Voxel>> result = new List<List<Voxel>>();
			foreach (var v in inVoxels)
			{
				List<Voxel> bestList = result
					.Where(l => l.Count < chunkSize && l.Select(s => s.Coordinate).IsConnected(v.Coordinate))
					.FirstOrDefault();
				if (bestList == null)
				{
					bestList = new List<Voxel>();
					result.Add(bestList);
				}
				bestList.Add(v);
			}
			return result;
		}

		public static bool IsConnected(this IEnumerable<VoxelCoordinate> inVoxels, VoxelCoordinate coord) =>
			inVoxels.Any(v => coord.IsNeighbour(v));

		public static bool IsNeighbour(this VoxelCoordinate coord1, VoxelCoordinate coord)
		{
			return ManhattenDistance(coord1, coord) <= VoxelCoordinate.LayerToScale(Math.Max(coord1.Layer, coord.Layer));
		}

		public static float ManhattenDistance(this VoxelCoordinate coord1, VoxelCoordinate coord)
		{
			return coord.ToVector3().ManhattenDistance(coord1.ToVector3());
		}

		public static float DistanceBetweenSurfaces(SurfaceData surface1, SurfaceData surface2)
		{
			var s1Albedo = surface1.Albedo;
			var s2Albedo = surface2.Albedo;
			var albedoDistance = Vector4.Distance(new Vector4(s1Albedo.r, s1Albedo.g, s1Albedo.b, s1Albedo.a), new Vector4(s2Albedo.r, s2Albedo.g, s2Albedo.b, s2Albedo.a));

			var metallicDistance = Mathf.Abs(surface1.Metallic - surface2.Metallic);
			var smoothnessDistance = Mathf.Abs(surface1.Smoothness - surface2.Smoothness);
			var texFadeDistance = Mathf.Abs(surface1.TextureFade - surface2.TextureFade);

			return albedoDistance + metallicDistance + smoothnessDistance + texFadeDistance;
		}
	}
}