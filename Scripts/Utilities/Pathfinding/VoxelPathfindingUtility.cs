using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Pathfinding
{

	/// <summary>
	/// An implementation of the A* algorithm for 3D pathfinding in voxel space.
	/// </summary>
	public static class VoxelPathfindingUtility
	{
		public delegate float PathfindingCostDelegate(ISet<VoxelCoordinate> navmesh, VoxelCoordinate from, VoxelCoordinate to);

		public static IEnumerable<VoxelCoordinate> GetPath(this VoxelNavmesh navmesh, VoxelCoordinate from, VoxelCoordinate to,
			PathfindingCostDelegate pathBehaviour = null)
		{
			return GetPath(navmesh?.GetCoordinates(), from, to, pathBehaviour);
		}

		private static List<VoxelCoordinate> ReconstructPathRecursive(VoxelCoordinate coord,
			Dictionary<VoxelCoordinate, (VoxelCoordinate, float)> history,
			List<VoxelCoordinate> path = null)
		{
			if (path == null)
			{
				path = new List<VoxelCoordinate>();
				path.Add(coord);
			}
			if (!history.TryGetValue(coord, out var p))
			{
				return path;
			}
			if (p.Item1 == coord)
			{
				throw new System.Exception("Cyclical path detected, this shouldn't happen.");
			}
			path.Add(p.Item1);
			return ReconstructPathRecursive(p.Item1, history, path);
		}

		private static float Distance(VoxelCoordinate start, VoxelCoordinate end)
		{
			return (start - end).ToVector3().magnitude;
		}

		public static IEnumerable<VoxelCoordinate> GetPath(this ISet<VoxelCoordinate> navmesh,
			VoxelCoordinate from, VoxelCoordinate to,
			PathfindingCostDelegate pathBehaviour = null)
		{
			bool collideCheck(VoxelCoordinate c) => navmesh != null && navmesh.CollideCheck(c, out _);
			if (collideCheck(to) || collideCheck(from))
			{
				return null;
			}

			var open = new SortedList<float, VoxelCoordinate>();
			var closed = new HashSet<VoxelCoordinate>();
			var bestDistances = new Dictionary<VoxelCoordinate, float>();
			var history = new Dictionary<VoxelCoordinate, (VoxelCoordinate, float)>();
			open.Add(Distance(from, to), from);
			(float, VoxelCoordinate) best = (float.MaxValue, from);
			while (open.Any() && closed.Count < 1000)
			{
				var nextNode = open.First();    // Get the highest scoring open node
				open.RemoveAt(0);
				var nextCoord = nextNode.Value;

				closed.Add(nextCoord);  // Close the coordinate
				foreach (var neighbour in nextCoord.GetNeighbours())
				{
					if (closed.Contains(neighbour))
					{
						// Don't re-explore
						continue;
					}

					if (collideCheck(neighbour))
					{
						// Something is blocking the way
						continue;
					}

					var cost = 0f;
					if (pathBehaviour != null)
					{
						cost = pathBehaviour(navmesh, nextCoord, neighbour);
					}
					if(cost > 1)
					{
						continue;
					}

					// Calculate heuristics
					var distanceToTarget = Distance(neighbour, to);
					if (!bestDistances.TryGetValue(nextCoord, out var distanceFromHome))
					{
						distanceFromHome = 0;
					}
					else
					{
						distanceFromHome += neighbour.GetScale();
					}

					if (neighbour == to)
					{
						// We're there - reconstruct the path
						history[to] = (nextCoord, distanceToTarget);
						return ReconstructPathRecursive(to, history, new List<VoxelCoordinate>() { to })
							.Reverse<VoxelCoordinate>();
					}

					// Otherwise, process this node into the open list
					// and calculate the two heuristics
					if (!open.ContainsValue(neighbour))
					{
						var h = distanceToTarget + distanceFromHome;
						h += cost;
						while (open.ContainsKey(h))
						{
							h += neighbour.GetScale() * 0.01f;
						}
						if(h < best.Item1)
						{
							best = (h, neighbour);
						}
						open.Add(h, neighbour);
					}

					if (history.TryGetValue(neighbour, out var bestScore) &&
						bestScore.Item2 < distanceToTarget)
					{
						continue;
					}
					bestDistances[neighbour] = distanceFromHome;
					history[neighbour] = (nextCoord, distanceToTarget);
				}
			}
			return ReconstructPathRecursive(best.Item2, history, new List<VoxelCoordinate>() { best.Item2 })
							.Reverse<VoxelCoordinate>(); ;
		}
	}
}
