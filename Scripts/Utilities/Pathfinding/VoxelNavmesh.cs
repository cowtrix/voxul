using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Pathfinding
{

	public class VoxelNavmesh : MonoBehaviour
	{
		public List<VoxelRenderer> Renderers;

		public void OnValidate()
		{
			if(Renderers == null)
			{
				return;
			}
			foreach(var r in Renderers)
			{
				if (!r)
				{
					continue;
				}
			}
		}

		public static float GroundedCheck(ISet<VoxelCoordinate> navmesh, VoxelCoordinate from, VoxelCoordinate to)
		{
			if (!from.IsNeighbour(to))
			{
				return 1;
			}
			var groundDir = VoxelCoordinate.DirectionToCoordinate(EVoxelDirection.YNeg, to.Layer);
			return navmesh.CollideCheck(to + groundDir, out _) ? 1 : 0;
		}

		public ISet<VoxelCoordinate> GetCoordinates() => Renderers?
			.Where(r => r.SnapMode == VoxelRenderer.eSnapMode.Global)
			.SelectMany(r => 
				r.Mesh.Voxels.Keys.Select(k => 
					VoxelCoordinate.FromVector3(transform.localToWorldMatrix.MultiplyPoint3x4(k.ToVector3()), k.Layer))
			).ToSet();
	}
}
