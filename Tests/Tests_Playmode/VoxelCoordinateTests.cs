using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Voxul.Test
{

	public class VoxelCoordinateTests
	{
		[Test]
		public void CanAdd()
		{
			var first = TestUtil.RandomCoord;
			var second = TestUtil.RandomCoord;
			var result = first + second;

			Assert.AreEqual(result.Layer, Mathf.Max(first.Layer, second.Layer));

			var vec1 = first.ToVector3() + second.ToVector3();
			var vec2 = result.ToVector3();
			Assert.True(vec1 == vec2, $"{first} + {second}\n{vec1} != {vec2}");
		}

		[Test]
		public void CanSubtract()
		{
			var first = TestUtil.RandomCoord;
			var second = TestUtil.RandomCoord;
			var result = first - second;

			Assert.AreEqual(result.Layer, Mathf.Max(first.Layer, second.Layer));

			var vec1 = first.ToVector3() - second.ToVector3();
			var vec2 = result.ToVector3();
			Assert.True(vec1 == vec2, $"{first} - {second}\n{vec1} != {vec2}");
		}

		[Test]
		public void CanSubdivide()
		{
			var coord = TestUtil.RandomCoord;
			var bounds = coord.ToBounds();
			var subdivision = coord.Subdivide().ToList();
			var layerRatio = VoxelManager.Instance.LayerRatio;
			Assert.AreEqual(layerRatio * layerRatio * layerRatio, subdivision.Count);
			foreach (var sub in subdivision)
			{
				var subBounds = sub.ToBounds();
				Assert.True(bounds.Intersects(subBounds), $"Bounds {bounds} did not intersect {subBounds}");
			}
		}
	}
}