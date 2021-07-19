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

        private const int RANDOM_COORD_RANGE = 2000;
        public static VoxelCoordinate RandomCoord =>
            new VoxelCoordinate
            {
                X = Random.Range(-RANDOM_COORD_RANGE, RANDOM_COORD_RANGE),    // TODO bigger values
                Y = Random.Range(-RANDOM_COORD_RANGE, RANDOM_COORD_RANGE),
                Z = Random.Range(-RANDOM_COORD_RANGE, RANDOM_COORD_RANGE),
                Layer = (sbyte)Random.Range(-10, 10)
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