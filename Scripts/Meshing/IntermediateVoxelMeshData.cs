using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Meshing
{
	public class IntermediateVoxelMeshData
	{
		public TriangleVoxelMapping TriangleVoxelMapping;
		public VoxelMapping Voxels;

		public List<Vector3> Vertices;
		public Dictionary<int, List<int>> Triangles;
		public List<Color> Color1;
		public List<Vector2> UV1;
		public List<Vector4> UV2;

		public void Initialise(IEnumerable<KeyValuePair<VoxelCoordinate, Voxel>> voxels)
		{
			Voxels = new VoxelMapping(voxels);
			TriangleVoxelMapping = new TriangleVoxelMapping();
			Vertices = new List<Vector3>(Voxels.Count * 8);
			Triangles = new Dictionary<int, List<int>>(Voxels.Count * 16 * 3);
			Color1 = new List<Color>(Vertices.Count);
			UV1 = new List<Vector2>(Vertices.Count);
			UV2 = new List<Vector4>(Vertices.Count);
		}

		public Mesh SetMesh(Mesh mesh)
		{
			if (!mesh)
			{
				mesh = new Mesh();
			}
			lock (mesh)
			{
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
			}
			return mesh;
		}

		public void Clear()
		{
			TriangleVoxelMapping = null;
			Voxels = null;
			Vertices = null;
			UV1 = null;
			UV2 = null;
			Color1 = null;
			Triangles = null;
		}
	}
}