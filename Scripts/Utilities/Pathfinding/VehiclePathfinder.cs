using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Pathfinding
{
	public class VehiclePathfinder : ExtendedMonoBehaviour
	{ 
		public sbyte Layer;
		public VoxelRenderer Cost;

		public VoxelCoordinate TargetPosition { get; private set; }
		private List<VoxelCoordinate> m_path = new List<VoxelCoordinate>();

		public IEnumerable<VoxelCoordinate> GetPathToPosition(Vector3 position)
		{
			var origin = VoxelCoordinate.FromVector3(transform.position, Layer);
			var destination = VoxelCoordinate.FromVector3(position, Layer);
			return VoxelPathfindingUtility.GetPath(default(VoxelNavmesh), origin, destination, GetCosts); 
		}

		private float GetCosts(ISet<VoxelCoordinate> navmesh, VoxelCoordinate from, VoxelCoordinate to)
		{
			var toLocalVec = Cost.transform.worldToLocalMatrix.MultiplyPoint3x4(to.ToVector3());
			var toLocalVoxelCoord = VoxelCoordinate.FromVector3(toLocalVec, Layer);
			return Cost.Mesh.Voxels.ContainsKey(toLocalVoxelCoord) ? .5f : 1f;
		}

		private void Start()
		{
			StartCoroutine(Think());
		}

		private IEnumerator Think()
		{
			var coords = Cost.Mesh.Voxels.Keys
				.ToList();
			while (true)
			{
				yield return null;
				TargetPosition = coords.Random().ChangeLayer(Layer);
				var path = GetPathToPosition(TargetPosition.ToVector3());
				if(path == null)
				{
					yield return null;
					continue;
				}
				m_path = path.ToList();
				foreach(var pos in m_path)
				{
					yield return StartCoroutine(MoveToPoint(pos));
				}
			}
		}

		private IEnumerator MoveToPoint(VoxelCoordinate coord)
		{
			var coordVec = coord.ToVector3();
			while (Vector3.Distance(transform.position, coordVec) > .1f)
			{
				yield return null;
				transform.position = Vector3.MoveTowards(transform.position, coordVec, Time.deltaTime);
			}
		}

		private void OnDrawGizmosSelected()
		{
			if(m_path == null || !m_path.Any())
			{
				return;
			}
			var lastP = m_path.First();
			foreach(var p in m_path)
			{
				Gizmos.DrawCube(lastP.ToVector3(), Vector3.one * .1f);
				Gizmos.DrawLine(lastP.ToVector3(), p.ToVector3());
				lastP = p;
			}
			Gizmos.DrawCube(m_path.Last().ToVector3(), Vector3.one * .15f);
		}
	}
}
