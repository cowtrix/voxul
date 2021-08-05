using NUnit.Framework;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Test
{
	public class VoxelRendererTests
	{
        [Test]
        public void CanCreateNewVoxelRenderer()
        {
            var m = ScriptableObject.CreateInstance<VoxelMesh>();
            TestUtil.PopulateVoxelMesh(100, m);
            var r = new GameObject("TestRenderer")
                .AddComponent<VoxelRenderer>();

            // Set mesh and recalculate
            r.Mesh = m;
            r.Invalidate(false, false);
            Assert.That(!string.IsNullOrEmpty(m.Hash));

            var subMesh = r.Submeshes.First();
            Assert.NotNull(subMesh);
            Assert.NotNull(subMesh.MeshFilter);
            Assert.NotNull(subMesh.MeshRenderer);
            Assert.NotNull(subMesh.MeshCollider);
        }
    }
}