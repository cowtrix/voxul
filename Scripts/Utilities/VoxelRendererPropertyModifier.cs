using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul
{
	[ExecuteAlways]
	public abstract class VoxelRendererPropertyModifier : ExtendedMonoBehaviour
	{
		public bool IncludeChildren = true;
		public IEnumerable<VoxelRenderer> Renderers
		{
			get
			{
				if(m_renderers != null)
				{
					return m_renderers;
				}
				m_renderers = new List<VoxelRenderer>();
				var lod = GetComponent<LODGroup>();
				if (lod)
				{
					foreach(var l in lod.GetLODs())
					{
						foreach(var r in l.renderers)
						{
							var vt = r?.GetComponent<VoxelColorTint>();
                            if (vt && vt != this)
                            {
								continue;
                            }
							var vr = r?.GetComponent<VoxelRenderer>();
							if (vr && !m_renderers.Contains(vr))
							{
								m_renderers.Add(vr);
							}
						}
					}
				}
				else
				{
					var thisComp = GetComponent<VoxelRenderer>();
					if (thisComp && !m_renderers.Contains(thisComp))
					{
						m_renderers.Add(thisComp);
					}
				}
				if (IncludeChildren)
				{
					m_renderers.AddRange(GetComponentsInChildren<VoxelRenderer>().Where(r => !r.GetComponent<VoxelColorTint>()));
				}
				return m_renderers;
			}
		}
		private List<VoxelRenderer> m_renderers;

		private static MaterialPropertyBlock m_propertyBlock;

		private void OnValidate()
		{
			Invalidate();
		}

		private void OnEnable()
		{
			Invalidate();
		}

		private void OnDisable()
		{
			foreach (var renderer in Renderers)
			{
				if (!renderer)
				{
					continue;
				}
				foreach (var submesh in renderer.Submeshes)
				{
					submesh.MeshRenderer?.SetPropertyBlock(null);
				}
			}
		}

		[ContextMenu("Refresh Renderers")]
		public void RefreshRenderers()
		{
			m_renderers = null;
			Invalidate();
		}

		[ContextMenu("Invalidate")]
		public void Invalidate()
		{
			if (!enabled)
			{
				return;
			}
			if (m_propertyBlock == null)
			{
				m_propertyBlock = new MaterialPropertyBlock();
			}
			m_propertyBlock.Clear();
			foreach(var renderer in Renderers)
			{
				if (!renderer)
				{
					continue;
				}
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
			this.TrySetDirty();
		}

		protected abstract void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh);
	}
}
