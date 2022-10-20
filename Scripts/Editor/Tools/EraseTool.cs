#if UNITY_EDITOR
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
    internal class EraseTool : VoxelPainterTool
    {
        public override GUIContent Icon => EditorGUIUtility.IconContent("d_Grid.EraserTool");
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
            base.OnDisable();
        }

        protected override bool GetVoxelDataFromPoint(VoxelPainter voxelPainterTool, VoxelRenderer renderer, MeshCollider collider,
            Vector3 hitPoint, Vector3 hitNorm, int triIndex, out HashSet<VoxelCoordinate> selection, out EVoxelDirection hitDir)
        {
            var result = base.GetVoxelDataFromPoint(voxelPainterTool, renderer, collider, hitPoint, hitNorm, triIndex, out selection, out hitDir);
            if (result)
            {
                Handles.matrix = renderer.transform.localToWorldMatrix;
                foreach (var s in selection)
                {
                    var layerScale = VoxelCoordinate.LayerToScale(s.Layer);
                    var dirs = new HashSet<EVoxelDirection>() { hitDir };
                    if (Event.current.shift)
                    {
                        foreach (var d in VoxelExtensions.Directions)
                        {
                            dirs.Add(d);
                        }
                    }
                    foreach (var d in dirs)
                    {
                        var rot = VoxelCoordinate.DirectionToQuaternion(d);
                        var pos = s.ToVector3() + rot * (layerScale * .5f * Vector3.up);
                        HandleExtensions.DrawWireCube(pos, new Vector3(layerScale / 2f, layerScale * .05f, layerScale / 2f), rot, Color.magenta);
                    }

                }
            }
            return result;
        }

        protected override EPaintingTool ToolID => EPaintingTool.Erase;

        protected override bool DrawSceneGUIInternal(VoxelPainter voxelPainter, VoxelRenderer renderer,
            Event currentEvent, HashSet<VoxelCoordinate> selection, EVoxelDirection hitDir, Vector3 hitPos)
        {
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                if (EditorApplication.timeSinceStartup < m_lastAdd + .1f)
                {
                    voxulLogger.Warning($"Swallowed double event");
                    return false;
                }
                m_lastAdd = EditorApplication.timeSinceStartup;
                var creationList = new HashSet<VoxelCoordinate>(selection);
                if (EraseVoxelSurface(creationList, renderer, hitDir, currentEvent))
                {
                    voxelPainter.SetSelection(creationList);
                }
                UseEvent(currentEvent);
            }
            return false;
        }

        protected override int GetToolWindowHeight()
        {
            return base.GetToolWindowHeight() + 25;
        }

        protected override void DrawToolLayoutGUI(Rect rect, Event currentEvent, VoxelPainter voxelPainter)
        {
            //base.DrawToolLayoutGUI(rect, currentEvent, voxelPainter);
        }

        private bool EraseVoxelSurface(IEnumerable<VoxelCoordinate> coords, VoxelRenderer renderer, EVoxelDirection dir, Event currentEvent)
        {
            var coordList = coords.ToList();
            foreach (var brushCoord in coordList)
            {
                if (!renderer.Mesh.Voxels.TryGetValue(brushCoord, out var vox))
                {
                    continue;
                }
                if (vox.Material.Overrides == null)
                {
                    vox.Material.Overrides = new List<DirectionOverride>();
                }
                var surface = vox.Material.GetSurface(dir);
                surface.Skip = true;
                vox.Material.SetSurface(dir, surface);
                renderer.Mesh.Voxels[brushCoord] = vox;
            }
            renderer.Mesh.Invalidate();
            renderer.Invalidate(true, true);
            return true;
        }
    }
}
#endif