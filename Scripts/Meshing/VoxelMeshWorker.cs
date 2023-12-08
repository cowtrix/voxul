using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Voxul.Utilities;
#if UNITY_2021_1_OR_NEWER
using Voxul.Utilities.RectanglePacker;
#endif
namespace Voxul.Meshing
{
    /// <summary>
    /// The VoxelMeshWorker is responsible for taking a VoxelMesh object
    /// and transforming it into one or more Unity Meshes.
    /// </summary>
    [Serializable]
    public class VoxelMeshWorker
    {
        /// <summary>
        /// A list of the intermediate data objects which are used while the job is running.
        /// </summary>
        public List<IntermediateVoxelMeshData> IntermediateData = new List<IntermediateVoxelMeshData>();

        public VoxelMeshWorker(VoxelMesh mesh)
        {
            VoxelMesh = mesh;
        }

        /// <summary>
        /// The voxel mesh which contains this worker.
        /// </summary>
        public VoxelMesh VoxelMesh;
        public event VoxelRebuildMeshEvent OnCompleted;

        protected float m_maxCoroutineUpdateTime;
        protected ThreadHandler m_handler;

        private Guid m_lastGenID;
        private object m_threadObjectLock = new object();

        public bool IsRecalculating => m_handler != null ? m_handler.IsRecalculating : false;

        private void CancelCurrentJob() => m_handler?.Cancel();

