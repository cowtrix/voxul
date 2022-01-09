using System;
using System.Linq;
using UnityEngine;

namespace Voxul
{
	[Serializable]
	public class VoxelBrush
	{
		[Serializable]
		public class BrushDirectionOverride
		{
			public EVoxelDirection Direction;
			public SurfaceBrush Surface;
		}

		[Serializable]
		public class SurfaceBrush
		{
			public EUVMode UVMode;
			public Gradient Albedo;
			public Vector2 Metallic;
			public Vector2 Smoothness;

			public SurfaceBrush()
			{
			}

			public SurfaceBrush(SurfaceData surface)
			{
				UVMode = surface.UVMode;
				Metallic = new Vector2(surface.Metallic, surface.Metallic);
				Smoothness = new Vector2(surface.Smoothness, surface.Smoothness);
				Albedo = new Gradient
				{
					alphaKeys = new GradientAlphaKey[]
					{
						new GradientAlphaKey
						{
							alpha = surface.Albedo.a,
							time = 0,
						}
					},
					colorKeys = new GradientColorKey[]
					{
						new GradientColorKey
						{
							color = surface.Albedo,
							time = 0,
						}
					}
				};
			}

			public SurfaceData Generate(float value)
			{
				value = Mathf.Clamp01(value);
				return new SurfaceData
				{
					Albedo = Albedo.Evaluate(value),
					Metallic = Mathf.Lerp(Metallic.x, Metallic.y, value),
					Smoothness = Mathf.Lerp(Smoothness.x, Smoothness.y, value),
					UVMode = UVMode,
				};
			}

			public SurfaceBrush Copy()
			{
				return new SurfaceBrush
				{
					Albedo = Albedo,
					Metallic = Metallic,
					Smoothness = Smoothness,
					UVMode = UVMode,
				};
			}
		}

		public EMaterialMode MaterialMode;
		public ERenderMode RenderMode;
		public ENormalMode NormalMode;

		public SurfaceBrush Default;
		public BrushDirectionOverride[] Overrides;

		public VoxelBrush() { }

		public VoxelBrush(VoxelMaterial material)
		{
			RenderMode = material.RenderMode;
			MaterialMode = material.MaterialMode;
			NormalMode = material.NormalMode;
			Default = new SurfaceBrush(material.Default);
			Overrides = material.Overrides?.Select(o => new BrushDirectionOverride
			{
				Direction = o.Direction,
				Surface = new SurfaceBrush(o.Surface)
			}).ToArray();
		}

		public VoxelMaterial Generate(float value)
		{
			return new VoxelMaterial
			{
				MaterialMode = MaterialMode,
				RenderMode = RenderMode,
				NormalMode = NormalMode,
				Default = Default.Generate(value),
				Overrides = Overrides?.Select(o =>
				new DirectionOverride
				{
					Direction = o.Direction,
					Surface = o.Surface.Generate(value)
				}).ToArray(),
			};
		}

		public SurfaceBrush GetSurface(EVoxelDirection dir)
		{
			if (Overrides != null)
			{
				var ov = Overrides.Where(o => o.Direction == dir);
				if (ov.Any())
				{
					return ov.First().Surface;
				}
			}
			return Default;
		}

		public VoxelBrush Copy()
		{
			return new VoxelBrush
			{
				Default = Default,
				Overrides = Overrides?.Select(o => new BrushDirectionOverride
				{
					Direction = o.Direction,
					Surface = o.Surface.Copy()
				}).ToArray(),
				RenderMode = RenderMode,
				NormalMode = NormalMode,
				MaterialMode = MaterialMode,
			};
		}
	}
}