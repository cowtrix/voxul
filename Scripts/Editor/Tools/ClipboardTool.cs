using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[Serializable]
	internal class ClipboardTool : VoxelPainterTool
	{
		public override GUIContent Icon => EditorGUIUtility.IconContent("Clipboard");
		public enum ePasteMode
		{
			Add,    // Won't override existing voxels
			Override,   // Will override existing voxels
		}

		[Serializable]
		public class Snippet
		{
			public VoxelMapping Data;
			public Bounds Bounds => Data.Keys.GetBounds();
			public sbyte SnapLayer;

			public Snippet(IEnumerable<Voxel> data)
			{
				Data = new VoxelMapping();
				foreach (var v in data)
				{
					Data.Add(v.Coordinate, v);
				}
				if (Data.Any())
				{
					SnapLayer = data.Min(d => d.Coordinate.Layer);
				}
			}

			public void DrawGUI()
			{
				EditorGUILayout.BeginVertical("Box");
				EditorGUILayout.LabelField($"{Data.Count} Voxels, {Bounds}");
				EditorGUILayout.LabelField($"Snap Layer: {SnapLayer}");
				EditorGUILayout.EndVertical();
			}
		}

		public enum ERotationAxis
		{
			X, Y, Z
		}
		public ERotationAxis RotationAxis = ERotationAxis.Y;
		public ePasteMode PasteMode;
		public Snippet CurrentClipboard;
		public Bounds SelectionBounds;
		public Vector3 Offset;
		public VoxelCursor Cursor
		{
			get
			{
				if (__cursor == null)
				{
					__cursor = new VoxelCursor(null);
				}
				return __cursor;
			}
		}
		private VoxelCursor __cursor;

		public override void OnDisable()
		{
			Cursor.Destroy();
			base.OnDisable();
		}

		protected override EPaintingTool ToolID => EPaintingTool.Clipboard;

		public override bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			PasteMode = (ePasteMode)EditorGUILayout.EnumPopup("Paste Mode", PasteMode);
			if (CurrentClipboard == null)
			{
				if (GUILayout.Button("Copy Selection To Clipboard") ||
				(Event.current.isKey && Event.current.control && Event.current.keyCode == KeyCode.C))
				{
					CurrentClipboard = new Snippet(voxelPainter.CurrentSelection.Select(c => voxelPainter.Renderer.Mesh.Voxels[c]));
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
					voxelPainter.SetSelection(null);
					Offset = Vector3.zero;
					return true;
				}
				else if (GUILayout.Button("Cut Selection To Clipboard") ||
				(Event.current.isKey && Event.current.control && Event.current.keyCode == KeyCode.X))
				{
					CurrentClipboard = new Snippet(voxelPainter.CurrentSelection.Select(c => voxelPainter.Renderer.Mesh.Voxels[c]));
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
					foreach(var v in CurrentClipboard.Data.Values)
					{
						voxelPainter.Renderer.Mesh.Voxels.Remove(v.Coordinate);
					}
					voxelPainter.Renderer.Mesh.Invalidate();
					voxelPainter.SetSelection(null);
					Offset = Vector3.zero;
					return true;
				}
				else if (GUILayout.Button("Cut Selection To New Voxel Object"))
				{
					var newObj = new GameObject(voxelPainter.Renderer.gameObject.name + "_submesh")
						.AddComponent<VoxelRenderer>();
					newObj.transform.SetParent(voxelPainter.Renderer.transform);
					newObj.transform.localPosition = Vector3.zero;
					newObj.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
					foreach (var v in voxelPainter.CurrentSelection)
					{
						var vox = voxelPainter.Renderer.Mesh.Voxels[v];
						voxelPainter.Renderer.Mesh.Voxels.Remove(v);
						newObj.Mesh.Voxels.AddSafe(vox);
					}
					voxelPainter.Renderer.Mesh.Invalidate();
					voxelPainter.SetSelection(null);
					newObj.Mesh.Invalidate();
					Offset = Vector3.zero;
					return true;
				}
			}
			else
			{
				RotationAxis = (ERotationAxis)EditorGUILayout.EnumPopup("Rotation Axis", RotationAxis);
				CurrentClipboard.DrawGUI();

				Vector3 axisFactor(ERotationAxis axis, float angle)
				{
					switch (axis)
					{
						case ERotationAxis.X:
							return new Vector3(angle, 0, 0);
						case ERotationAxis.Y:
							return new Vector3(0, angle, 0);
						case ERotationAxis.Z:
							return new Vector3(0, 0, angle);
					}
					throw new Exception($"Bad axis: {axis}");
				}

				var snappedBoundsCenter = CurrentClipboard.Bounds.center.RoundToIncrement(VoxelCoordinate.LayerToScale(CurrentClipboard.SnapLayer));
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("↶ 90°"))
				{
					CurrentClipboard.Data = CurrentClipboard.Data.Values
						.Rotate(Quaternion.Euler(axisFactor(RotationAxis, 90)), snappedBoundsCenter)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				if (GUILayout.Button("↷ 90°"))
				{
					CurrentClipboard.Data =
						CurrentClipboard.Data.Values
						.Rotate(Quaternion.Euler(axisFactor(RotationAxis, -90)), snappedBoundsCenter)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				if (GUILayout.Button("↺ 180°"))
				{
					CurrentClipboard.Data =
						CurrentClipboard.Data.Values
						.Rotate(Quaternion.Euler(axisFactor(RotationAxis, 180)), snappedBoundsCenter)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("⤒ Flip Y"))
				{
					CurrentClipboard.Data =
						CurrentClipboard.Data.Values
						.Transform(x => new VoxelCoordinate(x.X, -x.Y, x.Z, x.Layer))
						.FlipSurface(EVoxelDirection.YPos)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				if (GUILayout.Button("⇥ Flip X"))
				{
					CurrentClipboard.Data =
						CurrentClipboard.Data.Values
						.Transform(x => new VoxelCoordinate(-x.X, x.Y, x.Z, x.Layer))
						.FlipSurface(EVoxelDirection.XPos)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				if (GUILayout.Button("⇤ Flip Z"))
				{
					CurrentClipboard.Data =
						CurrentClipboard.Data.Values
						.Transform(x => new VoxelCoordinate(x.X, x.Y, -x.Z, x.Layer))
						.FlipSurface(EVoxelDirection.ZPos)
						.Finalise();
					Cursor.SetData(voxelPainter.Renderer.transform.localToWorldMatrix, CurrentClipboard.Data.Values);
				}
				EditorGUILayout.EndHorizontal();

				if (GUILayout.Button("Paste Selection To Bounds") ||
				(Event.current.isKey && Event.current.control && Event.current.keyCode == KeyCode.V))
				{
					foreach (var v in CurrentClipboard.Data)
					{
						var coord = v.Key;
						var offset = VoxelCoordinate.FromVector3(Offset, v.Key.Layer);
						var newVox = new Voxel(coord + offset, v.Value.Material.Copy());
						if (PasteMode == ePasteMode.Add)
						{
							if (!voxelPainter.Renderer.Mesh.Voxels.AddSafe(newVox))
							{
								DebugHelper.DrawCube(newVox.Coordinate, voxelPainter.Renderer.transform.localToWorldMatrix, Color.red, 5);
							}
						}
						else if (PasteMode == ePasteMode.Override)
						{
							voxelPainter.Renderer.Mesh.Voxels.SetSafe(newVox);
						}
					}
					return true;
				}
				if (GUILayout.Button("Clear Clipboard"))
				{
					CurrentClipboard = null;
					Cursor.Destroy();
				}
			}
			return false;
		}

		protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer, Event currentEvent,
			List<VoxelCoordinate> selection, EVoxelDirection hitDir)
		{
			var mat = renderer.transform.localToWorldMatrix;

			Handles.matrix = mat;
			if (CurrentClipboard == null)
			{
				if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
				{
					if (!currentEvent.shift)
					{
						voxelPainter.SetSelection(null);
					}
					if (selection == null)
					{
						return false;
					}
					foreach (var c in selection)
					{
						voxelPainter.AddSelection(c);
					}
					SelectionBounds = voxelPainter.CurrentSelection.First().ToBounds();
					foreach (var p in voxelPainter.CurrentSelection.Skip(1))
					{
						SelectionBounds.Encapsulate(p.ToBounds());
					}
					if (currentEvent.control)
					{
						foreach (var v in renderer.Mesh.Voxels)
						{
							if (SelectionBounds.Contains(v.Key.ToVector3()))
							{
								voxelPainter.AddSelection(v.Key);
							}
						}
					}
					UseEvent(currentEvent);
				}
				HandleExtensions.DrawWireCube(SelectionBounds.center, SelectionBounds.extents, Quaternion.identity, Color.magenta);
			}
			else
			{
				var bcenter = CurrentClipboard.Bounds.center + Offset;
				Offset = Handles.PositionHandle(bcenter, Quaternion.identity) - CurrentClipboard.Bounds.center;
				Offset = Offset.RoundToIncrement(VoxelCoordinate.LayerToScale(CurrentClipboard.SnapLayer));
				HandleExtensions.DrawWireCube(bcenter, CurrentClipboard.Bounds.extents, Quaternion.identity, Color.magenta);

				Cursor.SetData(mat * Matrix4x4.TRS(Offset, Quaternion.identity, Vector3.one));
				Cursor.Update();
			}

			return false;
		}
	}
}