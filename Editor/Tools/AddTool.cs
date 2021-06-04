using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit
{
	[Serializable]
	internal class AddTool : VoxelPainterTool
	{
		private double m_lastAdd;
		private VoxelMesh m_previewMesh;

		public override void OnEnable()
		{
			m_previewMesh = ScriptableObject.CreateInstance<VoxelMesh>();
			base.OnEnable();
		}

		public override void OnDisable()
		{
			GameObject.DestroyImmediate(m_previewMesh);
			m_previewMesh = null;
		}

		protected override EPaintingTool ToolID => EPaintingTool.Add;

		protected override bool GetVoxelDataFromPoint(VoxelPainter painter, VoxelRenderer renderer, Vector3 hitPoint,
			Vector3 hitNorm, int triIndex, sbyte layer,
			out List<VoxelCoordinate> selection, out EVoxelDirection hitDir)
		{
			if (Event.current.alt)
			{
				return base.GetVoxelDataFromPoint(painter, renderer, hitPoint, hitNorm, triIndex, layer, out selection, out hitDir);
			}

			hitPoint = renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPoint);
			hitNorm = renderer.transform.worldToLocalMatrix.MultiplyVector(hitNorm);
			VoxelCoordinate.VectorToDirection(hitNorm, out hitDir);
			var scale = VoxelCoordinate.LayerToScale(layer);
			var singleCoord = VoxelCoordinate.FromVector3(hitPoint + hitNorm * scale / 2f, layer);
			selection = new List<VoxelCoordinate>() { singleCoord };
			switch(painter.MirrorMode)
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
			return true;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
			Event currentEvent, List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			if (currentEvent.isMouse && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				if (EditorApplication.timeSinceStartup < m_lastAdd + .5f)
				{
					Debug.LogWarning($"Swallowed double event");
					return false;
				}
				m_lastAdd = EditorApplication.timeSinceStartup;
				var creationList = new HashSet<VoxelCoordinate>(selection);
				if (currentEvent.control && currentEvent.shift)
				{
					var bounds = voxelPainter.CurrentSelection.GetBounds();
					bounds.Encapsulate(selection.GetBounds());
					foreach (VoxelCoordinate coord in renderer.Mesh.GetVoxelCoordinates(bounds, voxelPainter.CurrentLayer))
					{
						creationList.Add(coord);
					}
				}
				voxelPainter.SetSelection(CreateVoxel(creationList, renderer).ToList());
			}
			return false;
		}

		private IEnumerable<VoxelCoordinate> CreateVoxel(IEnumerable<VoxelCoordinate> coords, VoxelRenderer renderer)
		{
			foreach (var brushCoord in coords)
			{
				if (renderer.Mesh.Voxels.AddSafe(new Voxel(brushCoord, CurrentBrush.Copy())))
				{
					yield return brushCoord;
				}
			}
			renderer.Invalidate(true);
		}
	}
}