using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class IntermediateVoxelMeshData
{
	public IntermediateVoxelMeshData(VoxelMesh mesh)
	{
		Voxels = mesh?.Voxels;
		VoxelMapping = mesh?.VoxelMapping;
	}

	public TriangleVoxelMapping VoxelMapping;
	public VoxelMapping Voxels;

	public List<Vector3> Vertices = new List<Vector3>();
	public Dictionary<int, List<int>> Triangles = new Dictionary<int, List<int>>();
	public List<Color> Color1 = new List<Color>();
	public List<Vector2> UV1 = new List<Vector2>();
	public List<Vector4> UV2 = new List<Vector4>();

	public Mesh SetMesh(Mesh mesh)
	{
		if (!mesh)
		{
			mesh = new Mesh();
		}
		mesh.Clear();
		mesh.SetVertices(Vertices);
		mesh.SetColors(Color1);
		if (Triangles.Any())
		{
			var meshCount = Triangles.Max(k => k.Key) + 1;
			mesh.subMeshCount = meshCount;
			foreach (var submesh in Triangles)
			{
				mesh.SetTriangles(submesh.Value, submesh.Key);
			}
		}
		mesh.SetUVs(0, UV1);
		mesh.SetUVs(1, UV2);
		mesh.RecalculateNormals();
		return mesh;
	}
}

[Serializable]
public struct VoxelCoordinateTriangleMapping
{
	public VoxelCoordinate Coordinate;
	public EVoxelDirection Direction;
}

[Serializable]
public class TriangleVoxelMapping : SerializableDictionary<int, TriangleVoxelMapping.InnerMapping>
{

	[Serializable]
	public class InnerMapping : SerializableDictionary<int, VoxelCoordinateTriangleMapping> { }
}
[Serializable]
public class VoxelMapping : SerializableDictionary<VoxelCoordinate, Voxel>
{
	public bool AddSafe(Voxel vox)
	{
		if (Keys.CollideCheck(vox.Coordinate, out var hit))
		{
			Debug.LogWarning($"Voxel {vox.Coordinate} collided with {hit} and so was skipped");
			return false;
		}
		Add(vox.Coordinate, vox);
		return true;
	}

	public void SetSafe(Voxel vox)
	{
		while (Keys.CollideCheck(vox.Coordinate, out var hit))
		{
			Remove(hit);
		}
		Add(vox.Coordinate, vox);
	}
}

[CreateAssetMenu]
public class VoxelMesh : ScriptableObject
{
	public Mesh Mesh;
	[HideInInspector]
	public string Hash;
	[HideInInspector]
	public TriangleVoxelMapping VoxelMapping = new TriangleVoxelMapping();
	[HideInInspector]
	public VoxelMapping Voxels = new VoxelMapping();

	public static EVoxelDirection[] Directions = Enum.GetValues(typeof(EVoxelDirection)).Cast<EVoxelDirection>().ToArray();

	public void Invalidate() => Hash = Guid.NewGuid().ToString();

