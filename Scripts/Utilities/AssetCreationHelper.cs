using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Voxul.Utilities
{

#if UNITY_EDITOR

	public static class AssetCreationHelper
    {
        private static Assembly UnityEditor;
        private static System.Type ProjectBrowser;
        private static System.Type ProjectWindowUtil;
        private static EditorWindow ProjectWindow;
        private static MethodInfo FrameObjectInProjectWindow;

        static AssetCreationHelper()
        {
			UnityEditor = Assembly.Load("UnityEditor");
			ProjectBrowser = UnityEditor.GetType("UnityEditor.ProjectBrowser");
			ProjectWindow = EditorWindow.GetWindow(ProjectBrowser);
			ProjectWindowUtil = UnityEditor.GetType("UnityEditor.ProjectWindowUtil");
			FrameObjectInProjectWindow =
			ProjectWindowUtil.GetMethod("FrameObjectInProjectWindow",
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.Instance |
                                                          BindingFlags.Static);
        }

        public static string CreateAssetInCurrentDirectory(UnityEngine.Object asset, string nameWithExtension, bool overwrite = false)
        {
            return CreateAssetInCurrentDirectory<UnityEngine.Object>(asset, nameWithExtension, overwrite);
        }

        public static string CreateAssetInCurrentDirectory(string asset, string nameWithExtension, bool overwrite = false)
        {
            return CreateAssetInCurrentDirectory<string>(asset, nameWithExtension, overwrite);
        }

        private static string CreateAssetInCurrentDirectory<T>(object asset, string nameWithExtension, bool overwrite)
        {
            string path = GetCurrentAssetDirectoryPathRelative() + "/" + nameWithExtension;

            if (!overwrite)
            {
                path = CorrectAssetNameToAvoidOverwrite(path);
            }

            if (typeof(T) == typeof(UnityEngine.Object))
            {
                AssetDatabase.CreateAsset((UnityEngine.Object)asset, path);
            }
            else
            {
                System.IO.File.WriteAllText(path, (string)asset);
            }

            AssetDatabase.Refresh();

            StartToRenameAsset(path);

            return path;
        }

        public static string GetCurrentAssetDirectoryPathAbsolute()
        {
            string path = Application.dataPath;

            foreach (UnityEngine.Object asset in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string tempPath = AssetDatabase.GetAssetPath(asset);

                if (System.IO.File.Exists(path))
                {
                    path += "/" + System.IO.Path.GetDirectoryName(tempPath);
                    break;
                }
            }

            return path;
        }

        public static string GetCurrentAssetDirectoryPathRelative()
        {
            // WARNING:
            // "Application.dataPath" returns absolute path
            // and the following code makes a relative path.
            // If any assets are not selected in UnityEditor like a just after started,
            // this function returns absolute path.
            // "AssetDatabase.CreateAsset()" will be failed if the path is absolute path.

            string path = "Assets";

            foreach (UnityEngine.Object asset in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(asset);

                if (System.IO.File.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                    break;
                }
            }

            return path;
        }

        public static string CorrectAssetNameToAvoidOverwrite(string assetPath)
        {
            // NOTE:
            // This assetPath has extension.

            string tempPath = "";
            string suffixNum = "";
            string extension = System.IO.Path.GetExtension(assetPath);
            assetPath = assetPath.Remove(assetPath.Length - extension.Length, extension.Length);

            Regex regex = new Regex(@"(?<assetPath>) (?<suffixNum>\d+)");
            Match match = regex.Match(assetPath);

            if (match.Success)
            {
                assetPath = match.Groups["assetPath"].Value;
                suffixNum = match.Groups["suffixNum"].Value;
            }

            while (true)
            {
                tempPath = assetPath + (string.IsNullOrEmpty(suffixNum) ? "" : " " + suffixNum) + extension;

                if (!System.IO.File.Exists(tempPath))
                {
                    return tempPath;
                }

                suffixNum = string.IsNullOrEmpty(suffixNum) ? "1" : (int.Parse(suffixNum) + 1).ToString();
            }
        }

        public static void StartToRenameAsset(string assetPath)
        {
            StartToRenameAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
        }

        public static void StartToRenameAsset(UnityEngine.Object asset)
        {
            // NOTE:
            // PingObject function is quite simple & work completely to rename.
            // However the animation is not so good.
            // EditorGUIUtility.PingObject(asset);

            // NOTE:
            // FocusAsset function is important to rename asset in following case.
            // - "Project/Assets/~Any Directory" is focused.
            // - Any other UnityEditor window is focused.

            Selection.activeObject = asset;
            EditorUtility.FocusProjectWindow();
            FocusAsset(asset);
			ProjectWindow.SendEvent(Event.KeyboardEvent(KeyCode.F2.ToString()));
        }

        public static void FocusAsset(UnityEngine.Object asset)
        {
			// NOTE:
			// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ProjectWindow/ProjectWindowUtil.cs

			FrameObjectInProjectWindow.Invoke
            (ProjectWindow, new object[] { asset.GetInstanceID() });
        }

    }
#endif
}