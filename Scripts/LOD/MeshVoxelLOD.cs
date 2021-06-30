using System;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.LevelOfDetail
{
	[RequireComponent(typeof(VoxelRenderer))]
	public class MeshVoxelLOD : VoxelLOD
	{
		[Range(sbyte.MinValue, sbyte.MaxValue)]
		public sbyte MaxLayer = 0;
		private VoxelRenderer m_voxelRenderer => GetComponent<VoxelRenderer>();

		public override void Rebuild(VoxelRenderer renderer)
		{
			m_voxelRenderer.GenerateCollider = false;
			if (m_voxelRenderer.Mesh == null)
			{
				m_voxelRenderer.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
			}
			m_voxelRenderer.Mesh.Voxels = renderer.Mesh.Voxels.Values
				.Where(s => s.Coordinate.Layer <= MaxLayer)
				.Finalise();
			m_voxelRenderer.Invalidate(false, false);
		}
	}
}