using System;
using System.Collections.Generic;
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
		private TextConfiguration m_lastConfig;
		[SerializeField]
		[HideInInspector]
		private VoxelTextWorker m_textWorker;

		protected override VoxelMeshWorker GetVoxelMeshWorker()
		{
			if(m_textWorker == null)
			{
				m_textWorker = new VoxelTextWorker();
			}
			return m_textWorker;
		}

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
				Mesh.CurrentWorker?.Clear();
			}
			m_lastConfig = JsonUtility.FromJson<TextConfiguration>(JsonUtility.ToJson(Configuration));
			SetDirty();
		}

		protected override void OnClear()
		{
			Mesh.Voxels.Clear();
			Mesh.CurrentWorker?.Clear();
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

		public override void Invalidate(bool force, bool forceCollider)
		{
			m_isDirty = false;
			UnityMainThreadDispatcher.EnsureSubscribed();
			if (Mesh && Configuration.Font)
			{
				if (!Configuration.Font.dynamic)
				{
					Configuration.Font = Font.CreateDynamicFontFromOSFont(Configuration.Font.name, Configuration.FontSize);
				}
				if (Configuration.Font)
				{
					Configuration.Font.RequestCharactersInTexture(Configuration.Text, Configuration.FontSize, Configuration.FontStyle);
				}
			}
			base.Invalidate(force, forceCollider);
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
				if (!VoxelTextWorker.GetCharacterInfo(Configuration.Font.characterInfo, str[i], out var ch))
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
			if(Mesh.CurrentWorker?.Dispatcher == this)
			{
				(Mesh.CurrentWorker as VoxelTextWorker)?.Clear();
			}
		}
	}
}