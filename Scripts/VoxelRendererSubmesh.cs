using UnityEngine;
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
		public MeshFilter MeshFilter;
		public MeshRenderer MeshRenderer;
		public MeshCollider MeshCollider;

		public void SetupComponents(bool collider)
		{
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
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
				MeshCollider.convex = false;
				MeshCollider.cookingOptions = MeshColliderCookingOptions.None;
			}
		}

		public Bounds Bounds => MeshRenderer.bounds;
	}
}