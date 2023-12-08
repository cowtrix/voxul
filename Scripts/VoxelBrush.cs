using System;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

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
			public SerializableGradient Albedo;
			[MinMax(0, 1)]
			public Vector2 Metallic;
			[MinMax(0, 1)]
			public Vector2 Smoothness;
			[MinMax(0, 1)]
			public Vector2 TextureFade;
            public TextureIndex TextureIndex;

            public static SerializableGradient DefaultGradient => new SerializableGradient
			{
				alphaKeys = new SerializableGradient.AlphaKey[]
					{
						new SerializableGradient.AlphaKey
						{
							Alpha = 1,
							Time = 0,
						}
					},
				colorKeys = new SerializableGradient.ColorKey[]
					{
						new SerializableGradient.ColorKey
						{
							Color = Color.white,
							Time = 0,
						}
					}
			};

			public SurfaceBrush()
			{
				Albedo = DefaultGradient;
			}

			public SurfaceBrush(SurfaceData surface)
			{
				UVMode = surface.UVMode;
				Metallic = new Vector2(surface.Metallic, surface.Metallic);
				Smoothness = new Vector2(surface.Smoothness, surface.Smoothness);
				Albedo = new SerializableGradient
				{
					alphaKeys = new SerializableGradient.AlphaKey[]
					{
						new SerializableGradient.AlphaKey
						{
							Alpha = surface.Albedo.a,
							Time = 0,
						}
					},
					colorKeys = new SerializableGradient.ColorKey[]
					{
						new SerializableGradient.ColorKey
						{
							Color = surface.Albedo,
							Time = 0,
						}
					}
				};
				TextureFade = new Vector2(surface.TextureFade, surface.TextureFade);
				TextureIndex = surface.Texture;
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
					TextureFade = Mathf.Lerp(TextureFade.x, TextureFade.y, value),
					Texture = TextureIndex,
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
					TextureFade = TextureFade,
                    TextureIndex = TextureIndex,
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
				}).ToList(),
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