using UnityEngine;

namespace Voxul
{
	[RequireComponent(typeof(VoxelRenderer))]
	public abstract class DynamicVoxelGenerator : ExtendedMonoBehaviour
	{
		private VoxelRenderer Renderer => GetComponent<VoxelRenderer>();

		[ContextMenu("Generate")]
		public void Generate()
		{
			Renderer.Mesh.Voxels.Clear();
			SetVoxels(Renderer);
			Renderer.Mesh.Invalidate();
			Renderer.Invalidate(true, false);
		}

		protected abstract void SetVoxels(VoxelRenderer renderer);
	}
}
