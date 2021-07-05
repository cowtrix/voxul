using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Meshing
{
	/// <summary>
	/// This is the data container for a rebake job, where we store data
	/// for use within a VoxelMeshWorker.
	/// </summary>
	public class IntermediateVoxelMeshData
	{
		// Input
		public TriangleVoxelMapping TriangleVoxelMapping;
		public VoxelMapping Voxels;

		// Intermediate face data
		public Dictionary<VoxelFaceCoordinate, VoxelFace> Faces;

		// Output
		public List<Vector3> Vertices;
		public Dictionary<int, List<int>> Triangles;
		public List<Color> Color1;
		public List<Vector2> UV1;
		public List<Vector4> UV2;

		/// <summary>
		/// Copy data from voxels and initialize data structures if null.
		/// </summary>
		/// <param name="voxels"></param>
		public void Initialise(IEnumerable<KeyValuePair<VoxelCoordinate, Voxel>> voxels)
		{
			Voxels = new VoxelMapping(voxels);
			TriangleVoxelMapping = TriangleVoxelMapping ?? new TriangleVoxelMapping();
			TriangleVoxelMapping.Clear();
			Faces = Faces ?? new Dictionary<VoxelFaceCoordinate, VoxelFace>();
			Faces.Clear();
			Vertices = Vertices ?? new List<Vector3>(Voxels.Count * 8);
			Vertices.Clear();
			Triangles = Triangles ?? new Dictionary<int, List<int>>(Voxels.Count * 16 * 3);
			Triangles.Clear();
			Color1 = Color1 ?? new List<Color>(Vertices.Count);
			Color1.Clear();
			UV1 = UV1 ?? new List<Vector2>(Vertices.Count);
			UV1.Clear();
			UV2 = UV2 ?? new List<Vector4>(Vertices.Count);
			UV2.Clear();
		}

		/// <summary>
		/// Set the data of the mesh to the values in this data object.
		/// </summary>
		/// <param name="mesh">The mesh to modify. If null, a new mesh will be initialised.</param>
		/// <returns>The mesh that has been modified.</returns>
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
				if (Triangles != null && Triangles.Any())
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

		/// <summary>
		/// Clear the data.
		/// </summary>
		public void Clear()
		{
			TriangleVoxelMapping = null;
			Voxels = null;
			Faces = null;
			Vertices = null;
			UV1 = null;
			UV2 = null;
			Color1 = null;
			Triangles = null;
		}
	}
}