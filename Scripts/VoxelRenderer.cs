using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Voxul.LevelOfDetail;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{

    [SelectionBase]
    public class VoxelRenderer : ExtendedMonoBehaviour, ISpriteSheetProvider
    {
        public enum eSnapMode
        {
            None, Local, Global
        }

        [Serializable]
        public class RenderLODSettings
        {
            public enum eLODMode
            {
                None,
                Retarget,
                Cast,
            }

            [Serializable]
            public class LODLevel
            {
                public VoxelRenderer Renderer;
                [Range(0, 1)]
                public float ScreenTransitionWidth;
                [Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
                public sbyte MaxLayer;
                public float MaterialMergeDistance;
                public float FillRequirement = .5f;
                public eLODMode Mode;
                public bool Collider;
            }

            public List<LODLevel> LODs = new List<LODLevel>();
            [Range(0, 1)]
            public float PrimaryScreenTransitionWidth = .05f;
            public bool GenerateLODGroup = true;
        }

        public VoxelMesh Mesh;

        [Header("Settings")]
        public bool CustomMaterials;
        public Material OpaqueMaterial;
        public Material TransparentMaterial;

        public bool GenerateCollider = true;
        public eSnapMode SnapMode;
        [Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
        public sbyte SnapLayer = 0;

        [Header("Rendering")]
        public EThreadingMode ThreadingMode;
        public float MaxCoroutineUpdateTime = 0.5f;
        public bool BatchingEnabled = true;
        public UnityEvent OnMeshRebuilt;
        public SpriteSheet SpriteSheetOverride;
        public RenderLODSettings LODSettings;

        // private fields
        [SerializeField]
        [HideInInspector]
        protected VoxelMeshWorker m_voxWorker;
        protected bool m_isDirty;

        protected virtual VoxelMeshWorker GetVoxelMeshWorker()
        {
            if (m_voxWorker == null)
            {
                m_voxWorker = new VoxelMeshWorker(Mesh);
            }
            return m_voxWorker;
        }

        [SerializeField]
        [HideInInspector]
        private string m_lastMeshHash;

        [FormerlySerializedAs("Renderers")]
        [SerializeField]
        public List<VoxelRendererSubmesh> Submeshes = new List<VoxelRendererSubmesh>();

        public Bounds Bounds => Submeshes.Select(b => b.Bounds).EncapsulateAll();

        private void Reset()
        {
            ThreadingMode = VoxelManager.Instance.DefaultThreadingMode;
            MaxCoroutineUpdateTime = VoxelManager.Instance.DefaultMaxCoroutineUpdateTime;
        }

        protected virtual void Awake()
        {
            VoxelManager.Instance.OnValidate();
        }

        public void SetDirty() => m_isDirty = true;

        [ContextMenu("Delete All Voxels")]
        public void ClearMesh()
        {
            if (!Util.PromptEditor($"Clear Mesh {this.Mesh}?", "Are you sure you want to clear this mesh and delete all of its data permanently?", "Yes, I'm sure"))
            {
                return;
            }
            Mesh.Voxels.Clear();
            Mesh.Invalidate();
            OnClear();
        }

        [ContextMenu("Clean Submeshes")]
        public void CleanSubmeshes()
        {
            foreach (var submesh in Submeshes)
            {
                if (!submesh)
                {
                    continue;
                }
                if (submesh.gameObject != gameObject)
                {
                    submesh.gameObject.SafeDestroy();
                }
                else
                {
                    submesh.SafeDestroy();
                }
            }
            Submeshes.Clear();
        }

        [ContextMenu("Force Invalidate")]
        public void ForceInvalidate()
        {
            if (Mesh)
            {
                SetupComponents(this.GenerateCollider);
                Mesh.Invalidate();
                Invalidate(true, false);
            }
        }

        protected virtual void OnClear() { }

        public void SetupComponents(bool forceCollider)
        {
            Submeshes = new List<VoxelRendererSubmesh>(GetComponentsInChildren<VoxelRendererSubmesh>()
                .Where(r => r.Parent == this));
            foreach (var submesh in Submeshes)
            {
                submesh.SetupComponents(this, GenerateCollider || forceCollider);
                if (!CustomMaterials)
                {
                    var vm = VoxelManager.Instance;
                    if (!vm.DefaultMaterial || !vm.DefaultMaterialTransparent)
                    {
                        vm.OnValidate();
                    }
                    SetMaterials(submesh, VoxelManager.Instance.DefaultMaterial, VoxelManager.Instance.DefaultMaterialTransparent);
                }
                else
                {
                    SetMaterials(submesh, OpaqueMaterial, TransparentMaterial);
                }
            }
        }

        public virtual void Invalidate(bool force, bool forceCollider, bool forceDispatch = false)
        {
            if (forceDispatch || !UnityMainThreadDispatcher.IsOnMainThread)
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    UnityMainThreadDispatcher.EnsureSubscribed();
                    this.Invalidate(force, forceCollider);
                });
                return;
            }
            m_isDirty = false;
            if (!Mesh)
            {
                foreach (var submesh in Submeshes)
                {
                    if (submesh.MeshFilter)
                    {
                        submesh.MeshFilter.sharedMesh = null;
                    }
                    if (submesh.MeshRenderer)
                    {
                        submesh.MeshRenderer.sharedMaterial = null;
                    }
                    if (submesh.MeshCollider)
                    {
                        submesh.MeshCollider.sharedMesh = null;
                    }
                }
                return;
            }

            if (m_lastMeshHash == Mesh.Hash && !force)
            {
                return;
            }

            if (m_lastMeshHash == Mesh.Hash)
            {
                ApplyVoxelMesh(Mesh);
                this.TrySetDirty();
                return;
            }

            Profiler.BeginSample("Invalidate");
            SetupComponents(forceCollider || GenerateCollider);

            Mesh.CurrentWorker = GetVoxelMeshWorker();
            Mesh.CurrentWorker.OnCompleted -= ApplyVoxelMesh;
            Mesh.CurrentWorker.OnCompleted += ApplyVoxelMesh;
            Mesh.CurrentWorker.VoxelMesh = Mesh;
            Mesh.CurrentWorker.GenerateMesh(ThreadingMode, force);

            m_lastMeshHash = Mesh.Hash;
            this.TrySetDirty();
            Profiler.EndSample();
        }

        protected virtual void ApplyVoxelMesh
            (VoxelMesh voxelMesh)
        {
            if (!this)
            {
                // Object has been destroyed
                return;
            }
            voxulLogger.Debug($"VoxelRenderer.OnMeshRebuilt: {voxelMesh}, {this}", this);
            if (Mesh.Hash != voxelMesh.Hash)
            {
                voxulLogger.Error("Unexpected hash!");
                return;
            }
            for (int i = 0; i < voxelMesh.UnityMeshInstances.Count; i++)
            {
                var data = voxelMesh.UnityMeshInstances[i];
                var unityMesh = data.UnityMesh;
                unityMesh.MarkDynamic();

                VoxelRendererSubmesh submesh;
                if (Submeshes.Count < voxelMesh.UnityMeshInstances.Count)
                {
                    if (i == 0)
                    {
                        submesh = gameObject.GetOrAddComponent<VoxelRendererSubmesh>();
                    }
                    else
                    {
                        submesh = new GameObject($"{name}_submesh_hidden_{i}")
                            .AddComponent<VoxelRendererSubmesh>();
                        submesh.transform.SetParent(transform);
                    }
                    Submeshes.Add(submesh);
                }
                else
                {
                    submesh = Submeshes[i];
                }
                submesh.SetupComponents(this, GenerateCollider);
                submesh.MeshFilter.sharedMesh = unityMesh;
                if (GenerateCollider && unityMesh.vertexCount > 0)
                {
                    voxulLogger.Debug($"Set MeshCollider mesh");
                    unityMesh.MarkDynamic();
                    unityMesh.MarkModified();
                    submesh.MeshCollider.sharedMesh = unityMesh;
                }
                if (!CustomMaterials)
                {
                    var vm = VoxelManager.Instance;
                    if (!vm.DefaultMaterial || !vm.DefaultMaterialTransparent)
                    {
                        vm.OnValidate();
                    }
                    SetMaterials(submesh, VoxelManager.Instance.DefaultMaterial, VoxelManager.Instance.DefaultMaterialTransparent);
                }
                else
                {
                    SetMaterials(submesh, OpaqueMaterial, TransparentMaterial);
                }
            }
            //Submeshes = Submeshes.Distinct().ToList();
            for (var i = Submeshes.Count - 1; i >= Mesh.UnityMeshInstances.Count; --i)
            {
                var r = Submeshes[i];
                if (r || (r != null && r.gameObject))
                {
                    voxulLogger.Debug($"Destroying submesh renderer {r}", this);
                    if (i == 0)
                    {
                        r.SafeDestroy();
                    }
                    else
                    {
                        r.gameObject.SafeDestroy();
                    }
                }
                Submeshes.RemoveAt(i);
            }
            if (Submeshes.Count == 0)
            {
                var mf = GetComponent<MeshFilter>();
                if (mf)
                {
                    mf.SafeDestroy();
                }
                var mr = GetComponent<MeshRenderer>();
                if (mr)
                {
                    mr.SafeDestroy();
                }
            }
            m_lastMeshHash = Mesh.Hash;
#if UNITY_EDITOR
            foreach (var r in Submeshes)
            {
                r.SetDirty();
            }
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
            GenerateLODs();
            OnMeshRebuilt?.Invoke();
        }

        private void GenerateLODs()
        {
            if (LODSettings == null || LODSettings.LODs == null || !LODSettings.LODs.Any())
            {
                return;
            }
            LODGroup lodGroup = null;
            if (LODSettings.GenerateLODGroup)
            {
                lodGroup = gameObject.GetOrAddComponent<LODGroup>();
                var lods = new LOD[LODSettings.LODs.Count + 1];
                lods[0] = new LOD { renderers = Submeshes.Select(r => r.MeshRenderer).ToArray(), screenRelativeTransitionHeight = LODSettings.PrimaryScreenTransitionWidth };
                for (var i = 1; i < lods.Length; ++i)
                {
                    const float margin = .000001f;
                    var l = lods[i];
                    l.screenRelativeTransitionHeight = (LODSettings.LODs.Count * margin) - (i * margin);
                    lods[i] = l;
                }
                if (lods.Length != lodGroup.GetLODs().Length)
                {
                    lodGroup.SetLODs(lods);
                }
            }
            for (int i = 0; i < LODSettings.LODs.Count; i++)
            {
                RenderLODSettings.LODLevel lod = LODSettings.LODs[i];
                if (!lod.Renderer)
                {
                    lod.Renderer = new GameObject($"LOD_{i}")
                        .AddComponent<VoxelRenderer>();
                }
                lod.Renderer.GenerateCollider = lod.Collider;
                lod.Renderer.gameObject.isStatic = gameObject.isStatic;
                lod.Renderer.gameObject.layer = gameObject.layer;
                if (!lod.Renderer.Mesh)
                {
                    lod.Renderer.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
                    lod.Renderer.Mesh.name = $"{Mesh.name}_LOD_{i}";
                    lod.Renderer.transform.SetParent(transform);
                    lod.Renderer.transform.Reset();
#if UNITY_EDITOR
                    var currentPath = UnityEditor.AssetDatabase.GetAssetPath(Mesh);
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        UnityEditor.AssetDatabase.AddObjectToAsset(lod.Renderer.Mesh, currentPath);
                    }
#endif
                }
                var lodIndex = i + 1;
                var t = new Task(() =>
                {
                    try
                    {
                        IEnumerable<Voxel> voxels;
                        switch (lod.Mode)
                        {
                            case RenderLODSettings.eLODMode.None:
                                voxels = Mesh.Voxels.Values;
                                break;
                            case RenderLODSettings.eLODMode.Retarget:
                                voxels = LevelOfDetailBuilder.RetargetToLayer(Mesh.Voxels.Values, lod.MaxLayer, lod.FillRequirement);
                                break;
                            case RenderLODSettings.eLODMode.Cast:
                                voxels = LevelOfDetailBuilder.Cast(Mesh.Voxels.Values, lod.MaxLayer, lod.FillRequirement, lod.MaterialMergeDistance);
                                break;
                            default:
                                throw new Exception($"Mode {lod.Mode} not supported.");
                        }
                        voxels = LevelOfDetailBuilder.MergeMaterials(voxels, lod.MaterialMergeDistance);
                        voxels = LevelOfDetailBuilder.StripVoxels(voxels);
                        lod.Renderer.Mesh.Voxels = new VoxelMapping(voxels);
                        lod.Renderer.Mesh.Invalidate();

                        if (LODSettings.GenerateLODGroup)
                        {
                            if (lod.Renderer.OnMeshRebuilt == null)
                            {
                                lod.Renderer.OnMeshRebuilt = new UnityEvent();
                            }
                            lod.Renderer.OnMeshRebuilt.RemoveAllListeners();
                            lod.Renderer.OnMeshRebuilt.AddListener(() => UnityMainThreadDispatcher.Enqueue(() => lod.Renderer.UpdateLOD(lodGroup, lodIndex, lod.ScreenTransitionWidth)));
                        }
                        lod.Renderer.Invalidate(true, false);
                    }
                    catch (Exception e)
                    {
                        voxulLogger.Exception(e);
                    }
                });
                t.Start();
            }
        }

        private void UpdateLOD(LODGroup lodGroup, int lodIndex, float screenTransitionWidth)
        {
            var lods = lodGroup.GetLODs();
            for (var i = 0; i < lods.Length; ++i)
            {
                const float margin = .0001f;
                if (i == lodIndex)
                {
                    continue;
                }
                var l = lods[i];

                if (i > lodIndex && l.screenRelativeTransitionHeight >= screenTransitionWidth)
                {
                    l.screenRelativeTransitionHeight = screenTransitionWidth - margin * lodIndex;
                }
                else if (i < lodIndex && l.screenRelativeTransitionHeight <= screenTransitionWidth)
                {
                    l.screenRelativeTransitionHeight = screenTransitionWidth + margin * lodIndex;
                }

                lods[i] = l;
            }
            if (lodIndex >= lods.Length)
            {
                Debug.LogError("e");
            }
            lods[lodIndex] = new LOD { renderers = Submeshes.Select(s => s.MeshRenderer).ToArray(), screenRelativeTransitionHeight = screenTransitionWidth };
            lodGroup.SetLODs(lods);
        }

        private void SetMaterials(VoxelRendererSubmesh submesh, Material opaque, Material transparent)
        {
            lock (Mesh)
            {
                if (Mesh.Voxels.Any(v => v.Value.Material.MaterialMode == EMaterialMode.Transparent))
                {
                    submesh.MeshRenderer.sharedMaterials = new[] { opaque, transparent, };
                }
                else if (submesh.MeshRenderer)
                {
                    submesh.MeshRenderer.sharedMaterials = new[] { opaque, };
                }
            }
        }

        public Voxel? GetVoxel(Vector3 worldPos, Vector3 worldNormal)
        {
            if (!Mesh)
            {
                return default;
            }
            var localCoord = transform.worldToLocalMatrix.MultiplyPoint3x4(worldPos);
            var localNormal = transform.worldToLocalMatrix.MultiplyVector(worldNormal)
                .ClosestAxisNormal();
            localCoord -= localNormal * .001f;
            return Mesh.Voxels.GetVoxel(localCoord, Mesh.MinLayer, Mesh.MaxLayer);
        }

        public SpriteSheet GetSpriteSheet() => SpriteSheetOverride ?? VoxelManager.Instance.GetSpriteSheet();
    }
}