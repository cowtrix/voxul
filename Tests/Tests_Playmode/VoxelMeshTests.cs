using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;
using Voxul.Meshing;

namespace Voxul.Test
{
	public class VoxelMeshTests
	{
		[UnityTest]
		public IEnumerator CanRebakeSingleThreaded()
		{
			var it = CanRebakeVoxelMeshInMode(Utilities.EThreadingMode.SingleThreaded);
			while (it.MoveNext())
			{
				yield return it.Current;
			}
		}

		[UnityTest]
		public IEnumerator CanRebakeCoroutine()
		{
			var it = CanRebakeVoxelMeshInMode(Utilities.EThreadingMode.Coroutine);
			while (it.MoveNext())
			{
				yield return it.Current;
			}
		}

		[UnityTest]
		public IEnumerator CanRebakeTask()
		{
			var it = CanRebakeVoxelMeshInMode(Utilities.EThreadingMode.Task);
			while (it.MoveNext())
			{
				yield return it.Current;
			}
		}

		private IEnumerator CanRebakeVoxelMeshInMode(Utilities.EThreadingMode mode)
		{
			var completeEventFired = false;
			void CurrentWorker_OnCompleted(VoxelMeshWorker worker, VoxelMesh mesh)
			{
				completeEventFired = true;
			}

			var m = ScriptableObject.CreateInstance<VoxelMesh>();
			TestUtil.PopulateVoxelMesh(100, m);
			Assert.That(m.Voxels.Count > 0);

			m.CurrentWorker = new VoxelMeshWorker(m);
			m.CurrentWorker.OnCompleted += CurrentWorker_OnCompleted;
			m.CurrentWorker.GenerateMesh(mode);

			while (m.CurrentWorker.IsRecalculating)
			{
				yield return null;
			}

			Assert.True(completeEventFired);
			Assert.That(m.UnityMeshInstances.Count > 0);
			var unityMesh = m.UnityMeshInstances.First().UnityMesh;
			Assert.NotNull(unityMesh);
			Assert.That(unityMesh.vertexCount > 0);
		}
	}
}