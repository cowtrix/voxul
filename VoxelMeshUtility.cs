
using Common;
using System.Linq;
using UnityEngine;

public static class VoxelMeshUtility
{
	public static void GetPlane(Vector3 origin, float offset, Vector2 size, EVoxelDirection dir,
		VoxelMaterial material, IntermediateVoxelMeshData data)
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
		var vOffset = data.Vertices.Count;
		data.Vertices.AddRange(new[]
		{
			v1, v2, v3, v4
		});

		// Triangles
		if(!data.Triangles.TryGetValue(submeshIndex, out var submeshList))
		{
			submeshList = new System.Collections.Generic.List<int>();
			data.Triangles[submeshIndex] = submeshList;
		}
		submeshList.AddRange(new[]
		{
			// Cube Left Side Triangles
			3 + vOffset, 1 + vOffset, 0 + vOffset,
			3 + vOffset, 2 + vOffset, 1 + vOffset,
		});

		Vector2 _00_CORDINATES = new Vector2(1f, 1f);
		Vector2 _10_CORDINATES = new Vector2(0f, 1f);
		Vector2 _01_CORDINATES = new Vector2(1f, 0f);
		Vector2 _11_CORDINATES = new Vector2(0f, 0f);
		var uvMode = surface.UVMode;
		switch (uvMode)
		{
			case EUVMode.Local:
				data.UV1.AddRange(new[]
				{
					_11_CORDINATES, _01_CORDINATES, _00_CORDINATES, _10_CORDINATES,
				});
				break;
			case EUVMode.LocalScaled:
				data.UV1.AddRange(new[]
				{
					_11_CORDINATES * size.x, _01_CORDINATES * size.x, _00_CORDINATES * size.x, _10_CORDINATES * size.x,
				});
				break;
			case EUVMode.Global:
				switch (dir)
				{
					case EVoxelDirection.ZNeg:
					case EVoxelDirection.ZPos:
						data.UV1.AddRange(new[] { v1.xy(), v2.xy(), v3.xy(), v4.xy(), });
						break;
					case EVoxelDirection.YNeg:
					case EVoxelDirection.YPos:
						data.UV1.AddRange(new[] { v1.xz(), v2.xz(), v3.xz(), v4.xz(), });
						break;
					case EVoxelDirection.XNeg:
					case EVoxelDirection.XPos:
						data.UV1.AddRange(new[] { v1.yz(), v2.yz(), v3.yz(), v4.yz(), });
						break;
				}
				break;
			case EUVMode.GlobalScaled:
				switch (dir)
				{
					case EVoxelDirection.ZNeg:
					case EVoxelDirection.ZPos:
						data.UV1.AddRange(new[] { v1.xy() / size.x, v2.xy() / size.x, v3.xy() / size.x, v4.xy() / size.x, });
						break;
					case EVoxelDirection.YNeg:
					case EVoxelDirection.YPos:
						data.UV1.AddRange(new[] { v1.xz() / size.x, v2.xz() / size.x, v3.xz() / size.x, v4.xz() / size.x, });
						break;
					case EVoxelDirection.XNeg:
					case EVoxelDirection.XPos:
						data.UV1.AddRange(new[] { v1.yz() / size.x, v2.yz() / size.x, v3.yz() / size.x, v4.yz() / size.x, });
						break;
				}
				break;
		}

	}
}
