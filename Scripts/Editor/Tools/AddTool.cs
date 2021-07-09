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
		private double m_lastAdd;
		[SerializeField]
		private VoxelRenderer m_cursor;
		private Color LerpColor { get => EditorPrefUtility.GetPref("voxul_lerpcolor", Color.white); set => EditorPrefUtility.SetPref("voxul_lerpcolor", value); }
		public bool LerpEnabled { get => EditorPrefUtility.GetPref("voxul_lerpenabled", false); set => EditorPrefUtility.SetPref("voxul_lerpenabled", value); }
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
		}

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			LerpEnabled = EditorGUILayout.Toggle("Enable color lerp", LerpEnabled);
			LerpColor = EditorGUILayout.ColorField("Lerp Color", LerpColor);
			return base.DrawInspectorGUI(voxelPainter);
		}

		protected override EPaintingTool ToolID => EPaintingTool.Add;

		protected override bool GetVoxelDataFromPoint(VoxelPainter painter, VoxelRenderer renderer, MeshCollider collider, Vector3 hitPoint,
			Vector3 hitNorm, int triIndex, sbyte layer,
			out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			if (Event.current.alt)
			{
				m_cursor?.gameObject.SetActive(false);
				return base.GetVoxelDataFromPoint(painter, renderer, collider, hitPoint, hitNorm, triIndex, layer, out selection, out hitDir);
			}

			hitPoint = renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPoint);
			hitNorm = renderer.transform.worldToLocalMatrix.MultiplyVector(hitNorm);
			VoxelCoordinate.VectorToDirection(hitNorm, out hitDir);
			var scale = VoxelCoordinate.LayerToScale(layer);
			var singleCoord = VoxelCoordinate.FromVector3(hitPoint + hitNorm * scale / 2f, layer);
			selection = new List<VoxelCoordinate>() { singleCoord };
			switch (painter.MirrorMode)
			{
				case eMirrorMode.X:
					selection.Add(new VoxelCoordinate(-singleCoord.X, singleCoord.Y, singleCoord.Z, singleCoord.Layer));
					break;
				case eMirrorMode.Y:
					selection.Add(new VoxelCoordinate(singleCoord.X, -singleCoord.Y, singleCoord.Z, singleCoord.Layer));
					break;
				case eMirrorMode.Z:
					selection.Add(new VoxelCoordinate(singleCoord.X, singleCoord.Y, -singleCoord.Z, singleCoord.Layer));
					break;
			}

			if (!m_cursor || !m_cursor.Mesh)
			{
				OnEnable();
			}
			m_cursor.gameObject.SetActive(true);
			m_cursor.GetComponent<AutoDestroyer>().KeepAlive();
			m_cursor.transform.ApplyTRSMatrix(renderer.transform.localToWorldMatrix);
			//m_cursor.transform.SetParent(renderer.transform);
			if (!m_cursor.Mesh.Voxels.Keys.SequenceEqual(selection))
			{
				m_cursor.Mesh.Voxels.Clear();
				foreach (var s in selection)
				{
					var v = CurrentBrush.Copy();
					if (LerpEnabled)
					{
						UnityEngine.Random.InitState(s.GetHashCode());
						var surf = v.Default;
						surf.Albedo = Color.Lerp(surf.Albedo, LerpColor, UnityEngine.Random.value);
						v.Default = surf;
						var ov = v.Overrides.Select(o =>
						{
							o.Data.Albedo = Color.Lerp(o.Data.Albedo, LerpColor, UnityEngine.Random.value);
							return o;
						}).ToArray();
						v.Overrides = ov;
					}
					m_cursor.Mesh.Voxels.AddSafe(new Voxel { Coordinate = s, Material = v });
				}
				m_cursor.Mesh.Invalidate();
				m_cursor.Invalidate(true, false);
			}

			return true;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{

			if (currentEvent.isMouse && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				if (EditorApplication.timeSinceStartup < m_lastAdd + .5f)
				{
					//voxulLogger.Warning($"Swallowed double event");
					return false;
				}
				m_lastAdd = EditorApplication.timeSinceStartup;
				var creationList = new HashSet<VoxelCoordinate>(selection);
				if (currentEvent.control && currentEvent.shift)
				{
					var bounds = voxelPainter.CurrentSelection.GetBounds();
					bounds.Encapsulate(selection.GetBounds());
					foreach (VoxelCoordinate coord in bounds.GetVoxelCoordinates(voxelPainter.CurrentLayer))
					{
						creationList.Add(coord);
					}
				}
				voxelPainter.SetSelection(CreateVoxel(creationList, renderer).ToList());
				if (m_cursor)
				{
					m_cursor.gameObject.SafeDestroy();
				}
			}
			return false;
		}

		private IEnumerable<VoxelCoordinate> CreateVoxel(IEnumerable<VoxelCoordinate> coords, VoxelRenderer renderer)
		{
			foreach (var brushCoord in coords)
			{
				var v = CurrentBrush.Copy();
				if (LerpEnabled)
				{
					UnityEngine.Random.InitState(brushCoord.GetHashCode());
					var s = v.Default;
					s.Albedo = Color.Lerp(s.Albedo, LerpColor, UnityEngine.Random.value);
					v.Default = s;
					var ov = v.Overrides.Select(o =>
					{
						o.Data.Albedo = Color.Lerp(o.Data.Albedo, LerpColor, UnityEngine.Random.value);
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