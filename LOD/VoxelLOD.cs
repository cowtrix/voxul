using UnityEngine;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[ExecuteAlways]
	public abstract class VoxelLOD : MonoBehaviour
	{
		[Range(0, 1)]
		public float ScreenRelativeTransitionHeight = .25f;

		protected static MaterialPropertyBlock m_propertyBlock;
		public MeshRenderer MeshRenderer => GetComponent<MeshRenderer>();
		protected MeshFilter MeshFilter => GetComponent<MeshFilter>();

		public abstract void Rebuild(VoxelRenderer renderer);
	}
}