	public Mesh GenerateMeshInstance(sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
	{
		if (!Mesh
#if UNITY_EDITOR
			|| (Mesh && UnityEditor.AssetDatabase.Contains(this) && !UnityEditor.AssetDatabase.Contains(Mesh))
#endif
			)
		{
			Mesh = new Mesh();
#if UNITY_EDITOR
			if(UnityEditor.AssetDatabase.Contains(this))
			{
				var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
				UnityEditor.AssetDatabase.AddObjectToAsset(Mesh, assetPath);
			}
#endif
		}
		Mesh.name = $"{name}_mesh_{Hash}";
		var data = new IntermediateVoxelMeshData(this);
		foreach (var vox in Voxels
			.Where(v => v.Key.Layer >= minLayer && v.Key.Layer <= maxLayer)
			.OrderBy(v => v.Value.Material.MaterialMode))
		{
			if (vox.Key != vox.Value.Coordinate)
			{
				throw new Exception($"Voxel {vox.Key} had incorrect key in data");
			}
			switch (vox.Value.Material.RenderMode)
			{
				case ERenderMode.Block:
					Cube(vox.Value, data);
					break;
				case ERenderMode.XPlane:
					Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, });
					break;
				case ERenderMode.YPlane:
					Plane(vox.Value, data, new[] { EVoxelDirection.YPos, EVoxelDirection.YNeg, });
					break;
				case ERenderMode.ZPlane:
					Plane(vox.Value, data, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
					break;
				case ERenderMode.XYCross:
					Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
					break;
				case ERenderMode.XZCross:
					Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
					break;
				case ERenderMode.ZYCross:
					Plane(vox.Value, data, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
					break;
				case ERenderMode.FullCross:
					Plane(vox.Value, data, Directions.ToArray());
					break;
			}
		}

		Mesh = data.SetMesh(Mesh);
		VoxelMapping = data.VoxelMapping;
		Voxels = data.Voxels;
		return Mesh;
	}

	public IEnumerable<VoxelCoordinate> GetVoxelCoordinates(Bounds bounds, sbyte currentLayer)
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

	public IEnumerable<Voxel> GetVoxels(Bounds bounds)
	{
		return Voxels
			.Where(v => bounds.Contains(v.Key.ToVector3()))
			.Select(v => v.Value);
	}

	public IEnumerable<Voxel> GetVoxels(Vector3 localPos, float radius)
	{
		return Voxels
			.Where(v => (v.Key.ToVector3() - localPos).sqrMagnitude < (radius * radius))
			.Select(v => v.Value);
	}

	public static void Plane(Voxel vox, IntermediateVoxelMeshData data, IEnumerable<EVoxelDirection> dirs)
	{
		var origin = vox.Coordinate.ToVector3();
		var size = vox.Coordinate.GetScale() * Vector3.one;
		DoPlanes(origin, 0, size.xz(), dirs, vox, data);
	}

	public static void Cube(Voxel vox, IntermediateVoxelMeshData data)
	{
		var origin = vox.Coordinate.ToVector3();
		var size = vox.Coordinate.GetScale() * Vector3.one;
		var dirs = Directions.ToList();

		var higherLayer = (sbyte)(vox.Coordinate.Layer - 1);
		var higherCoord = vox.Coordinate.ChangeLayer(higherLayer);

		for (int i = dirs.Count - 1; i >= 0; i--)
		{
			EVoxelDirection dir = dirs[i];
			var neighborCoord = vox.Coordinate + VoxelCoordinate.DirectionToCoordinate(dir, vox.Coordinate.Layer);
			if (data.Voxels != null
				&& data.Voxels.TryGetValue(neighborCoord, out var n)
				&& n.Material.RenderMode == ERenderMode.Block
				&& n.Material.MaterialMode == vox.Material.MaterialMode)
			{
				dirs.RemoveAt(i);
				continue;
			}

			// If the neighbour in a higher layer blocks then the whole side is guaranteed to be occluded
			/*neighborCoord = higherCoord + VoxelCoordinate.DirectionToCoordinate(dir, higherLayer);
			if (Voxels.TryGetValue(neighborCoord, out n)
				&& n.Material.RenderMode == ERenderMode.Block
				&& n.Material.MaterialMode == vox.Material.MaterialMode)
			{
				dirs.RemoveAt(i);
			}*/
		}
		DoPlanes(origin, size.y, size.xz(), dirs, vox, data);
	}

	private static void DoPlanes(Vector3 origin, float offset, Vector2 size,
		IEnumerable<EVoxelDirection> dirs, Voxel vox, IntermediateVoxelMeshData data)
	{
		var submeshIndex = (int)vox.Material.MaterialMode;
		if (!data.Triangles.TryGetValue(submeshIndex, out var tris))
		{
			tris = new List<int>();
			data.Triangles[submeshIndex] = tris;
			if (data.VoxelMapping != null)
			{
				data.VoxelMapping[submeshIndex] = new TriangleVoxelMapping.InnerMapping();
			}
		}
		var startTri = tris.Count / 3;
		foreach (var dir in dirs)
		{
			var surface = vox.Material.GetSurface(dir);
			// Get the basic mesh stuff
			VoxelMeshUtility.GetPlane(origin, offset, size, dir, vox.Material, data);

			// Do the colors
			data.Color1.AddRange(Enumerable.Repeat(surface.Albedo, 4));

			// UV2 extra data
			var uv2 = new Vector4(surface.Smoothness, surface.Texture.Index, surface.Metallic, 1 - surface.TextureFade);
			data.UV2.AddRange(Enumerable.Repeat(uv2, 4));

			var endTri = tris.Count / 3;
			for (var j = startTri; j < endTri; ++j)
			{
				if (data.VoxelMapping != null)
				{
					data.VoxelMapping[submeshIndex][j] =
						new VoxelCoordinateTriangleMapping { Coordinate = vox.Coordinate, Direction = dir };
				}
			}
		}
	}

}
