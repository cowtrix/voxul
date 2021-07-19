using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public static class EditorPrefUtility
	{
		private static Dictionary<string, object> m_cache = new Dictionary<string, object>();

		public static T GetPref<T>(string key, T defaultVal)
		{
			if(m_cache.TryGetValue(key, out var cacheObj) && cacheObj is T result)
			{
				return result;
			}
			if (!EditorPrefs.HasKey(key))
			{
				return defaultVal;
			}
			var obj = JsonUtility.FromJson<T>(EditorPrefs.GetString(key));
			return obj;
		}

		public static void SetPref<T>(string key, T val)
		{
			if (m_cache.TryGetValue(key, out var cacheObj) 
				&& cacheObj is T result
				&& cacheObj.Equals(val))
			{
				return;
			}
			m_cache[key] = val;
			var json = JsonUtility.ToJson(val);
			EditorPrefs.SetString(key, json);
		}

		public static bool GetPref(string key, bool defaultVal)
		{
			return EditorPrefs.GetBool(key, defaultVal);
		}

		public static void SetPref(string key, bool val)
		{
			if (m_cache.TryGetValue(key, out var cacheObj)
				&& cacheObj.Equals(val))
			{
				return;
			}
			m_cache[key] = val;
			EditorPrefs.SetBool(key, val);
		}

		public static sbyte GetPref(string key, sbyte defaultVal)
		{
			return (sbyte)EditorPrefs.GetInt(key, defaultVal);
		}

		public static void SetPref(string key, sbyte val)
		{
			if (m_cache.TryGetValue(key, out var cacheObj)
				&& cacheObj.Equals(val))
			{
				return;
			}
			m_cache[key] = val;
			EditorPrefs.SetInt(key, val);
		}
	}
}