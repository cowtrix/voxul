using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public static class EditorPrefUtility
	{
		public static T GetPref<T>(string key, T defaultVal)
		{
			if (!EditorPrefs.HasKey(key))
			{
				return defaultVal;
			}
			return JsonUtility.FromJson<T>(EditorPrefs.GetString(key));
		}

		public static void SetPref<T>(string key, T val)
		{
			EditorPrefs.SetString(key, JsonUtility.ToJson(val));
		}

		public static bool GetPref(string key, bool defaultVal)
		{
			return EditorPrefs.GetBool(key, defaultVal);
		}

		public static void SetPref(string key, bool val)
		{
			EditorPrefs.SetBool(key, val);
		}

		public static sbyte GetPref(string key, sbyte defaultVal)
		{
			return (sbyte)EditorPrefs.GetInt(key, defaultVal);
		}

		public static void SetPref(string key, sbyte val)
		{
			EditorPrefs.SetInt(key, val);
		}
	}
}