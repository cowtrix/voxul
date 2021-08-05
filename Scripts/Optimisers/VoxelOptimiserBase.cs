using System;
using UnityEngine;

namespace Voxul.Meshing
{
	[Serializable]
	public abstract class VoxelOptimiserBase
	{
		public bool Enabled = true;

		public virtual void OnPreRebakeMainThread(IntermediateVoxelMeshData data) { }

		public virtual void OnPreFaceStep(IntermediateVoxelMeshData data) { }

		public virtual void OnBeforeCompleteOffThread(IntermediateVoxelMeshData data) { }
	}
}