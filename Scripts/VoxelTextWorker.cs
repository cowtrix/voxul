using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	/// <summary>
	/// The VoxelTextWorker is responsible for taking the VoxelText.Configuration data
	/// and transforming it into the necessary voxel data, before calling the base class
	/// to generate the unity mesh.
	/// </summary>
	[Serializable]
	public class VoxelTextWorker : VoxelMeshWorker
	{
		[Serializable]
		public class CharVoxelData
		{
			public char Character;
			public List<VoxelCoordinate> Coordinates = new List<VoxelCoordinate>();
		}

		[Serializable]
		public class CharacterBoundsMapping : SerializableDictionary<Bounds, CharVoxelData> { }

		/// <summary>
		/// In order to sample the texture off-thread, we copy the alpha to a float array.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		private Texture2D m_workingTexture;
		private float[,] m_workingTextureAlpha;

		[SerializeField]
		[HideInInspector]
		private CharacterBoundsMapping m_cache = new CharacterBoundsMapping();

		/// <summary>
		/// We cache the character -> voxel list so we don't have to update
		/// text that hasn't moved. Makes redrawing large paragraphs lots
		/// faster.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		private List<CharacterInfo> m_charInfo = new List<CharacterInfo>();

		public VoxelText.TextConfiguration Configuration { get; set; }

		/// <summary>
		/// This is a semaphore indicating the progress of UpdateFontWorkingTexture()
		/// 0	- clear
		/// > 0 - not clear
		/// TODO: replace with semaphoreslim
		/// </summary>
		private int m_fontUpdate;

		public VoxelTextWorker(VoxelMesh mesh) : base(mesh)
		{
		}

		/// <summary>
		/// There are two reasons we need to do this bit of work before we can access the font texture data:
		///		1 - The font texture isn't readable so we can't sample it
		///		2 - You can't sample Texture objects from another thread anyway so we need to copy it
		/// </summary>
		public void UpdateFontWorkingTexture()
		{
			Interlocked.Increment(ref m_fontUpdate);

			if (!Configuration.Font)
			{
				// Nothing to do
				m_fontUpdate = 0;
				return;
			}

			voxulLogger.Debug("Began UpdateFontWorkingTexture for font " + Configuration.Font);
			// Generate a mesh for the characters we want to print.
			var fontTexture = (Texture2D)Configuration.Font.material.GetTexture(Configuration.Font.material.GetTexturePropertyNameIDs().First());
			if (!fontTexture)
			{
				m_fontUpdate = 0;
				voxulLogger.Error($"Texture was null for font {Configuration.Font}, this shouldn't really happen.", Configuration.Font);
				return;
			}

			// Blit the font texture to a temporary working texture so we can use things like GetPixels
			var rt = RenderTexture.GetTemporary(fontTexture.width, fontTexture.height);
			if (m_workingTexture == null)
			{
				m_workingTexture = new Texture2D(fontTexture.width, fontTexture.height);
			}
			else if (m_workingTexture.width != fontTexture.width || m_workingTexture.height != fontTexture.height)
			{
				m_workingTexture.Resize(fontTexture.width, fontTexture.height);
			}
			if(m_workingTextureAlpha == null || m_workingTextureAlpha.GetLength(0) != m_workingTexture.width || m_workingTextureAlpha.GetLength(1) != m_workingTexture.height)
			{
				m_workingTextureAlpha = new float[m_workingTexture.width, m_workingTexture.height];
			}
			Graphics.Blit(fontTexture, rt);
			RenderTexture.active = rt;
			m_workingTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(rt);
			m_workingTexture.Apply();

			// Copy texture alpha channel to float array for off-thread access
			for(var x = 0; x < m_workingTexture.width; ++x)
			{
				for (var y = 0; y < m_workingTexture.height; ++y)
				{
					m_workingTextureAlpha[x, y] = m_workingTexture.GetPixel(x, y).a;
				}
			}

			// Add the character infos for off-thread access
			if(m_charInfo == null)
			{
				m_charInfo = new List<CharacterInfo>();
			}
			m_charInfo.Clear();
			m_charInfo.AddRange(Configuration.Font.characterInfo);
			m_fontUpdate = 0;	// Release int-semaphore
			voxulLogger.Debug("Finished UpdateFontWorkingTexture");
		}

		public IEnumerable<(Bounds, CharacterInfo)> GetCharacters(string str, float lineSize)
		{
			Vector3 pos = Vector3.zero;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\n')
				{
					pos = new Vector2(0, pos.y - lineSize);
					continue;
				}

				// Get character rendering information from the font
				if (!GetCharacterInfo(m_charInfo, str[i], out var ch))
				{
					voxulLogger.Warning($"Couldn't find character info for char {str[i]} @ {i}");
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

		public static bool GetCharacterInfo(IEnumerable<CharacterInfo> infos, char character, out CharacterInfo info)
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

		protected override IEnumerator GenerateMesh(EThreadingMode mode, CancellationToken token, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
		{
			voxulLogger.Debug("Started VoxelGenerateMesh step");
			var timeLim = m_maxCoroutineUpdateTime;
			var sw = Stopwatch.StartNew();
			if (mode == EThreadingMode.Task)
			{
				Interlocked.Increment(ref m_fontUpdate);
				UnityMainThreadDispatcher.Enqueue(() => UpdateFontWorkingTexture());
				while(m_fontUpdate > 0)
				{
					yield return null;
				}
			}
			else
			{
				UpdateFontWorkingTexture();
			}
			var newCache = new Dictionary<Bounds, CharVoxelData>();
			var characterCoordList = new List<VoxelCoordinate>();
			var layerStep = VoxelCoordinate.LayerToScale(Configuration.Resolution);
			foreach (var info in GetCharacters(Configuration.Text, Configuration.LineSize))
			{
				var spatialBound = info.Item1;
				var ch = info.Item2;
				var character = ch.GetCharacter();

				if (m_cache.TryGetValue(spatialBound, out var cacheData)
					&& cacheData.Character == character
					&& cacheData.Coordinates.Any(v => VoxelMesh.Voxels.ContainsKey(v)))
				{
					newCache.Add(spatialBound, cacheData);
					continue;
				}

				characterCoordList.Clear();
				VoxelMesh.Voxels.Remove(cacheData?.Coordinates);

				for (var x = 0f; x <= spatialBound.size.x; x += layerStep)
				{
					var fracX = x / spatialBound.size.x;
					for (var y = 0f; y <= spatialBound.size.y; y += layerStep)
					{
						var fracY = 1 - (y / spatialBound.size.y);
						var uv = VectorExtensions.QuadLerp(
							ch.uvTopLeft, ch.uvTopRight, ch.uvBottomRight, ch.uvBottomLeft,
							fracX, fracY);

						var c = m_workingTextureAlpha.GetBilinear(uv.x, uv.y);

						if (c < Configuration.AlphaThreshold)
						{
							continue;
						}
						else
						{
							var voxPos = new Vector3(x + spatialBound.min.x, y + spatialBound.min.y);
							var voxCoord = VoxelCoordinate.FromVector3(voxPos, Configuration.Resolution);
							characterCoordList.Add(voxCoord);
						}

						if (mode == EThreadingMode.Coroutine && sw.Elapsed.TotalSeconds > timeLim)
						{
							sw.Restart();
							yield return null;
						}
						if (token.IsCancellationRequested)
						{
							yield break;
						}
					}
				}
				newCache.Add(spatialBound, new CharVoxelData { Character = character, Coordinates = characterCoordList.ToList() });
			}
			foreach (var c in m_cache.Where(m => !newCache.ContainsKey(m.Key)))
			{
				VoxelMesh.Voxels.Remove(c.Key);
			}
			lock (m_cache)
			{
				m_cache.Clear();
				foreach (var c in newCache)
				{
					m_cache.Add(c.Key, c.Value);
					foreach (var coord in c.Value.Coordinates)
					{
						VoxelMesh.Voxels[coord] = new Voxel(coord, Configuration.Material.Copy());
					}
				}
			}
			voxulLogger.Debug("Finished VoxelGenerateMesh step");

			var baseCoroutine = base.GenerateMesh(mode, token, minLayer, maxLayer);
			while (baseCoroutine.MoveNext())
			{
				yield return baseCoroutine.Current;
			}
		}

		public override void Clear()
		{
			lock (m_cache)
			{
				m_cache.Clear();
			}
			m_workingTexture.SafeDestroy();
			base.Clear();
		}
	}
}