using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
    public class VoxelSprite : VoxelRenderer
    {
		public EMaterialMode MaterialMode;
		public float AlphaCuttoff = .5f;
        public Sprite Sprite;
        public sbyte Layer;
		
		public override void Invalidate(bool force, bool forceCollider)
		{
			if (!Sprite || !Mesh)
			{
				return;
			}
			if (!Sprite.texture.isReadable)
			{
				throw new System.Exception("Sprite must be readable.");
			}
			Mesh.Voxels.Clear();

			var startX = Mathf.RoundToInt(Sprite.rect.xMin);
			var endX = Mathf.RoundToInt(Sprite.rect.xMax);
			var startY = Mathf.RoundToInt(Sprite.rect.yMin);
			var endY = Mathf.RoundToInt(Sprite.rect.yMax);
			var width = endX - startX;
			var height = endY - startY;

			var pix = Sprite.texture.GetPixels(startX, startY, endX - startX, endY - startY);

			for (var u = 0; u < width; ++u)
			{
				for (var v = 0; v < height; ++v)
				{
					var index = v + u * width;
					var c = pix[index];
					if (MaterialMode == EMaterialMode.Opaque && c.a < AlphaCuttoff)
					{
						continue;
					}
					var coord = new VoxelCoordinate(u, v, 0, Layer);
					var mat = new VoxelMaterial
					{
						MaterialMode = MaterialMode,
						Default = new SurfaceData
						{
							Albedo = c,							
						}
					};
					var vox = new Voxel(coord, mat);
					Mesh.Voxels.Add(coord, vox);
				}
			}
			Mesh.Invalidate();
			base.Invalidate(force, forceCollider);
		}
	}
}