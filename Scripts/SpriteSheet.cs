using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul
{
    public interface ISpriteSheetProvider
    {
        SpriteSheet GetSpriteSheet();
    }

    [CreateAssetMenu(menuName = "voxul/New Spritesheet")]
    public class SpriteSheet : ScriptableObject
    {
        public int SpriteResolution = 64;
        public Texture2DArray AlbedoTextureArray, MaterialTextureArray;
        public List<Texture2D> AlbedoTextures = new List<Texture2D>();
        public List<Texture2D> MaterialTextures = new List<Texture2D>();

        [ContextMenu("Regenerate Spritesheet")]
        public void RegenerateSpritesheet()
        {
            AlbedoTextureArray = GenerateArray(AlbedoTextureArray, AlbedoTextures, TextureFormat.ARGB32, SpriteResolution, nameof(AlbedoTextureArray));
            MaterialTextureArray = GenerateArray(MaterialTextureArray, MaterialTextures, TextureFormat.ARGB32, SpriteResolution, nameof(MaterialTextureArray));
            this.TrySetDirty();
        }

        static Texture2D ResizeTexture(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }

        private Texture2DArray GenerateArray(Texture2DArray existing, IList<Texture2D> textures, TextureFormat format, int size, string name)
        {
            if (!textures.Any())
            {
                return null;
            }

            var texture2DArray = new Texture2DArray(size, size, textures.Count, format, false);
            texture2DArray.name = name;
            for (int i = 0; i < textures.Count; i++)
            {
                var tex = textures[i];
                if (tex.height != size || tex.width != size)
                {
                    tex = ResizeTexture(tex, size, size);
                }
                texture2DArray.SetPixels(tex.GetPixels(), i);
            }

            texture2DArray.Apply();
            texture2DArray.filterMode = FilterMode.Point;
            texture2DArray.wrapMode = TextureWrapMode.Repeat;

#if UNITY_EDITOR
            if (existing)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(existing);
            }
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(texture2DArray, assetPath);
            }
#endif

            if (existing)
            {
                existing.SafeDestroy();
            }

            return texture2DArray;
        }
    }
}