using UnityEngine;

namespace Voxul
{
	public class PerlinNoiseGenerator : DynamicVoxelGenerator
	{
		public Bounds Bounds;
		public AnimationCurve Falloff = AnimationCurve.Linear(0, 1, 1, 0);
		[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
		public sbyte Layer;
		public float Frequency = 1;
		public float Magnitude = 1;
		public int Octaves = 3;
		[Range(0, 1)]
		public float Cuttoff;
		public VoxelBrush Brush;

		protected override void SetVoxels(VoxelRenderer renderer)
		{
			var randomOffset = new Vector3(Random.Range(-5000, 5000), Random.Range(-5000, 5000), Random.Range(-5000, 5000));
			var layerScale = VoxelCoordinate.LayerToScale(Layer);
			for(var x = Bounds.min.x; x < Bounds.min.x + Bounds.size.x; x += layerScale)
			{
				for (var y = Bounds.min.x; y < Bounds.min.y + Bounds.size.y; y += layerScale)
				{
					for (var z = Bounds.min.x; z < Bounds.min.z + Bounds.size.z; z += layerScale)
					{
						var value = Perlin.Fbm(x * Frequency + randomOffset.x, y * Frequency + randomOffset.y, z * Frequency + randomOffset.z, Octaves)
							* Magnitude;
						var vec3 = new Vector3(x, y, z);
						var cuttoffFactor = Falloff.Evaluate((vec3 - Bounds.center).magnitude / Bounds.extents.magnitude);
						var scaledCuttoff = 1 -  Mathf.Clamp01(Cuttoff * cuttoffFactor);
						if (value < scaledCuttoff)
						{
							continue;
						}
						
						var material = Brush.Generate(value);
						var voxelCoordinate = VoxelCoordinate.FromVector3(x, y, z, Layer);
						renderer.Mesh.Voxels.AddSafe(new Voxel(voxelCoordinate, material));
					}
				}
			}
		}
	}
}
