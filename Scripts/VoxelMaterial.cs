using System;
using System.Collections.Generic;
using System.Linq;

namespace Voxul
{

    [Serializable]
    public struct VoxelMaterial
    {
        public EMaterialMode MaterialMode;
        public ERenderMode RenderMode;
        public ENormalMode NormalMode;
        public SurfaceData Default;
        public List<DirectionOverride> Overrides;

        public SurfaceData GetSurface(EVoxelDirection dir)
        {
            if (Overrides != null)
            {
                var ov = Overrides.Where(o => o.Direction == dir);
                if (ov.Any())
                {
                    return ov.First().Surface;
                }
            }

            return Default;
        }

        public IEnumerable<(EVoxelDirection, SurfaceData)> GetSurfaces()
        {
            yield return (EVoxelDirection.XNeg, GetSurface(EVoxelDirection.XNeg));
            yield return (EVoxelDirection.XPos, GetSurface(EVoxelDirection.XPos));
            yield return (EVoxelDirection.YNeg, GetSurface(EVoxelDirection.YNeg));
            yield return (EVoxelDirection.YPos, GetSurface(EVoxelDirection.YPos));
            yield return (EVoxelDirection.ZNeg, GetSurface(EVoxelDirection.ZNeg));
            yield return (EVoxelDirection.ZPos, GetSurface(EVoxelDirection.ZPos));
        }

        public VoxelMaterial Copy()
        {
            return new VoxelMaterial
            {
                Default = Default,
                Overrides = Overrides?.ToList(),
                RenderMode = RenderMode,
                NormalMode = NormalMode,
                MaterialMode = MaterialMode,
            };
        }

        public override bool Equals(object obj)
        {
            return obj is VoxelMaterial material &&
                   MaterialMode == material.MaterialMode &&
                   RenderMode == material.RenderMode &&
                   NormalMode == material.NormalMode &&
                   Default.Equals(material.Default) &&
                   ((Overrides == null && material.Overrides == null) || (material.Overrides != null && !Overrides.Any(o => !material.Overrides.Contains(o))));
        }

        public override int GetHashCode()
        {
            int hashCode = 1681058014;
            hashCode = hashCode * -1521134295 + MaterialMode.GetHashCode();
            hashCode = hashCode * -1521134295 + RenderMode.GetHashCode();
            hashCode = hashCode * -1521134295 + NormalMode.GetHashCode();
            hashCode = hashCode * -1521134295 + Default.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<List<DirectionOverride>>.Default.GetHashCode(Overrides);
            return hashCode;
        }

        public void SetSurface(EVoxelDirection dir, SurfaceData surface)
        {
            if (Overrides == null)
            {
                Overrides = new List<DirectionOverride>();
            }
            var ov = new DirectionOverride
            {
                Direction = dir,
                Surface = surface,
            };
            for (var i = 0; i < Overrides.Count; i++)
            {
                var surf = Overrides[i];
                if (surf.Direction == dir)
                {
                    Overrides[i] = ov;
                    return;
                }
            }
            Overrides.Add(ov);
        }
    }
}