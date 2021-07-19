using UnityEngine;
using System.Linq;
using Voxul.Meshing;

namespace Voxul.Test
{
	public static class TestUtil
	{
        public static void PopulateVoxelMesh(int randomVoxelCount, VoxelMesh mesh)
		{
            for(var i = 0; i < randomVoxelCount; ++i)
			{
                mesh.Voxels.AddSafe(RandomVoxel);
			}
            mesh.Invalidate();
		}

        public static T RandomEnum<T>() where T: System.Enum
		{
            var values = System.Enum.GetValues(typeof(T));
            return values.Cast<T>()
                .ElementAt(Random.Range(0, values.Length));
		}

        public static VoxelCoordinate RandomCoord =>
            new VoxelCoordinate
            {
                X = Random.Range(-200, 200),    // TODO bigger values
                Y = Random.Range(-200, 200),
                Z = Random.Range(-200, 200),
                Layer = (sbyte)Random.Range(-20, 20)
            };

        public static SurfaceData RandomSurf =>
            new SurfaceData
            {
                Albedo = Random.ColorHSV(),
                Metallic = Random.value,
                Smoothness = Random.value,
                TextureFade = Random.value,
                UVMode = RandomEnum<EUVMode>(),
            };

        public static VoxelMaterial RandomMat =>
            new VoxelMaterial
            {
                Default = RandomSurf,
                Overrides = new DirectionOverride[]
                {
                   new DirectionOverride
				   {
                       Direction = RandomEnum<EVoxelDirection>(),
                       Surface = RandomSurf,
				   }
                },

                MaterialMode = RandomEnum<EMaterialMode>(),
                NormalMode = RandomEnum<ENormalMode>(),
                RenderMode = RandomEnum<ERenderMode>(),
            };

        public static Voxel RandomVoxel =>
            new Voxel
            {
                Coordinate = RandomCoord,
                Material = RandomMat,
            };
    }
}