using UnityEngine;

namespace Voxul.Utilities
{
	public static class Util
	{
		public static void SafeDestroy(this Object obj)
		{
			if(obj == null || !obj)
			{
				return;
			}
			if (Application.isPlaying)
			{
				Object.Destroy(obj);
			}
			else
			{
				Object.DestroyImmediate(obj);
			}
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