using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Testing
{
	public class TextBenchmark : MonoBehaviour
	{
		public int TextCount = 10;
		public Vector2 CharacterCount = new Vector2(2, 64);
		public Font Font;
		List<VoxelText> m_texts = new List<VoxelText>();

		private static string GenerateRandomAlphanumericString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			var random = new System.Random();
			var randomString = new string(Enumerable.Repeat(chars, length)
													.Select(s => s[random.Next(s.Length)]).ToArray());
			return randomString;
		}

		private void Start()
		{
			for (var i = 0; i < TextCount; ++i)
			{
				var t = new GameObject($"Text_{i}")
					.AddComponent<VoxelText>();
				t.transform.position = new Vector3(0, i * 10, 0);
				t.Configuration.Font = Font;
				t.GenerateCollider = false;
				m_texts.Add(t);
			}
		}

		// Update is called once per frame
		void Update()
		{
			foreach (var t in m_texts)
			{
				var str = GenerateRandomAlphanumericString((int)UnityEngine.Random.Range(CharacterCount.x, CharacterCount.y));
				t.Configuration.Text = str;
				t.Invalidate(false, false);
			}
		}
	}
}