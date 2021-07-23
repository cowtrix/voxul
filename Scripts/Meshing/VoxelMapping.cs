
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
		public VoxelMapping() { }

		public VoxelMapping(IEnumerable<Voxel> voxels)
		{
			foreach(var v in voxels)
			{
				this[v.Coordinate] = v;
			}
		}

		public VoxelMapping(IEnumerable<KeyValuePair<VoxelCoordinate, Voxel>> voxels)
		{
			if(voxels == null)
			{
				return;
			}
			foreach (var v in voxels)
			{
				this[v.Key] = v.Value;
			}
		}

		public bool AddSafe(Voxel vox)
		{
			if (Keys.CollideCheck(vox.Coordinate, out var hit))
			{
				//voxulLogger.Warning($"Voxel {vox.Coordinate} collided with {hit} and so was skipped");
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