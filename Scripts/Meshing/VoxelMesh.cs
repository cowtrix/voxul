
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
		public TriangleVoxelMapping VoxelMapping = new TriangleVoxelMapping();
	}

	[CreateAssetMenu]
	public class VoxelMesh : ScriptableObject
	{
		public VoxelMeshWorker CurrentWorker { get; set; }

		public List<MeshVoxelData> UnityMeshInstances = new List<MeshVoxelData>();

		[HideInInspector]
		public string Hash;
		[HideInInspector]
		public VoxelMapping Voxels = new VoxelMapping();

		public void Invalidate() => Hash = Guid.NewGuid().ToString();
	}
}