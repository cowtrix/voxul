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
			foreach(var r in Renderers)
			{
				if (!r.SnapToGrid)
				{
					Debug.LogWarning($"Navmesh mesh {r} is not set to Snap To Grid so it will be ignored.", r);
				}
			}
		}

		public static bool GroundedCheck(ISet<VoxelCoordinate> navmesh, VoxelCoordinate from, VoxelCoordinate to)
		{
			if (!from.IsNeighbour(to))
			{
				return false;
			}
			var groundDir = VoxelCoordinate.DirectionToCoordinate(EVoxelDirection.YNeg, to.Layer);
			return navmesh.CollideCheck(to + groundDir, out _);
		}

		public ISet<VoxelCoordinate> GetCoordinates() => Renderers?
			.Where(r => r.SnapToGrid)
			.SelectMany(r => 
				r.Mesh.Voxels.Keys.Select(k => 
					VoxelCoordinate.FromVector3(transform.localToWorldMatrix.MultiplyPoint3x4(k.ToVector3()), k.Layer))
			).ToSet();
	}
}
