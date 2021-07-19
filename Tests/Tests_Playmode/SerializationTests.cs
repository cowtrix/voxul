using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Voxul;
using UnityEngine.TestTools;
using Voxul.Meshing;

namespace Voxul.Test
{
	public class SerializationTests
    {
        [Test]
        public void CanCompareVoxels()
        {
            var v = TestUtil.RandomVoxel;
            Assert.AreEqual(v, v);
            Assert.AreNotEqual(v, TestUtil.RandomVoxel);
        }

        [Test]
        public void CanCompareVoxelCoordinates()
        {
            var coord = TestUtil.RandomCoord;
            Assert.AreEqual(coord, coord);
            Assert.AreNotEqual(coord, TestUtil.RandomCoord);
        }

        [Test]
        public void CanCompareVoxelMaterials()
        {
            var mat = TestUtil.RandomMat;
            Assert.AreEqual(mat, mat);
            Assert.AreNotEqual(mat, TestUtil.RandomMat);
        }

        [Test]
        public void CanCompareVoxelSurface()
        {
            var surf = TestUtil.RandomSurf;
            Assert.AreEqual(surf, surf);
            Assert.AreNotEqual(surf, TestUtil.RandomSurf);
        }

        [Test]
        public void CanSerializeVoxelMapping()
        {
            var mapping = new VoxelMapping();
            var vox = TestUtil.RandomVoxel;
            mapping.AddSafe(vox);
            mapping.OnBeforeSerialize();
            mapping.OnAfterDeserialize();
            Assert.That(mapping.ContainsKey(vox.Coordinate));
            Assert.That(mapping.ContainsValue(vox));
        }
    }
}