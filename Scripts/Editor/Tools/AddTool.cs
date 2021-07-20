using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[Serializable]
	internal class AddTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("CreateAddNew");

		public static Gradient DefaultGradient = new Gradient
		{
			colorKeys = new[]
			{
				new GradientColorKey
				{
					color = Color.white,
					time = 0,
				},
				new GradientColorKey
				{
					color = Color.black,
					time = 0,
				},
			}
		};

		private double m_lastAdd;
		[SerializeField]
		private VoxelRenderer m_cursor;
		private SerializableGradient LerpColor { get => EditorPrefUtility.GetPref("voxul_lerpcolor", new SerializableGradient(DefaultGradient)); set => EditorPrefUtility.SetPref("voxul_lerpcolor", value); }
		private bool LerpEnabled { get => EditorPrefUtility.GetPref("voxul_lerpenabled", false); set => EditorPrefUtility.SetPref("voxul_lerpenabled", value); }

		public sbyte CurrentLayer
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainter_CurrentLayer", default(sbyte));
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainter_CurrentLayer", value);
			}
		}

		public override void OnEnable()
		{
			if (!m_cursor)
			{
				m_cursor = new GameObject("AddTool_Cursor").AddComponent<VoxelRenderer>();
				m_cursor.gameObject.hideFlags = HideFlags.HideAndDontSave;
				m_cursor.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
				m_cursor.GenerateCollider = false;
				m_cursor.enabled = false;
				m_cursor.SetupComponents(false);
				m_cursor.gameObject.AddComponent<AutoDestroyer>();
			}
			base.OnEnable();
		}

		public override void OnDisable()
		{
			if (m_cursor)
			{
				m_cursor.gameObject.SafeDestroy();
				m_cursor = null;
			}
			base.OnDisable();
		}

		protected override EPaintingTool ToolID => EPaintingTool.Add;

		protected override bool GetVoxelDataFromPoint(VoxelPainter painter, VoxelRenderer renderer, MeshCollider collider, Vector3 hitPoint,
			Vector3 hitNorm, int triIndex, out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			if (Event.current.alt)
			{
				m_cursor?.gameObject.SetActive(false);
				return base.GetVoxelDataFromPoint(painter, renderer, collider, hitPoint, hitNorm, triIndex, out selection, out hitDir);
			}

			hitPoint = renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPoint);
			hitNorm = renderer.transform.worldToLocalMatrix.MultiplyVector(hitNorm);
			VoxelCoordinate.VectorToDirection(hitNorm, out hitDir);
			var scale = VoxelCoordinate.LayerToScale(CurrentLayer);
			var singleCoord = VoxelCoordinate.FromVector3(hitPoint + hitNorm * (scale / 2f) * (collider ? 1 : 0), CurrentLayer);
			selection = new List<VoxelCoordinate>() { singleCoord };
			
			DoMeshCursorPreview(renderer, selection);

			return true;
		}

		private void DoMeshCursorPreview(VoxelRenderer renderer, IEnumerable<VoxelCoordinate> selection)
		{
			if (!m_cursor || !m_cursor.Mesh)
			{
				OnEnable();
			}
			m_cursor.gameObject.SetActive(true);
			m_cursor.GetComponent<AutoDestroyer>().KeepAlive();
			m_cursor.transform.ApplyTRSMatrix(renderer.transform.localToWorldMatrix);
			if (!m_cursor.Mesh.Voxels.Keys.SequenceEqual(selection))
			{
				m_cursor.Mesh.Voxels.Clear();
				foreach (var s in selection)
				{
					var v = CurrentBrush.Copy();
					if (LerpEnabled)
					{
						var gradient = LerpColor.ToGradient();
						UnityEngine.Random.InitState(s.GetHashCode());
						var surf = v.Default;
						surf.Albedo = Color.Lerp(surf.Albedo, gradient.Evaluate(UnityEngine.Random.value), UnityEngine.Random.value);
						v.Default = surf;
						var ov = v.Overrides.Select(o =>
						{
							o.Surface.Albedo = Color.Lerp(o.Surface.Albedo, gradient.Evaluate(UnityEngine.Random.value), UnityEngine.Random.value);
							return o;
						}).ToArray();
						v.Overrides = ov;
					}
					m_cursor.Mesh.Voxels.AddSafe(new Voxel { Coordinate = s, Material = v });
				}
				m_cursor.Mesh.Invalidate();
				m_cursor.Invalidate(true, false);
			}
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (currentEvent.isMouse && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
			{
				if (EditorApplication.timeSinceStartup < m_lastAdd + .5f)
				{
					return false;
				}
				m_lastAdd = EditorApplication.timeSinceStartup;
				var creationList = new HashSet<VoxelCoordinate>(selection);
				if (currentEvent.control && currentEvent.shift)
				{
					var bounds = voxelPainter.CurrentSelection.GetBounds();
					bounds.Encapsulate(selection.GetBounds());
					foreach (VoxelCoordinate coord in bounds.GetVoxelCoordinates(CurrentLayer))
					{
						creationList.Add(coord);
					}
				}
				voxelPainter.SetSelection(CreateVoxel(creationList, renderer).ToList());
				if (m_cursor)
				{
					m_cursor.gameObject.SafeDestroy();
				}
				UseEvent(currentEvent);
			}
			return false;
		}

		protected override int GetToolWindowHeight()
		{
			return base.GetToolWindowHeight() + 40;
		}

		protected override void DrawToolLayoutGUI(Rect rect, Event currentEvent, VoxelPainter voxelPainter)
		{
			base.DrawToolLayoutGUI(rect, currentEvent, voxelPainter);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(EditorGUIUtility.IconContent("d_ToggleUVOverlay")
				.WithTooltip("Current Layer"), EditorStyles.miniLabel, GUILayout.Width(25));

			if (GUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				CurrentLayer--;
			}
			CurrentLayer = (sbyte)EditorGUILayout.IntField((int)CurrentLayer, GUILayout.Width(32));
			if (GUILayout.Button("+", EditorStyles.miniButtonLeft))
			{
				CurrentLayer++;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUI.color = LerpEnabled ? Color.green : Color.white;
			if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreTextureRGB")
				.WithTooltip("Enable Gradient Painting")))
			{
				LerpEnabled = !LerpEnabled;
			}
			GUI.color = Color.white;
			LerpColor = new SerializableGradient(EditorGUILayout.GradientField(LerpColor.ToGradient()));
			EditorGUILayout.EndHorizontal();
		}

		private IEnumerable<VoxelCoordinate> CreateVoxel(IEnumerable<VoxelCoordinate> coords, VoxelRenderer renderer)
		{
			foreach (var brushCoord in coords)
			{
				var v = CurrentBrush.Copy();
				if (LerpEnabled)
				{
					var gradient = LerpColor.ToGradient();
					UnityEngine.Random.InitState(brushCoord.GetHashCode());
					var s = v.Default;
					s.Albedo = gradient.Evaluate(UnityEngine.Random.value);
					v.Default = s;
					var ov = v.Overrides.Select(o =>
					{
						o.Surface.Albedo = Color.Lerp(o.Surface.Albedo, gradient.Evaluate(UnityEngine.Random.value), UnityEngine.Random.value);
						return o;
					}).ToArray();
					v.Overrides = ov;
				}
				if (renderer.Mesh.Voxels.AddSafe(new Voxel(brushCoord, v)))
				{
					yield return brushCoord;
				}
			}
			renderer.Mesh.Invalidate();
			renderer.Invalidate(true, true);
		}
	}
}