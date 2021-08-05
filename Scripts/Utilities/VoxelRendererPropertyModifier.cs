using System.Collections.Generic;
using UnityEngine;

namespace Voxul
{
	[ExecuteAlways]
	public abstract class VoxelRendererPropertyModifier : MonoBehaviour
	{
		public IEnumerable<VoxelRenderer> Renderers
		{
			get
			{
				var thisComp = GetComponent<VoxelRenderer>();
				if (thisComp)
				{
					yield return thisComp;
				}
				foreach(var subR in GetComponentsInChildren<VoxelRenderer>())
				{
					yield return subR;
				}
			}
		}

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
			foreach(var renderer in Renderers)
			{
				foreach (var submesh in renderer.Submeshes)
				{
					if (submesh.MeshRenderer.HasPropertyBlock())
					{
						submesh.MeshRenderer.GetPropertyBlock(m_propertyBlock);
					}

					SetPropertyBlock(m_propertyBlock, submesh);

					submesh.MeshRenderer.SetPropertyBlock(m_propertyBlock);
				}
			}			
		}

		protected abstract void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh);
	}
}