        public void GenerateMesh(EThreadingMode mode, bool force = false, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
        {
            if (!VoxelMesh)
            {
                throw new ArgumentNullException(nameof(VoxelMesh));
            }
            voxulLogger.Debug($"VoxelMeshWorker: GenerateMesh for {VoxelMesh}", VoxelMesh);
            if (IsRecalculating)
            {
                if (!force)
                {
                    voxulLogger.Warning($"ignoring rebake for {VoxelMesh} as one is already in progress", VoxelMesh);
                    return;
                }
                CancelCurrentJob();
            }
            IntermediateData = IntermediateData ?? new List<IntermediateVoxelMeshData>();
            IntermediateData.Clear();
            if (m_handler == null)
            {
                m_handler = new ThreadHandler(VoxelMesh.name);
            }
            m_handler.Action = (m, t) => GenerateMesh(m, t, minLayer, maxLayer);
            m_handler.Start(force, mode);
        }

        public static IEnumerator DecomposeToFaces(List<KeyValuePair<VoxelCoordinate, Voxel>> allVoxels,
            List<IntermediateVoxelMeshData> intermediateVoxelMeshData,
            CancellationToken token, EThreadingMode mode,
            Guid thisJobGuid, VoxelPointMapping pointMapping,
            IEnumerable<VoxelOptimiserBase> optimisers,
            bool generateLightmaps,
            int voxelOffset = 0, float maxCoroutineTime = 1f)
        {
            // Iterate through all voxels and transform into face data
            var sw = Stopwatch.StartNew();
            int vertexCounter = 0;
            while (voxelOffset < allVoxels.Count)
            {
                var data = new IntermediateVoxelMeshData();
#if UNITY_2021_1_OR_NEWER
                data.Initialise(allVoxels, pointMapping, generateLightmaps);
#else
                data.Initialise(allVoxels, pointMapping);
#endif
                intermediateVoxelMeshData.Add(data);
                int startVoxCount = voxelOffset;
                foreach (var vox in allVoxels.Skip(startVoxCount))
                {
                    if (token.IsCancellationRequested)
                    {
                        voxulLogger.Debug($"Cancelled rebake job {thisJobGuid}");
                        yield break;
                    }
                    if (vox.Key != vox.Value.Coordinate)
                    {
                        throw new Exception($"Voxel {vox.Key} had incorrect key in data");
                    }
                    if (vox.Key.Layer < data.MinLayer)
                    {
                        data.MinLayer = vox.Key.Layer;
                    }
                    if (vox.Key.Layer > data.MaxLayer)
                    {
                        data.MaxLayer = vox.Key.Layer;
                    }
                    switch (vox.Value.Material.RenderMode)
                    {
                        case ERenderMode.Block:
                            GenerateFaces_Cube(vox.Value, data);
                            break;
                        case ERenderMode.XPlane:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg);
                            break;
                        case ERenderMode.YPlane:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.YPos, EVoxelDirection.YNeg);
                            break;
                        case ERenderMode.ZPlane:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
                            break;
                        case ERenderMode.XYCross:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
                                                                         EVoxelDirection.YPos, EVoxelDirection.YNeg);
                            break;
                        case ERenderMode.XZCross:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
                                                                         EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
                            break;
                        case ERenderMode.ZYCross:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.ZPos, EVoxelDirection.ZNeg,
                                                                         EVoxelDirection.YPos, EVoxelDirection.YNeg);
                            break;
                        case ERenderMode.FullCross:
                            GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
                                                                         EVoxelDirection.YPos, EVoxelDirection.YNeg,
                                                                         EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
                            break;
                    }
                    if (mode == EThreadingMode.Coroutine && sw.Elapsed.TotalSeconds > maxCoroutineTime)
                    {
                        // If we've spent the maximum amount of time in this frame, yield
                        sw.Restart();
                        yield return null;
                    }
                    voxelOffset++;
                    vertexCounter += vox.Value.Material.RenderMode.EstimateVertexCount();
                    /*if (vertexCounter >= 65535)
					{
						// We've reached the max vertex count more or less, so make a new renderer
						vertexCounter = 0;
						break;
					}*/
                }
                foreach (var opt in optimisers?
                    .Where(o => o != null && o.Enabled))
                {
                    opt.OnPreFaceStep(data);
                }
                ConvertFacesToMesh(data);
            }
        }

        protected virtual IEnumerator GenerateMesh(EThreadingMode mode, CancellationToken token, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
        {
            var thisJobGuid = Guid.NewGuid();
            voxulLogger.Debug($"Started rebake job {thisJobGuid}");
            m_lastGenID = thisJobGuid;
            int voxelCount = 0;
            List<KeyValuePair<VoxelCoordinate, Voxel>> allVoxels;
            VoxelPointMapping pointMapping;
            m_threadObjectLock = m_threadObjectLock ?? new object();
            lock (m_threadObjectLock)
            {
                allVoxels = VoxelMesh.Voxels
                    .Where(v => v.Key.Layer >= minLayer && v.Key.Layer <= maxLayer)
                    .OrderBy(v => v.Value.Material.MaterialMode)
                    .ToList();
                pointMapping = new VoxelPointMapping(VoxelMesh.PointMapping);
            }
            var iter = DecomposeToFaces(allVoxels, IntermediateData, token, mode, thisJobGuid, pointMapping, VoxelMesh.Optimisers.Data, VoxelMesh.GenerateLightmaps, voxelCount, m_maxCoroutineUpdateTime);
            while (iter.MoveNext())
            {
                yield return iter.Current;
            }
            foreach (var data in IntermediateData)
            {
                foreach (var opt in VoxelMesh.Optimisers.Data
                    .Where(o => o != null && o.Enabled))
                {
                    opt.OnBeforeCompleteOffThread(data);
                }
            }
            if (mode == EThreadingMode.Task)
            {
                UnityMainThreadDispatcher.Enqueue(() => Complete(thisJobGuid));
            }
            else
            {
                Complete(thisJobGuid);
            }
        }

        protected virtual void Complete(Guid jobID)
        {
            if (!VoxelMesh || m_lastGenID != jobID)
            {
                voxulLogger.Debug($"Ignored the complete for job {jobID} because ID's were different (latest is {m_lastGenID})");
                return;
            }
            voxulLogger.Debug($"Completing render job for {this}");
            lock (m_threadObjectLock)
            {
                if (VoxelMesh.UnityMeshInstances == null)
                {
                    VoxelMesh.UnityMeshInstances = new List<MeshVoxelData>();
                }
                for (int i = 0; i < IntermediateData.Count; i++)
                {
                    if (VoxelMesh.UnityMeshInstances.Count <= i)
                    {
                        VoxelMesh.UnityMeshInstances.Add(new MeshVoxelData());
                    }
                    var voxData = IntermediateData[i];
                    var meshData = VoxelMesh.UnityMeshInstances[i];
                    if (!meshData.UnityMesh
#if UNITY_EDITOR
                || (meshData.UnityMesh && UnityEditor.AssetDatabase.Contains(VoxelMesh) && !UnityEditor.AssetDatabase.Contains(meshData.UnityMesh))
#endif
            )
                    {
                        voxulLogger.Debug($"Created new mesh for {VoxelMesh}", VoxelMesh);
                        meshData.UnityMesh = new Mesh();
                        meshData.UnityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        meshData.UnityMesh.MarkDynamic();
#if UNITY_EDITOR
                        if (UnityEditor.AssetDatabase.Contains(VoxelMesh))
                        {
                            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(VoxelMesh);
                            if (meshData.UnityMesh)
                                UnityEditor.AssetDatabase.AddObjectToAsset(meshData.UnityMesh, assetPath);
                            UnityEditor.EditorUtility.SetDirty(VoxelMesh);
                        }
#endif
                    }
                    meshData.UnityMesh.name = $"{VoxelMesh.name}_mesh_{VoxelMesh.Hash}_0";
                    IntermediateVoxelMeshData result = IntermediateData[i];
                    meshData.UnityMesh = voxData.SetMesh(meshData.UnityMesh, VoxelMesh.OptimiseMesh);
                }

                for (var i = VoxelMesh.UnityMeshInstances.Count - 1; i >= IntermediateData.Count; --i)
                {
                    var m = VoxelMesh.UnityMeshInstances[i];
                    if (m != null)
                    {
                        voxulLogger.Debug($"Destroying mesh {m}");
                        m.UnityMesh.SafeDestroy();
                    }
                    VoxelMesh.UnityMeshInstances.RemoveAt(i);
                }

                IntermediateData.Clear();
            }
            OnCompleted.Invoke(VoxelMesh);
            m_handler.Release();
        }

        public virtual void Clear()
        {
            CancelCurrentJob();
            IntermediateData?.Clear();
        }

        public static void GenerateFaces_CenteredPlane(Voxel vox, IntermediateVoxelMeshData data, params EVoxelDirection[] dirs)
        {
            var map = new List<VoxelFaceCoordinate>();
            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                var surface = vox.Material.GetSurface(dir);
                if (surface.Skip)
                {
                    continue;
                }
                var vec3int = new Vector3Int(vox.Coordinate.X, vox.Coordinate.Y, vox.Coordinate.Z);
                var swizzle = vec3int.SwizzleForDir(dir, out var depthFloat).RoundToVector2Int();
                var faceCoord = new VoxelFaceCoordinate
                {
                    Min = swizzle,
                    Max = swizzle,
                    Depth = (int)depthFloat,
                    Direction = dir,
                    Layer = vox.Coordinate.Layer,
                    Offset = 0,
                };
                var face = new VoxelFace
                {
                    Surface = surface,
                    MaterialMode = vox.Material.MaterialMode,
                    RenderMode = vox.Material.RenderMode,
                    NormalMode = vox.Material.NormalMode,
                };
                data.Faces.Add(faceCoord, face);
                map.Add(faceCoord);
            }
            data.CoordinateFaceMapping[vox.Coordinate] = map;
        }

        public static void GenerateFaces_Cube(Voxel vox, IntermediateVoxelMeshData data)
        {
            var map = new List<VoxelFaceCoordinate>();
            for (int i = 0; i < VoxelExtensions.Directions.Length; i++)
            {
                EVoxelDirection dir = VoxelExtensions.Directions[i];
                var surface = vox.Material.GetSurface(dir);
                if (surface.Skip)
                {
                    continue;
                }
                var vec3int = new Vector3Int(vox.Coordinate.X, vox.Coordinate.Y, vox.Coordinate.Z);
                var swizzle = vec3int.SwizzleForDir(dir, out var depthFloat).RoundToVector2Int();
                var faceCoord = new VoxelFaceCoordinate
                {
                    Min = swizzle,
                    Max = swizzle,
                    Depth = (int)depthFloat,
                    Direction = dir,
                    Layer = vox.Coordinate.Layer,
                    Offset = .5f,
                };
                var face = new VoxelFace
                {
                    Surface = surface,
                    MaterialMode = vox.Material.MaterialMode,
                    RenderMode = vox.Material.RenderMode,
                    NormalMode = vox.Material.NormalMode,
                };
                data.Faces.Add(faceCoord, face);
                map.Add(faceCoord);
            }
            data.CoordinateFaceMapping[vox.Coordinate] = map;
        }

        public static void ConvertFacesToMesh(IntermediateVoxelMeshData data)
        {
            Vector3 GetOffset(Vector3 point)
            {
                if (data.PointOffsets != null && data.PointOffsets.TryGetValue(point, out var offset))
                {
                    return offset;
                }
                return Vector3.zero;
            }

            int counter = 0;

#if UNITY_2021_1_OR_NEWER
            const int lightmapPadding = 32;
            Dictionary<int, PackingRectangle> lightmapRects = null;
            PackingRectangle packingBounds = default;

            if (data.GenerateLightmaps)
            {
                var minLayer = data.Faces.Min(f => f.Key.Layer);
                var minlayerScale = 1 / VoxelCoordinate.LayerToScale(minLayer);
                lightmapRects = RectanglePacker.Pack(
                    data.Faces.Keys.Select(f =>
                    {
                        var w = (f.Width + 1) * 32 * VoxelCoordinate.LayerToScale(f.Layer) * minlayerScale;
                        var h = (f.Height + 1) * 32 * VoxelCoordinate.LayerToScale(f.Layer) * minlayerScale;
                        if (w > h)
                        {
                            return new Vector2(h, w);
                        }
                        return new Vector2(w, h);
                    }),
                    out packingBounds, padding: lightmapPadding);
            }
#endif

            foreach (var voxelFace in data.Faces)
            {
                var submeshIndex = (int)voxelFace.Value.MaterialMode;
                if (!data.Triangles.TryGetValue(submeshIndex, out var tris))
                {
                    tris = new List<int>(data.Voxels.Count * 16 * 3);
                    data.Triangles[submeshIndex] = tris;
                }

                var surface = voxelFace.Value.Surface;

                var layerScale = VoxelCoordinate.LayerToScale(voxelFace.Key.Layer);

                var height = voxelFace.Key.Offset * layerScale;
                var min = new VoxelCoordinate(voxelFace.Key.Min.ReverseSwizzleForDir(voxelFace.Key.Depth, voxelFace.Key.Direction), voxelFace.Key.Layer);
                var max = new VoxelCoordinate(voxelFace.Key.Max.ReverseSwizzleForDir(voxelFace.Key.Depth, voxelFace.Key.Direction), voxelFace.Key.Layer);
                var planeSize = ((voxelFace.Key.Max + Vector2.one * .5f) - (voxelFace.Key.Min - Vector2.one * .5f)) * layerScale;
                //planeSize.x /= (voxelFace.Key.Width + 1);
                //planeSize.y /= (voxelFace.Key.Height + 1);
                if (voxelFace.Key.Direction != EVoxelDirection.ZNeg && voxelFace.Key.Direction != EVoxelDirection.ZPos)
                {
                    planeSize = new Vector2(planeSize.y, planeSize.x);
                }


                var bounds = min.ToBounds();
                bounds.Encapsulate(max.ToBounds());

                var rot = VoxelCoordinate.DirectionToQuaternion(voxelFace.Key.Direction);

                // Vertices
                Vector3 v1 = bounds.center + rot * new Vector3(-planeSize.x * .5f, height, -planeSize.y * .5f);
                v1 += GetOffset(v1);
                Vector3 v2 = bounds.center + rot * new Vector3(planeSize.x * .5f, height, -planeSize.y * .5f);
                v2 += GetOffset(v2);
                Vector3 v3 = bounds.center + rot * new Vector3(planeSize.x * .5f, height, planeSize.y * .5f);
                v3 += GetOffset(v3);
                Vector3 v4 = bounds.center + rot * new Vector3(-planeSize.x * .5f, height, planeSize.y * .5f);
                v4 += GetOffset(v4);
                var vOffset = data.Vertices.Count;
                data.Vertices.AddRange(new[] { v1, v2, v3, v4 });

                switch (voxelFace.Value.NormalMode)
                {
                    case ENormalMode.Hard:
                        var normal = VoxelCoordinate.DirectionToVector3(voxelFace.Key.Direction);
                        data.Normals.AddRange(normal, normal, normal, normal);
                        break;
                    case ENormalMode.Spherical:
                        data.Normals.AddRange(v1.normalized, v2.normalized, v3.normalized, v4.normalized);
                        break;
                }

                // Triangles
                if (!data.Triangles.TryGetValue(submeshIndex, out var submeshList))
                {
                    submeshList = new System.Collections.Generic.List<int>();
                    data.Triangles[submeshIndex] = submeshList;
                }
                submeshList.Add(3 + vOffset);
                submeshList.Add(1 + vOffset);
                submeshList.Add(0 + vOffset);
                submeshList.Add(3 + vOffset);
                submeshList.Add(2 + vOffset);
                submeshList.Add(1 + vOffset);

                // Color data
                data.Color1.AddRange(Enumerable.Repeat(surface.Albedo, 4));

#if UNITY_2021_1_OR_NEWER
                // uv2 lightmap UVs
                if (data.GenerateLightmaps && lightmapRects != null)
                {
                    var rect = (Rect)lightmapRects[counter];
                    var offset = lightmapPadding / 2f;

                    var canvasW = (float)packingBounds.Width;
                    var canvasH = (float)packingBounds.Height;

                    var x = (rect.x + offset) / canvasW;
                    var y = (rect.y + offset) / canvasH;
                    var w = (rect.width - offset) / canvasW;
                    var h = (rect.height - offset) / canvasH;

                    var normalizedRect = new Rect(new Vector2(x, y), new Vector2(w, h));

                    data.UV2.Add(new Vector2(normalizedRect.xMax, normalizedRect.yMax));
                    data.UV2.Add(new Vector2(normalizedRect.xMax, normalizedRect.yMin));
                    data.UV2.Add(new Vector2(normalizedRect.xMin, normalizedRect.yMin));
                    data.UV2.Add(new Vector2(normalizedRect.xMin, normalizedRect.yMax));
                }
#else
                data.UV2.Add(new Vector2(1, 1));
                data.UV2.Add(new Vector2(1, 0));
                data.UV2.Add(new Vector2(0, 0));
                data.UV2.Add(new Vector2(0, 1));

#endif

                // UV3 extra data
                var auxData = new Vector4(surface.Smoothness, surface.Texture.Index, surface.Metallic, 1 - surface.TextureFade)
                    .RemoveNans();
                data.UV3.AddRange(Enumerable.Repeat(auxData, 4));

                float uvW = (voxelFace.Key.Height + 1);
                float uvH = (voxelFace.Key.Width + 1);
                if (voxelFace.Key.Direction != EVoxelDirection.ZNeg && voxelFace.Key.Direction != EVoxelDirection.ZPos)
                {
                    var t = uvH;
                    uvH = uvW;
                    uvW = t;
                }

                if (voxelFace.Value.Surface.UVMode == EUVMode.LocalScaled)
                {
                    uvW *= VoxelCoordinate.LayerToScale(voxelFace.Key.Layer);
                    uvH *= VoxelCoordinate.LayerToScale(voxelFace.Key.Layer);
                }

                Vector2 _00_CORDINATES = new Vector2(uvH, uvW);
                Vector2 _10_CORDINATES = new Vector2(0f, uvW);
                Vector2 _01_CORDINATES = new Vector2(uvH, 0f);
                Vector2 _11_CORDINATES = new Vector2(0f, 0f);

                
                var uvMode = surface.UVMode;
                switch (uvMode)
                {
                    case EUVMode.Local:
                        data.UV1.AddRange(_11_CORDINATES, _01_CORDINATES, _00_CORDINATES, _10_CORDINATES);
                        break;
                    case EUVMode.LocalScaled:
                        data.UV1.AddRange(_11_CORDINATES, _01_CORDINATES, _00_CORDINATES, _10_CORDINATES);
                        break;
                    case EUVMode.Global:
                        switch (voxelFace.Key.Direction)
                        {
                            case EVoxelDirection.ZNeg:
                            case EVoxelDirection.ZPos:
                                data.UV1.AddRange(v1.xy(), v2.xy(), v3.xy(), v4.xy());
                                break;
                            case EVoxelDirection.YNeg:
                            case EVoxelDirection.YPos:
                                data.UV1.AddRange(v1.xz(), v2.xz(), v3.xz(), v4.xz());
                                break;
                            case EVoxelDirection.XNeg:
                            case EVoxelDirection.XPos:
                                data.UV1.AddRange(v1.yz(), v2.yz(), v3.yz(), v4.yz());
                                break;
                        }
                        break;
                    case EUVMode.GlobalScaled:
                        switch (voxelFace.Key.Direction)
                        {
                            case EVoxelDirection.ZNeg:
                            case EVoxelDirection.ZPos:
                                data.UV1.AddRange(v1.xy() / planeSize.x, v2.xy() / planeSize.x, v3.xy() / planeSize.x, v4.xy() / planeSize.x);
                                break;
                            case EVoxelDirection.YNeg:
                            case EVoxelDirection.YPos:
                                data.UV1.AddRange(v1.xz() / planeSize.x, v2.xz() / planeSize.x, v3.xz() / planeSize.x, v4.xz() / planeSize.x);
                                break;
                            case EVoxelDirection.XNeg:
                            case EVoxelDirection.XPos:
                                data.UV1.AddRange(v1.yz() / planeSize.x, v2.yz() / planeSize.x, v3.yz() / planeSize.x, v4.yz() / planeSize.x);
                                break;
                        }
                        break;
                }

                counter++;
            }
        }

    }
}