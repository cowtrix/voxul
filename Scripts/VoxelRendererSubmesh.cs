using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Voxul.Utilities;

namespace Voxul
{
	/// <summary>
	/// This is the handler for managing a MeshFilter, MeshRenderer and MeshCollider
	/// with voxel data.
	/// TODO: in order to better support in-scene selection, the first renderer submesh
	/// should be itself.
	/// </summary>
	public class VoxelRendererSubmesh : MonoBehaviour
	{
		public UnityEvent OnRender;

		public VoxelRenderer Parent;
		public MeshFilter MeshFilter;
		public MeshRenderer MeshRenderer;
		public MeshCollider MeshCollider;

		private void OnWillRenderObject()
		{
			OnRender?.Invoke();
		}

		public void SetupComponents(VoxelRenderer r, bool collider)
		{
			if (!this)
			{
				voxulLogger.Warning("Attempting to setup VoxelRendererSubmesh but it has been destroyed.");
				return;
			}
			Parent = r;
			gameObject.hideFlags = gameObject == Parent.gameObject ? Parent.hideFlags : HideFlags.HideInHierarchy;
			if (gameObject != Parent.gameObject)
			{
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				gameObject.transform.localScale = Vector3.one;
			}
			if (!MeshFilter)
			{
				MeshFilter = gameObject.GetOrAddComponent<MeshFilter>();
			}
			if (!MeshRenderer)
			{
				MeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
			}
			if (collider)
			{
				if (!MeshCollider)
				{
					MeshCollider = gameObject.GetOrAddComponent<MeshCollider>();
				}
				if (MeshCollider.convex)
				{
					MeshCollider.convex = false;
				}
			}
			else if(MeshCollider)
			{
				MeshCollider.enabled = false;
			}
		}

		public Bounds Bounds => MeshRenderer.bounds;

		private void OnDestroy()
		{
			MeshFilter.SafeDestroy();
			MeshRenderer.SafeDestroy();
			MeshCollider.SafeDestroy();
		}

#if UNITY_EDITOR
		internal void SetDirty()
		{
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(this);
			if (MeshRenderer)
				UnityEditor.EditorUtility.SetDirty(MeshRenderer);
			if (MeshFilter)
				UnityEditor.EditorUtility.SetDirty(MeshFilter);
			if (MeshCollider)
				UnityEditor.EditorUtility.SetDirty(MeshCollider);
		}
#endif
	}
}