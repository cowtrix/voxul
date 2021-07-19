using NUnit.Framework;

namespace Voxul.Test
{
	public class UtilityTests
	{
		[Test]
		public void CanGetVoxelManager()
		{
			Assert.NotNull(VoxelManager.Instance);
		}

		[Test]
		public void CanGetVoxelManagerDefaultResources()
		{
			Assert.NotNull(VoxelManager.Instance.DefaultMaterial);
			Assert.NotNull(VoxelManager.Instance.DefaultMaterialTransparent);
			Assert.IsNotEmpty(VoxelManager.Instance.DefaultOptimisers);
		}
	}
}