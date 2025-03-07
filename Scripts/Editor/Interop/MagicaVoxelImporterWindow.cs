#if UNITY_EDITOR && MAGICA_VOXEL
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit.Interop
{
	public class MagicaVoxelImporterWindow : EditorWindow
	{
		public VoxelMesh Mesh;
		public string FilePath;
		private MagicaVoxelImportUtility m_importer = new MagicaVoxelImportUtility();

		[MenuItem("Tools/Voxul/Import MagicaVoxel File (.vox)")]
		public static void OpenWindow()
		{
			GetWindow<MagicaVoxelImporterWindow>(true, "Import MagicaVoxel File");
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Path");
			FilePath = EditorGUILayout.TextField(FilePath);
			if (GUILayout.Button(EditorGUIUtility.IconContent("Add-Available")))
			{
				FilePath = EditorUtility.OpenFilePanel("Open MagicaVoxel File", "Assets", "vox");
			}
			EditorGUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(FilePath))
			{
				EditorGUILayout.HelpBox("Select a .vox file to import.", MessageType.Info);
				return;
			}
			else if (!FilePath.EndsWith(".vox"))
			{
				EditorGUILayout.HelpBox("File is not of the correct format, expected '.vox'", MessageType.Error);
				return;
			}
			else if (!File.Exists(FilePath))
			{
				EditorGUILayout.HelpBox("File does not exist at target path.", MessageType.Error);
				return;
			}
			if (GUILayout.Button("Load"))
			{
				Load();
			}

			EditorGUILayout.BeginVertical("Box", GUILayout.MinHeight(position.width - 100), GUILayout.ExpandWidth(true));
			GUILayout.Label("Pallete");
			EditorGUILayout.EndVertical();

			var pRect = GUILayoutUtility.GetLastRect();
			pRect = new Rect(pRect.x, pRect.y + 20, pRect.width, pRect.height - 20);
			var step = pRect.size / 32;
			int counter = 0;
			foreach (var mat in m_importer.Pallete)
			{
				var x = counter % 32;
				var y = counter / 32;
				var r = new Rect(pRect.x + x * step.x, pRect.y + y * step.y, step.x - 1, step.y - 1);
				EditorGUI.DrawRect(r, mat.Value.Default.Albedo);
				counter++;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.Separator();
			if (Mesh?.Voxels.Count > 0)
			{
				EditorGUILayout.HelpBox($"Voxul Mesh: {Mesh.Voxels.Count} voxels", MessageType.Info);
				if (GUILayout.Button("Clear"))
				{
					Mesh = null;
				}
				if (GUILayout.Button("Instantiate In Scene"))
				{
					var go = new GameObject(Path.GetFileNameWithoutExtension(FilePath));
					var r = go.AddComponent<VoxelRenderer>();
					r.Mesh = Mesh;
				}
			}
		}

		private void Load()
		{
			var reader = new CsharpVoxReader.VoxReader(FilePath, m_importer);
			reader.Read();
			if (!Mesh)
			{
				Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
				Mesh.name = System.Guid.NewGuid().ToString();
			}
			m_importer.GetResult(Mesh);
		}
	}
}
#endif