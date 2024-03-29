﻿
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
    public delegate void VoxelRebuildMeshEvent(VoxelMesh mesh);

    [Serializable]
    public class MeshVoxelData
    {
        public Mesh UnityMesh;
    }

    [Serializable]
    public class VoxelPointMapping : SerializableDictionary<Vector3, Vector3>
    {
        public VoxelPointMapping() { }

        public VoxelPointMapping(VoxelPointMapping pointOffsets)
        {
            foreach (var p in pointOffsets)
            {
                this[p.Key] = p.Value;
            }
        }
    }

    [Serializable]
    public class VoxelMeshOptimiserList : PolyList<VoxelOptimiserBase> { }

    /// <summary>
    /// This is the asset object representing a voxel mesh. It contains the voxel information itself,
    /// as well as the bake data if the Voxel Mesh has been invalidated yet.
    /// </summary>
    [CreateAssetMenu(menuName = "voxul/Voxul Mesh")]
    public class VoxelMesh : ScriptableObject
    {
        /// <summary>
        /// The voxel worker transforms the VoxelMapping data into the UnityMeshInstances data.
        /// </summary>
        public VoxelMeshWorker CurrentWorker;
        public bool GenerateLightmaps;
        public List<MeshVoxelData> UnityMeshInstances = new List<MeshVoxelData>();

        [HideInInspector]
        public VoxelPointMapping PointMapping = new VoxelPointMapping();

        /// <summary>
        /// The hash is a random string used to detect asset version changes. It is set below in `Invalidate()`
        /// </summary>
        [HideInInspector]
        public string Hash;

        /// <summary>
        /// This is the voxel data of the object.
        /// </summary>
        [HideInInspector]
        public VoxelMapping Voxels = new VoxelMapping();
        public sbyte MaxLayer;
        public sbyte MinLayer;

        public VoxelMeshOptimiserList Optimisers => OverrideOptimisers ? OptimiserOverrides : VoxelManager.Instance.DefaultOptimisers;
        public bool OptimiseMesh;
        public bool OverrideOptimisers;
        public VoxelMeshOptimiserList OptimiserOverrides = new VoxelMeshOptimiserList();

        private VoxelCoordinateTree<Voxel> m_quickLookup;
        
        public VoxelCoordinateTree<Voxel> GetQuickLookup()
        {
            if (m_quickLookup == null)
            {
                m_quickLookup = new VoxelTree(MinLayer, Voxels);
            }
            return m_quickLookup;
        }

        public void CleanMesh()
        {
            var data = new VoxelMapping(Voxels);
            Voxels.Clear();
            foreach (var v in data)
            {
                var mat = v.Value.Material;
                if (mat.MaterialMode == EMaterialMode.Transparent && mat.Default.Albedo.a > .99f && mat.Overrides.All(o => o.Surface.Albedo.a > .99f))
                {
                    mat.MaterialMode = EMaterialMode.Opaque;
                }
                if (mat.MaterialMode == EMaterialMode.Opaque)
                {
                    mat.Default.Albedo = mat.Default.Albedo.WithAlpha(1);
                    for (int i = 0; i < mat.Overrides.Count; i++)
                    {
                        DirectionOverride ov = mat.Overrides[i];
                        ov.Surface.Albedo = ov.Surface.Albedo.WithAlpha(1);
                        mat.Overrides[i] = ov;
                    }
                }
                var vox = v.Value;
                vox.Material = mat;
                Voxels.AddSafe(vox);
            }
            Invalidate();
        }

        public void Invalidate()
        {
            Hash = Guid.NewGuid().ToString();
            if (Voxels != null && Voxels.Count > 0)
            {
                MaxLayer = Voxels.Max(v => v.Key.Layer);
                MinLayer = Voxels.Min(v => v.Key.Layer);
            }
            else
            {
                MaxLayer = 0;
                MinLayer = 0;
            }
            m_quickLookup = null;
        }
    }
}