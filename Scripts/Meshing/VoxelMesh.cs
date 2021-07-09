
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public delegate void VoxelRebuildMeshEvent(VoxelMeshWorker worker, VoxelMesh mesh);

	[Serializable]
	public class MeshVoxelData
	{
		public Mesh UnityMesh;
	}

	/// <summary>
	/// This is the asset object representing a voxel mesh. It contains the voxel information itself,
	/// as well as the bake data if the Voxel Mesh has been invalidated yet.
	/// </summary>
	[CreateAssetMenu]
	public class VoxelMesh : ScriptableObject
	{
		/// <summary>
		/// The voxel worker transforms the VoxelMapping data into the UnityMeshInstances data.
		/// </summary>
		public VoxelMeshWorker CurrentWorker;

		public List<MeshVoxelData> UnityMeshInstances = new List<MeshVoxelData>();

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

		public List<VoxelOptimiserBase> Optimisers = new List<VoxelOptimiserBase>();

		public void Invalidate() => Hash = Guid.NewGuid().ToString();
	}
}