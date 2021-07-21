using NUnit.Framework;
using Voxul.Utilities;

namespace Voxul.Test
{
	public class VoxelTreeTests
	{
        [Test]
        public void CanInsertIntoVoxelTree()
        {
            var tree = new VoxelTree<int>(0);
            var coord = TestUtil.RandomCoord;
            var inVal = 3;
            tree.Insert(coord, inVal);
            Assert.That(tree.TryGetValue(coord, out var outVal));
            Assert.AreEqual(inVal, outVal);
        }

    }
}