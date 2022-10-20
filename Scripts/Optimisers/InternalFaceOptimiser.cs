using System.Collections.Generic;
using System.Data.Metadata.Edm;
using System.Linq;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
    public class InternalFaceOptimiser : VoxelOptimiserBase
    {
        public override void OnPreFaceStep(IntermediateVoxelMeshData data)
        {
            var toRemove = new HashSet<VoxelFaceCoordinate>();

            foreach (var vox in data.Voxels)
            {
                var faces = data.CoordinateFaceMapping[vox.Key];
                for (var i = 0; i < faces.Count; i++)
                {
                    var faceCoord = faces[i];
                    if (toRemove.Contains(faceCoord))
                    {
                        continue;
                    }
                    var faceSurf = data.Faces[faceCoord];
                    if (faceSurf.RenderMode != ERenderMode.Block)
                    {
                        continue;
                    }
                    var dirVec = VoxelCoordinate.DirectionToCoordinate(faceCoord.Direction, faceCoord.Layer);
                    var coord = vox.Key + dirVec;
                    var neighbour = data.Voxels.GetVoxel(coord.ToVector3(), data.MinLayer, faceCoord.Layer);
                    if (neighbour.HasValue && neighbour.Value.Material.RenderMode == ERenderMode.Block)
                    {
                        toRemove.Add(faceCoord);
                    }
                }

                /*if (toRemove.Contains(face.Key))
                {
                    continue;
                }
                var faceCoord = face.Key;
                var faceSurf = face.Value;

                if (faceSurf.RenderMode != ERenderMode.Block)
                {
                    continue;
                }

                var neighbourFound = false;
                for (var layer = face.Key.Layer; layer >= data.MinLayer; layer--)
                {
                    VoxelCoordinate.DirectionToVector3(face.Key.Direction)
                        .RoundToVector3Int()
                        .SwizzleForDir(face.Key.Direction, out var depth);

                    var inverse = new VoxelFaceCoordinate
                    {
                        Min = face.Key.Min,
                        Max = face.Key.Max,
                        Depth = face.Key.Depth + (int)depth,
                        Direction = face.Key.Direction.FlipDirection(),
                        Layer = face.Key.Layer,
                        Offset = face.Key.Offset,
                    };
                    if (!data.Faces.TryGetValue(inverse, out var neighbour))
                    {
                        // Do more complex check?

                        continue;
                    }
                }
                if (neighbourFound)
                {
                    continue;
                }
                if (neighbour.RenderMode == ERenderMode.Block
                    && neighbour.MaterialMode == face.Value.MaterialMode)
                {
                    toRemove.Add(faceCoord);
                    toRemove.Add(inverse);
                    continue;
                }*/
            }

            foreach (var face in toRemove)
            {
                data.Faces.Remove(face);
            }
            voxulLogger.Debug($"InternalFaceOptimiser removed {toRemove.Count} faces");
        }
    }
}