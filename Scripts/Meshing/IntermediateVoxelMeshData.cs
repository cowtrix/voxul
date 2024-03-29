﻿using System.Collections.Generic;
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
        public VoxelMapping Voxels;
        public VoxelPointMapping PointOffsets;

        // Intermediate face data
        public Dictionary<VoxelCoordinate, List<VoxelFaceCoordinate>> CoordinateFaceMapping = new Dictionary<VoxelCoordinate, List<VoxelFaceCoordinate>>();
        public Dictionary<VoxelFaceCoordinate, VoxelFace> Faces;

        // Output
        public List<Vector3> Vertices;
        public List<Vector3> Normals;
        public Dictionary<int, List<int>> Triangles;
        public List<Color> Color1;
        public List<Vector2> UV1;   // Texture
        public List<Vector2> UV2;   // Lightmap
        public List<Vector4> UV3;   // Auxilary data
        public sbyte MinLayer;
        public sbyte MaxLayer;


#if UNITY_2021_1_OR_NEWER
        public bool GenerateLightmaps;
#endif

        /// <summary>
        /// Copy data from voxels and initialize data structures if null.
        /// </summary>
        /// <param name="voxels"></param>
        public void Initialise(IEnumerable<KeyValuePair<VoxelCoordinate, Voxel>> voxels, VoxelPointMapping pointOffsets
#if UNITY_2021_1_OR_NEWER
			, bool generateLightmaps)
		{
			GenerateLightmaps = generateLightmaps;
#else
            )
        {
#endif
            Voxels = new VoxelMapping(voxels);
            PointOffsets = pointOffsets;
            Faces = Faces ?? new Dictionary<VoxelFaceCoordinate, VoxelFace>();
            Faces.Clear();
            CoordinateFaceMapping = CoordinateFaceMapping ?? new Dictionary<VoxelCoordinate, List<VoxelFaceCoordinate>>();
            CoordinateFaceMapping.Clear();
            Vertices = Vertices ?? new List<Vector3>(Voxels.Count * 8);
            Vertices.Clear();
            Normals = Normals ?? new List<Vector3>(Vertices.Count);
            Normals.Clear();
            Triangles = Triangles ?? new Dictionary<int, List<int>>(Voxels.Count * 16 * 3);
            Triangles.Clear();
            Color1 = Color1 ?? new List<Color>(Vertices.Count);
            Color1.Clear();
            UV1 = UV1 ?? new List<Vector2>(Vertices.Count);
            UV1.Clear();
            UV2 = UV2 ?? new List<Vector2>(Vertices.Count);
            UV2.Clear();
            UV3 = UV3 ?? new List<Vector4>(Vertices.Count);
            UV3.Clear();

            MinLayer = sbyte.MaxValue;
            MaxLayer = sbyte.MinValue;
        }

        /// <summary>
        /// Set the data of the mesh to the values in this data object.
        /// </summary>
        /// <param name="mesh">The mesh to modify. If null, a new mesh will be initialised.</param>
        /// <returns>The mesh that has been modified.</returns>
        public Mesh SetMesh(Mesh mesh, bool optimise)
        {
            if (!mesh)
            {
                throw new System.NullReferenceException("Mesh cannot be null at this point");
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
                mesh.SetUVs(2, UV3);
                mesh.SetNormals(Normals);
                mesh.RecalculateBounds();
                if (optimise)
                {
                    mesh.Optimize();
                }
            }
            return mesh;
        }

        /// <summary>
        /// Clear the data.
        /// </summary>
        public void Clear()
        {
            PointOffsets = null;
            Voxels = null;
            Faces = null;
            Vertices = null;
            Normals = null;
            UV1 = null;
            UV3 = null;
            Color1 = null;
            Triangles = null;
            CoordinateFaceMapping = null;
            MinLayer = sbyte.MinValue;
            MaxLayer = sbyte.MaxValue;
        }
    }
}