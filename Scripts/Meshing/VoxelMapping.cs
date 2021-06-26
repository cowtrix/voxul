
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	[Serializable]
	public class VoxelMapping : SerializableDictionary<VoxelCoordinate, Voxel>
	{
		public bool AddSafe(Voxel vox)
		{
			if (Keys.CollideCheck(vox.Coordinate, out var hit))
			{
				//Debug.LogWarning($"Voxel {vox.Coordinate} collided with {hit} and so was skipped");
				return false;
			}
			Add(vox.Coordinate, vox);
			return true;
		}

		public void SetSafe(Voxel vox)
		{
			while (Keys.CollideCheck(vox.Coordinate, out var hit))
			{
				Remove(hit);
			}
			Add(vox.Coordinate, vox);
		}

		public void Remove(Bounds bounds)
		{
			var toRemove = this.Where(v => v.Key.ToBounds().Intersects(bounds))
				.ToList();
			foreach(var v in toRemove)
			{
				Remove(v.Key);
			}
		}

		public void Remove(IEnumerable<VoxelCoordinate> coordinates)
		{
			if(coordinates == null)
			{
				return;
			}
			foreach (var v in coordinates)
			{
				Remove(v);
			}
		}
	}
}