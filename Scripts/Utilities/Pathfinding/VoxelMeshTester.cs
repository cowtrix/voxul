using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Pathfinding
{
	[ExecuteAlways]
	public class VoxelMeshTester : MonoBehaviour 
	{
		public VoxelCoordinate From, To;
		public VoxelNavmesh Navmesh => GetComponent<VoxelNavmesh>();

		private void Update()
		{
			Debug.DrawLine(From.ToVector3(), To.ToVector3());
			var path = VoxelPathfindingUtility.GetPath(Navmesh, From, To, VoxelNavmesh.GroundedCheck)?
				.ToList();
			if(path == null || !path.Any())
			{
				return;
			}

			Color c(float dist) => Color.Lerp(Color.green, Color.red, dist / (From.ToVector3() - To.ToVector3()).magnitude);
			var prev = path.First();
			DebugHelper.DrawCube(prev, transform.localToWorldMatrix, Color.red, 0);
			foreach (var p in path.Skip(1))
			{
				var color = c((p.ToVector3() - To.ToVector3()).magnitude);
				DebugHelper.DrawCube(p, transform.localToWorldMatrix, color, 0);
				Debug.DrawLine(prev.ToVector3(), p.ToVector3(), color);
				prev = p;
			}

		}
	}
}
