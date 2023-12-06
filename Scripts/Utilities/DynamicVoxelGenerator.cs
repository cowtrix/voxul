using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	[RequireComponent(typeof(VoxelRenderer))]
	public abstract class DynamicVoxelGenerator : ExtendedMonoBehaviour
	{
		public VoxelRenderer Renderer { get; private set; }
		public EThreadingMode ThreadingMode;
		[NonSerialized]
		private bool m_isGenerating;

		private void Awake()
		{
			Renderer = GetComponent<VoxelRenderer>();
		}

		[ContextMenu("Generate")]
		public void Generate()
		{
			if (m_isGenerating)
			{
				return;
			}
			if (!Renderer)
			{
				Renderer = GetComponent<VoxelRenderer>();
			}
			if (!Renderer.Mesh)
			{
				Renderer.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
			}
			StartCoroutine(GenerateInternal());
		}

		IEnumerator GenerateInternal()
		{
			Renderer.Mesh.Voxels.Clear();
			m_isGenerating = true;
			if(ThreadingMode == EThreadingMode.Task)
			{
                var t = new Task(() =>
                {
                    try
                    {
                        SetVoxels(Renderer);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    m_isGenerating = false;
                });
                t.Start();
                while (m_isGenerating)
                {
                    yield return null;
                }
            }
			else
			{
                try
                {
                    SetVoxels(Renderer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                m_isGenerating = false;
            }
            Renderer.Mesh.Invalidate();
			Renderer.Invalidate(true, false);
		}

		protected abstract void SetVoxels(VoxelRenderer renderer);
	}
}
