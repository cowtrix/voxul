using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul
{
	[ExecuteAlways]
	public class VoxelText : VoxelRenderer
	{
		[Header("Text")]
		[Range(0, 2)]
		public sbyte Resolution;
		public Font Font;
		public int FontSize = 12;
		public int LineSize = 12;
		public FontStyle FontStyle;
		[Multiline]
		public string Text;

		public VoxelMaterial Material;
		[Range(0, 1)]
		public float AlphaThreshold = .5f;

		[SerializeField]
		[HideInInspector]
		private string m_lastText;
		[SerializeField]
		private Texture2D m_workingTexture;

		private void OnValidate()
		{
			if (Text == m_lastText)
			{
				Debug.Log("Text was the same, returning");
				return;
			}
			Debug.Log("Set dirty");
			SetDirty();
			m_lastText = Text;
		}

		private void OnEnable()
		{
			Font.textureRebuilt += OnFontTextureRebuilt;
		}

		private void OnDestroy()
		{
			Font.textureRebuilt -= OnFontTextureRebuilt;
		}

		private void OnFontTextureRebuilt(Font obj)
		{
			Debug.Log("OnFontTextureRebuilt");
		}

		public override void Invalidate(bool forceCollider)
		{
			Debug.Log("Invalidating");
			if (!Mesh || !Font)
			{
				base.Invalidate(forceCollider);
				return;
			}
			if (!Font.dynamic)
			{
				Font = Font.CreateDynamicFontFromOSFont(Font.name, FontSize);
			}

			Mesh.Voxels.Clear();

			// Generate a mesh for the characters we want to print.
			var fontTexture = (Texture2D)Font.material.GetTexture(Font.material.GetTexturePropertyNameIDs().First());
			if (!fontTexture)
			{
				Debug.LogError($"Texture was null for font {Font}", Font);
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

			var layerStep = VoxelCoordinate.LayerToScale(Resolution);
			var str = Text;
			Vector3 pos = Vector3.zero;
			for (int i = 0; i < str.Length; i++)
			{
				if(str[i] == '\n')
				{
					pos = new Vector2(0, pos.y - LineSize);
					continue;
				}
				// Get character rendering information from the font
				if (!GetCharacterInfo(Font.characterInfo, str[i], out var ch))
				{
					continue;
				}

				var spatialBound = new Bounds(pos + new Vector3(ch.minX, ch.maxY, 0), Vector3.zero);
				spatialBound.Encapsulate(pos + new Vector3(ch.maxX, ch.maxY, 0));
				spatialBound.Encapsulate(pos + new Vector3(ch.maxX, ch.minY, 0));
				spatialBound.Encapsulate(pos + new Vector3(ch.minX, ch.minY, 0));

				var uvBound = Rect.MinMaxRect(ch.uvBottomLeft.x, ch.uvBottomLeft.y, ch.uvTopRight.x, ch.uvTopRight.y);

				Debug.Log($"Drawing character {str[i]}");
				for (var x = 0f; x <= spatialBound.size.x; x += layerStep)
				{
					var fracX = x / spatialBound.size.x;
					for (var y = 0f; y <= spatialBound.size.y; y += layerStep)
					{
						var fracY = y / spatialBound.size.y;

						var uv = new Vector2(
							Mathf.Lerp(uvBound.xMin, uvBound.xMax, fracX),
							Mathf.Lerp(uvBound.yMin, uvBound.yMax, fracY));
						var c = m_workingTexture.GetPixelBilinear(uv.x, uv.y);

						if (c.a < AlphaThreshold)
						{
							continue;
						}
						else
						{
							var voxPos = new Vector3(x + spatialBound.min.x, y + spatialBound.min.y);
							if (Mesh.Voxels.AddSafe(new Voxel(VoxelCoordinate.FromVector3(voxPos, Resolution), Material.Copy())))
							{
								Debug.Log($"Adding voxel {ch.uvBottomLeft} at {voxPos}");
							}
						}
					}
				}

				// Advance character position
				Debug.Log("Advancing " + ch.advance);
				pos += new Vector3(ch.advance, 0, 0);
			}
			Mesh.Invalidate();
			base.Invalidate(forceCollider);
		}

		protected override void Update()
		{
			if (Font)
			{
				Font.RequestCharactersInTexture(Text, FontSize, FontStyle);
			}
			base.Update();
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

			var str = Text;
			Vector3 pos = Vector3.zero;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\n')
				{
					pos = new Vector2(0, pos.y - LineSize);
					continue;
				}
				// Get character rendering information from the font				
				if (!GetCharacterInfo(Font.characterInfo, str[i], out var ch))
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
	}
}