using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;
using System;

namespace Voxul.Utilities
{
	public static class Util
	{
		public static void CopyTo<T>(this T source, T target)
		{
			foreach(var f in source.GetType().GetFields())
			{
				f.SetValue(target, f.GetValue(source));
			}
		}

		public static string CamelcaseToSpaces(this string str)
		{
			return Regex.Replace(str, "(\\B[A-Z])", " $1");
		}

		public static void Swap<T>(ref T first, ref T second)
		{
			var tmp = first;
			first = second;
			second = tmp;
		}

		public static Color AverageColor(this IEnumerable<Color> cols)
		{
			var result = Color.clear;
			int count = 0;
			foreach(var c in cols)
			{
				result += c;
				count++;
			}
			return result / count;
		}

		public static ISet<T> ToSet<T>(this IEnumerable<T> collection)
		{
			var hash = new HashSet<T>();
			foreach(var item in collection)
			{
				hash.Add(item);
			}
			return hash;
		}

		public static void SafeDestroy(this UnityEngine.Object obj)
		{
			if(obj == null || !obj)
			{
				return;
			}
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(obj);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(obj);
			}
		}

		public static double GetDynamicTime()
		{
			return (
#if UNITY_EDITOR
			!UnityEditor.EditorApplication.isPlaying ? UnityEditor.EditorApplication.timeSinceStartup :
#endif
				Time.timeAsDouble);
		}

		public static Color WithAlpha(this Color c, float a)
		{
			return new Color(c.r, c.g, c.b, a);
		}

		public static T GetOrAddComponent<T>(this GameObject child) where T : Component
		{
			T result = child.GetComponent<T>();
			if (result == null)
			{
				result = child.AddComponent<T>();
			}
			return result;
		}

		public static Texture2D RenderTex(Vector3 origin, Quaternion rot, Vector3 objSize, Vector2 imgSize)
		{
			var w = Mathf.RoundToInt(imgSize.x);
			var h = Mathf.RoundToInt(imgSize.x);
			var rt = RenderTexture.GetTemporary(w, h, 16, RenderTextureFormat.ARGB32);

			var tmpCam = new GameObject("tmpCam").AddComponent<Camera>();
			tmpCam.nearClipPlane = .01f;
			tmpCam.farClipPlane = objSize.z * 2f;
			tmpCam.aspect = objSize.x / objSize.y;
			tmpCam.clearFlags = CameraClearFlags.Color;
			tmpCam.backgroundColor = Color.clear;
			tmpCam.orthographic = true;
			tmpCam.transform.position = origin;
			tmpCam.transform.rotation = rot;
			tmpCam.orthographicSize = objSize.y / 2f;
			tmpCam.targetTexture = rt;
			tmpCam.cullingMask = 1 << 31;

			var l = tmpCam.gameObject.AddComponent<Light>();
			l.type = LightType.Directional;

			tmpCam.Render();
			RenderTexture.active = rt;
			var tex = new Texture2D(w, h);
			tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			RenderTexture.active = null;
			tmpCam.targetTexture = null;
			RenderTexture.ReleaseTemporary(rt);
			tmpCam.gameObject.SafeDestroy();
			//tmpCam.gameObject.SetActive(false);
			return tex;
		}
	}
}