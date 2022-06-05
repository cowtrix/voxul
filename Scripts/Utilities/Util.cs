using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Voxul.Utilities
{
    public static class Util
    {
        public static IEnumerable<Bounds> GetOptimisedVoxelBounds(IEnumerable<VoxelCoordinate> coords)
        {
            var rawBounds = coords.Select(b => b.ToBounds()).ToList();
            var optimizedBounds = new List<Bounds>();
            var optimizationFoundThisIteration = false;
            do
            {
                optimizedBounds.Clear();
                optimizationFoundThisIteration = false;
                for (var i = 0; i < rawBounds.Count; i++)
                {
                    var bound = rawBounds[i];
                    var coord = coords.ElementAt(i);

                    if (optimizedBounds.Count == 0)
                    {
                        optimizedBounds.Add(bound);
                        continue;
                    }

                    var optimisationFound = false;
                    for (int j = optimizedBounds.Count - 1; j >= 0; j--)
                    {
                        Bounds optimisedBound = optimizedBounds[j];
                        if (optimisedBound.center.x != bound.center.x && optimisedBound.center.y != bound.center.y && optimisedBound.center.z != bound.center.z)
                        {
                            continue;
                        }

                        var closestPoint = optimisedBound.ClosestPoint(coord.ToVector3());
                        if (Vector3.Distance(closestPoint, coord.ToVector3()) >= VoxelCoordinate.LayerToScale(coord.Layer))
                        {
                            continue;
                        }

                        var expandedBound = optimisedBound;
                        expandedBound.Encapsulate(bound);

                        var largerCount = 0;
                        if (expandedBound.size.x > bound.size.x)
                        {
                            largerCount++;
                        }
                        if (expandedBound.size.y > bound.size.y)
                        {
                            largerCount++;
                        }
                        if (expandedBound.size.z > bound.size.z)
                        {
                            largerCount++;
                        }

                        if (expandedBound.size.x > optimisedBound.size.x)
                        {
                            largerCount++;
                        }
                        if (expandedBound.size.y > optimisedBound.size.y)
                        {
                            largerCount++;
                        }
                        if (expandedBound.size.z > optimisedBound.size.z)
                        {
                            largerCount++;
                        }
                        if (largerCount > 2)
                        {
                            continue;
                        }

                        optimizedBounds.RemoveAt(j);
                        optimizedBounds.Add(expandedBound);
                        optimisationFound = true;
                        optimizationFoundThisIteration = true;
                        break;
                    }
                    if (!optimisationFound)
                    {
                        optimizedBounds.Add(bound);
                    }
                }
                rawBounds = optimizedBounds.ToList();
            } while (optimizationFoundThisIteration);

            
            return optimizedBounds;
        }

        public static Bounds GetEncompassingBounds(this IEnumerable<Bounds> enumerable)
        {
            if (enumerable == null || !enumerable.Any())
            {
                return default;
            }
            var b = enumerable.First();
            foreach (var b2 in enumerable.Skip(1))
            {
                b.Encapsulate(b2);
            }
            return b;
        }

        public static bool PromptEditor(string title, string message, string ok = "Okay", string cancel = "Cancel")
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog(title, message, ok, cancel);
#else
			return true;
#endif
        }

        public static void TrySetDirty(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }

        public static IList<T> AddRange<T>(this IList<T> list, params T[] values)
        {
            foreach (var i in values)
            {
                list.Add(i);
            }
            return list;
        }


        public static T Random<T>(this IList<T> array)
        {
            if (array.Count == 0)
            {
                throw new Exception("Check for empty arrays before calling this!");
            }
            if (array.Count == 1)
            {
                return array[0];
            }
            var rnd = new System.Random();
            return array[rnd.Next(0, array.Count())];
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> collection, Func<T, object> selector)
        {
            var history = new HashSet<object>();
            foreach (var element in collection)
            {
                var selection = selector(element);
                if (history.Contains(selection))
                {
                    continue;
                }
                history.Add(selection);
                yield return element;
            }
        }

        public static void CopyTo<T>(this T source, T target)
        {
            foreach (var f in source.GetType().GetFields())
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
            foreach (var c in cols)
            {
                result += c;
                count++;
            }
            return result / count;
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> collection)
        {
            var hash = new HashSet<T>();
            foreach (var item in collection)
            {
                hash.Add(item);
            }
            return hash;
        }

        public static void SafeDestroy(this UnityEngine.Object obj)
        {
            if (obj == null || !obj)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj, true);
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

        public static Texture2D RenderTex(Vector3 origin, Quaternion rot, Vector3 objSize, Vector2 imgSize, LayerMask cullingLayers)
        {
            var w = Mathf.RoundToInt(imgSize.x);
            var h = Mathf.RoundToInt(imgSize.x);
            var rt = RenderTexture.GetTemporary(w, h, 16, RenderTextureFormat.ARGB32);

            var tmpCam = new GameObject("tmpCam").AddComponent<Camera>();

            tmpCam.cullingMask = cullingLayers;
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