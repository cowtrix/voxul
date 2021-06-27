using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public static class JobExtensions
	{
		public static void AddRange<T>(this NativeArray<T> array, IEnumerable<T> toAdd, int index) where T:struct
		{
			int counter = 0;
			foreach(var i in toAdd)
			{
				array[index + counter] = i;
				counter++;
			}
		}
	}

	public struct VoxelJob : IJob
	{
		[ReadOnly]
		public NativeArray<Voxel> Voxels;
		public NativeArray<Vector3> Vertices;
		public NativeArray<Color> Colors;
		public NativeArray<Vector2> UV1;
		public NativeArray<Vector4> UV2;
		public NativeArray<int> TriangleVoxelMapping;
		public NativeArray<int> OpaqueTriangles;
		public NativeArray<int> TransparentTriangles;

		public int StartIndex;
		public int EndIndex;

		public void Execute()
		{
			for(var i = StartIndex; i <= EndIndex; ++i)
			{
				var vox = Voxels[i];
				switch (vox.Material.RenderMode)
				{
					case ERenderMode.Block:
						Cube(vox, i);
						break;
					case ERenderMode.XPlane:
						Plane(vox, i, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, });
						break;
					case ERenderMode.YPlane:
						Plane(vox, i, new[] { EVoxelDirection.YPos, EVoxelDirection.YNeg, });
						break;
					case ERenderMode.ZPlane:
						Plane(vox, i, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
						break;
					case ERenderMode.XYCross:
						Plane(vox, i, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
						break;
					case ERenderMode.XZCross:
						Plane(vox, i, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
						break;
					case ERenderMode.ZYCross:
						Plane(vox, i, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
						break;
					case ERenderMode.FullCross:
						Plane(vox, i, new[] { EVoxelDirection.XNeg, EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.XNeg, EVoxelDirection.XNeg, EVoxelDirection.XNeg, });
						break;
				}
				if (Vertices.Length > 65535 - 100)
				{
					break;
				}
			}

		}

		private NativeArray<int> GetTrianglesForMode(EMaterialMode mode) =>
			(mode == EMaterialMode.Opaque ? OpaqueTriangles : TransparentTriangles);

		public void Plane(Voxel vox, int voxelIndex, IEnumerable<EVoxelDirection> dirs)
		{
			var origin = vox.Coordinate.ToVector3();
			var size = vox.Coordinate.GetScale() * Vector3.one;
			DoPlanes(origin, 0, size.xz(), dirs, vox, voxelIndex);
		}

		public void Cube(Voxel vox, int voxelIndex)
		{
			var origin = vox.Coordinate.ToVector3();
			var size = vox.Coordinate.GetScale() * Vector3.one;
			var dirs = new List<EVoxelDirection>
			{
				EVoxelDirection.XNeg,
				EVoxelDirection.XPos,
				EVoxelDirection.YNeg,
				EVoxelDirection.YPos,
				EVoxelDirection.ZNeg,
				EVoxelDirection.ZPos,
			};

			var higherLayer = (sbyte)(vox.Coordinate.Layer - 1);
			var higherCoord = vox.Coordinate.ChangeLayer(higherLayer);

			for (int i = dirs.Count - 1; i >= 0; i--)
			{
				EVoxelDirection dir = dirs[i];
				//var neighborCoord = vox.Coordinate + VoxelCoordinate.DirectionToCoordinate(dir, vox.Coordinate.Layer);
				/*if (Voxels != null
					&& Voxels.TryGetValue(neighborCoord, out var n)
					&& n.Material.RenderMode == ERenderMode.Block
					&& n.Material.MaterialMode == vox.Material.MaterialMode)
				{
					dirs.RemoveAt(i);
					continue;
				}*/

				// If the neighbour in a higher layer blocks then the whole side is guaranteed to be occluded
				/*neighborCoord = higherCoord + VoxelCoordinate.DirectionToCoordinate(dir, higherLayer);
				if (Voxels.TryGetValue(neighborCoord, out n)
					&& n.Material.RenderMode == ERenderMode.Block
					&& n.Material.MaterialMode == vox.Material.MaterialMode)
				{
					dirs.RemoveAt(i);
				}*/
			}
			DoPlanes(origin, size.y, size.xz(), dirs, vox, voxelIndex);
		}

		private void DoPlanes(Vector3 origin, float offset, Vector2 size,
			IEnumerable<EVoxelDirection> dirs, Voxel vox, int voxelIndex)
		{
			var tris = GetTrianglesForMode(vox.Material.MaterialMode);
			var startTri = tris.Length / 3;
			foreach (var dir in dirs)
			{
				var surface = vox.Material.GetSurface(dir);
				// Get the basic mesh stuff
				GetPlane(voxelIndex, origin, offset, size, dir, vox.Material);

				// Do the colors
				Colors.AddRange(Enumerable.Repeat(surface.Albedo, 4), voxelIndex);

				// UV2 extra data
				var uv2 = new Vector4(surface.Smoothness, surface.Texture.Index, surface.Metallic, 1 - surface.TextureFade)
					.RemoveNans();
				UV2.AddRange(Enumerable.Repeat(uv2, 4), voxelIndex);

				var endTri = tris.Length / 3;
				for (var j = startTri; j < endTri; ++j)
				{
					TriangleVoxelMapping[j] = voxelIndex;
				}
			}
		}

		public void GetPlane(int voxelIndex, Vector3 origin, float offset, Vector2 size, EVoxelDirection dir, VoxelMaterial material)
		{
			var surface = material.GetSurface(dir);
			var submeshIndex = (int)material.MaterialMode;

			var cubeLength = size.x;
			var cubeWidth = offset;
			var cubeHeight = size.y;

			Quaternion rot = VoxelCoordinate.DirectionToQuaternion(dir);

			// Vertices
			Vector3 v1 = origin + rot * new Vector3(-cubeLength * .5f, cubeWidth * .5f, -cubeHeight * .5f);
			Vector3 v2 = origin + rot * new Vector3(cubeLength * .5f, cubeWidth * .5f, -cubeHeight * .5f);
			Vector3 v3 = origin + rot * new Vector3(cubeLength * .5f, cubeWidth * .5f, cubeHeight * .5f);
			Vector3 v4 = origin + rot * new Vector3(-cubeLength * .5f, cubeWidth * .5f, cubeHeight * .5f);
			var vOffset = Vertices.Length;
			Vertices.AddRange(new[] { v1, v2, v3, v4 }, voxelIndex);

			// Triangles
			var tris = GetTrianglesForMode(material.MaterialMode);
			tris.AddRange(new[]
			{
				// Cube Left Side Triangles
				3 + vOffset, 1 + vOffset, 0 + vOffset,
				3 + vOffset, 2 + vOffset, 1 + vOffset,
			}, voxelIndex);

			Vector2 _00_CORDINATES = new Vector2(1f, 1f);
			Vector2 _10_CORDINATES = new Vector2(0f, 1f);
			Vector2 _01_CORDINATES = new Vector2(1f, 0f);
			Vector2 _11_CORDINATES = new Vector2(0f, 0f);
			var uvMode = surface.UVMode;
			switch (uvMode)
			{
				case EUVMode.Local:
					UV1.AddRange(new[]
						{
						_11_CORDINATES, _01_CORDINATES, _00_CORDINATES, _10_CORDINATES,
					}, voxelIndex);
					break;
				case EUVMode.LocalScaled:
					UV1.AddRange(new[]
						{
						_11_CORDINATES * size.x, _01_CORDINATES * size.x, _00_CORDINATES * size.x, _10_CORDINATES * size.x,
					}, voxelIndex);
					break;
				case EUVMode.Global:
					switch (dir)
					{
						case EVoxelDirection.ZNeg:
						case EVoxelDirection.ZPos:
							UV1.AddRange(new[] { v1.xy(), v2.xy(), v3.xy(), v4.xy(), }, voxelIndex);
							break;
						case EVoxelDirection.YNeg:
						case EVoxelDirection.YPos:
							UV1.AddRange(new[] { v1.xz(), v2.xz(), v3.xz(), v4.xz(), }, voxelIndex);
							break;
						case EVoxelDirection.XNeg:
						case EVoxelDirection.XPos:
							UV1.AddRange(new[] { v1.yz(), v2.yz(), v3.yz(), v4.yz(), }, voxelIndex);
							break;
					}
					break;
				case EUVMode.GlobalScaled:
					switch (dir)
					{
						case EVoxelDirection.ZNeg:
						case EVoxelDirection.ZPos:
							UV1.AddRange(new[] { v1.xy() / size.x, v2.xy() / size.x, v3.xy() / size.x, v4.xy() / size.x, }, voxelIndex);
							break;
						case EVoxelDirection.YNeg:
						case EVoxelDirection.YPos:
							UV1.AddRange(new[] { v1.xz() / size.x, v2.xz() / size.x, v3.xz() / size.x, v4.xz() / size.x, }, voxelIndex);
							break;
						case EVoxelDirection.XNeg:
						case EVoxelDirection.XPos:
							UV1.AddRange(new[] { v1.yz() / size.x, v2.yz() / size.x, v3.yz() / size.x, v4.yz() / size.x, }, voxelIndex);
							break;
					}
					break;
			}

		}
	}
}