using UnityEngine;

namespace Voxul
{
	[ExecuteAlways]
	public abstract class VoxelRendererPropertyModifier : MonoBehaviour
	{
		public VoxelRenderer Renderer => GetComponent<VoxelRenderer>();
		private static MaterialPropertyBlock m_propertyBlock;

		private void OnValidate()
		{
			Invalidate();
		}

		public void Invalidate()
		{
			if (m_propertyBlock == null)
			{
				m_propertyBlock = new MaterialPropertyBlock();
			}
			m_propertyBlock.Clear();
			foreach (var submesh in Renderer.Renderers)
			{
				if (submesh.MeshRenderer.HasPropertyBlock())
				{
					submesh.MeshRenderer.GetPropertyBlock(m_propertyBlock);
				}

				SetPropertyBlock(m_propertyBlock, submesh);

				submesh.MeshRenderer.SetPropertyBlock(m_propertyBlock);
			}
		}

		protected abstract void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh);
	}
}
