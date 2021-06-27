using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Meshing
{
	public class IntermediateVoxelMeshData
	{
		public IntermediateVoxelMeshData(VoxelMapping voxels, TriangleVoxelMapping mapping)
		{
			Voxels = voxels;
			VoxelMapping = mapping;
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
				Debug.Log("Created a new mesh in IntermediateVoxelMeshData");
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
}