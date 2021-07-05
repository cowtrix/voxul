using UnityEngine;

namespace Voxul.Meshing
{
	public abstract class VoxelOptimiserBase : ScriptableObject
	{
		public virtual void OnPreRebakeMainThread(IntermediateVoxelMeshData data) { }

		public virtual void OnPreFaceStep(IntermediateVoxelMeshData data) { }

		public virtual void OnBeforeCompleteOffThread(IntermediateVoxelMeshData data) { }
	}
}