using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	public static class FontExtensions
	{
		public static char GetCharacter(this CharacterInfo info) => Convert.ToChar(info.index);
	}

	[ExecuteAlways]
	public class VoxelText : VoxelRenderer
	{
		[Serializable]
		public class CharVoxelData
		{
			public char Character;
			public List<VoxelCoordinate> Coordinates = new List<VoxelCoordinate>();
		}
		[Serializable]
		public class CharacterBoundsMapping : SerializableDictionary<Bounds, CharVoxelData> { }
		[Serializable]
		public class TextConfiguration
		{
			[Range(0, 2)]
			public sbyte Resolution = 1;
			public Font Font;
			public int FontSize = 12;
			public int LineSize = 12;
			public FontStyle FontStyle;
			[Multiline]
			public string Text = "Text";

			public VoxelMaterial Material;
			[Range(0, 1)]
			public float AlphaThreshold = .5f;

			public override bool Equals(object obj)
			{
				return obj is TextConfiguration configuration &&
					   Resolution == configuration.Resolution &&
					   EqualityComparer<Font>.Default.Equals(Font, configuration.Font) &&
					   FontSize == configuration.FontSize &&
					   LineSize == configuration.LineSize &&
					   FontStyle == configuration.FontStyle &&
					   Text == configuration.Text &&
					   EqualityComparer<VoxelMaterial>.Default.Equals(Material, configuration.Material) &&
					   AlphaThreshold == configuration.AlphaThreshold;
			}

			public bool ShouldClearCache(TextConfiguration lastConfig)
			{
				if(lastConfig == null)
				{
					return true;
				}
				return Resolution != lastConfig.Resolution ||
					   Font != lastConfig.Font ||
					   FontSize != lastConfig.FontSize ||
					   LineSize != lastConfig.LineSize ||
					   FontStyle != lastConfig.FontStyle ||
					   !Material.Equals(lastConfig.Material) ||
					   AlphaThreshold != lastConfig.AlphaThreshold;
			}

			public override int GetHashCode()
			{
				int hashCode = 525090847;
				hashCode = hashCode * -1521134295 + Resolution.GetHashCode();
				hashCode = hashCode * -1521134295 + EqualityComparer<Font>.Default.GetHashCode(Font);
				hashCode = hashCode * -1521134295 + FontSize.GetHashCode();
				hashCode = hashCode * -1521134295 + LineSize.GetHashCode();
				hashCode = hashCode * -1521134295 + FontStyle.GetHashCode();
				hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
				hashCode = hashCode * -1521134295 + Material.GetHashCode();
				hashCode = hashCode * -1521134295 + AlphaThreshold.GetHashCode();
				return hashCode;
			}
		}

		public TextConfiguration Configuration = new TextConfiguration();

		[SerializeField]
		[HideInInspector]
		private CharacterBoundsMapping m_cache = new CharacterBoundsMapping();
		[SerializeField]
		[HideInInspector]
		private TextConfiguration m_lastConfig;

		private Texture2D m_workingTexture;

		private void OnValidate()
		{
			if (m_lastConfig != null && m_lastConfig.Equals(Configuration))
			{
				return;
			}
			if (!Mesh)
			{
				return;
			}
			
			if (Configuration.ShouldClearCache(m_lastConfig))
			{
				Mesh.Voxels.Clear();
				m_cache.Clear();
			}
			m_lastConfig = JsonUtility.FromJson<TextConfiguration>(JsonUtility.ToJson(Configuration));
			SetDirty();
		}

		protected override void OnClear()
		{
			Mesh.Voxels.Clear();
			m_cache.Clear();
		}

		private void OnEnable()
		{
			if (!Mesh)
			{
				Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
				Mesh.name = Guid.NewGuid().ToString();
			}
			if (Configuration.Font)
			{
				Configuration.Font = Font.CreateDynamicFontFromOSFont(Configuration.Font.name, Configuration.FontSize);
			}
		}

		public IEnumerable<(Bounds, CharacterInfo)> GetCharacters()
		{
			var str = Configuration.Text;
			Vector3 pos = Vector3.zero;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\n')
				{
					pos = new Vector2(0, pos.y - Configuration.LineSize);
					continue;
				}

				// Get character rendering information from the font
				if (!GetCharacterInfo(Configuration.Font.characterInfo, str[i], out var ch))
				{
					continue;
				}

				var spatialBound = new Bounds(pos + new Vector3(ch.minX, ch.maxY, 0), Vector3.zero);
				spatialBound.Encapsulate(pos + new Vector3(ch.maxX, ch.maxY, 0));
				spatialBound.Encapsulate(pos + new Vector3(ch.maxX, ch.minY, 0));
				spatialBound.Encapsulate(pos + new Vector3(ch.minX, ch.minY, 0));

				yield return (spatialBound, ch);

				// Advance character position
				pos += new Vector3(ch.advance, 0, 0);
			}
		}

		private void UpdateFontWorkingTexture()
		{
			// Generate a mesh for the characters we want to print.
			var fontTexture = (Texture2D)Configuration.Font.material.GetTexture(Configuration.Font.material.GetTexturePropertyNameIDs().First());
			if (!fontTexture)
			{
				Debug.LogError($"Texture was null for font {Configuration.Font}", Configuration.Font);
				return;
			}

			var rt = RenderTexture.GetTemporary(fontTexture.width, fontTexture.height);
			if (m_workingTexture == null)
			{
				m_workingTexture = new Texture2D(fontTexture.width, fontTexture.height);
			}
			else if (m_workingTexture.width != fontTexture.width || m_workingTexture.height != fontTexture.height)
			{
				m_workingTexture.Resize(fontTexture.width, fontTexture.height);
			}

			Graphics.Blit(fontTexture, rt);
			RenderTexture.active = rt;
			m_workingTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(rt);
			m_workingTexture.Apply();
		}

		public override void Invalidate(bool forceCollider)
		{
			if (!ShouldInvalidate())
			{
				return;
			}
			if (!Mesh || !Configuration.Font)
			{
				base.Invalidate(forceCollider);
				return;
			}
			if (!Configuration.Font.dynamic)
			{
				Configuration.Font = Font.CreateDynamicFontFromOSFont(Configuration.Font.name, Configuration.FontSize);
			}
			if (Configuration.Font)
			{
				Configuration.Font.RequestCharactersInTexture(Configuration.Text, Configuration.FontSize, Configuration.FontStyle);
			}
			foreach (var m in Mesh.Meshes)
			{
				// Because it's text, we mark it dynamic
				if (m.Mesh)
				{
					m.Mesh.MarkDynamic();
				}
			}
			UpdateFontWorkingTexture();
			var newCache = new Dictionary<Bounds, CharVoxelData>();
			var characterCoordList = new List<VoxelCoordinate>();
			var layerStep = VoxelCoordinate.LayerToScale(Configuration.Resolution);
			foreach (var info in GetCharacters())
			{
				var spatialBound = info.Item1;
				var ch = info.Item2;
				var character = ch.GetCharacter();

				if (m_cache.TryGetValue(spatialBound, out var cacheData)
					&& cacheData.Character == character
					&& cacheData.Coordinates.Any(v => Mesh.Voxels.ContainsKey(v)))
				{
					newCache.Add(spatialBound, cacheData);
					continue;
				}

				characterCoordList.Clear();
				Mesh.Voxels.Remove(cacheData?.Coordinates);

				for (var x = 0f; x <= spatialBound.size.x; x += layerStep)
				{
					var fracX = x / spatialBound.size.x;
					for (var y = 0f; y <= spatialBound.size.y; y += layerStep)
					{
						var fracY = 1 - ( y / spatialBound.size.y);
						var uv = VectorExtensions.QuadLerp(
							ch.uvTopLeft, ch.uvTopRight, ch.uvBottomRight, ch.uvBottomLeft,
							fracX, fracY);
						
						var c = m_workingTexture.GetPixelBilinear(uv.x, uv.y);

						if (c.a < Configuration.AlphaThreshold)
						{
							continue;
						}
						else
						{
							var voxPos = new Vector3(x + spatialBound.min.x, y + spatialBound.min.y);
							var voxCoord = VoxelCoordinate.FromVector3(voxPos, Configuration.Resolution);
							characterCoordList.Add(voxCoord);
						}
					}
				}
				newCache.Add(spatialBound, new CharVoxelData { Character = character, Coordinates = characterCoordList.ToList() });
			}

			foreach (var c in m_cache.Where(m => !newCache.ContainsKey(m.Key)))
			{
				Mesh.Voxels.Remove(c.Key);
			}
			m_cache.Clear();
			foreach (var c in newCache)
			{
				m_cache.Add(c.Key, c.Value);
				foreach (var coord in c.Value.Coordinates)
				{
					Mesh.Voxels[coord] = new Voxel(coord, Configuration.Material.Copy());
				}
			}

			Mesh.Invalidate();
			base.Invalidate(forceCollider);
		}

		private bool GetCharacterInfo(CharacterInfo[] infos, char character, out CharacterInfo info)
		{
			foreach (var i in infos)
			{
				if (Convert.ToChar(i.index) == character)
				{
					info = i;
					return true;
				}
			}
			info = default;
			return false;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = Color.white.WithAlpha(.25f);
			if (!Configuration.Font)
			{
				return;
			}
			var str = Configuration.Text ?? "";
			Vector3 pos = Vector3.zero;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\n')
				{
					pos = new Vector2(0, pos.y - Configuration.LineSize);
					continue;
				}
				// Get character rendering information from the font				
				if (!GetCharacterInfo(Configuration.Font.characterInfo, str[i], out var ch))
				{
					continue;
				}
				var bound = new Bounds(pos + new Vector3(ch.minX, ch.maxY, 0), Vector3.zero);
				bound.Encapsulate(pos + new Vector3(ch.maxX, ch.maxY, 0));
				bound.Encapsulate(pos + new Vector3(ch.maxX, ch.minY, 0));
				bound.Encapsulate(pos + new Vector3(ch.minX, ch.minY, 0));
				Gizmos.DrawWireCube(bound.center, bound.size);
				// Advance character position
				pos += new Vector3(ch.advance, 0, 0);
			}
		}

		private void OnDestroy()
		{
			m_workingTexture.SafeDestroy();
		}
	}
}