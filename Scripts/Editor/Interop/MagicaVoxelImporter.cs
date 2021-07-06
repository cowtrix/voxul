using CsharpVoxReader;
using CsharpVoxReader.Chunks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit.Interop
{
	public class MagicaVoxelImportUtility : IVoxLoader
	{
		public sbyte Layer;
		public Dictionary<byte, VoxelMaterial> Pallete { get; private set; } = new Dictionary<byte, VoxelMaterial>();
		public Vector3Int Size;
		public byte[,,] Data;

		public VoxelMesh GetResult(VoxelMesh mesh)
		{
			if (!mesh)
			{
				mesh = ScriptableObject.CreateInstance<VoxelMesh>();
			}
			mesh.Voxels.Clear();
			for (var x = 0; x < Size.x; ++x)
			{
				for (var y = 0; y < Size.y; ++y)
				{
					for (var z = 0; z < Size.z; ++z)
					{
						var val = Data[x, y, z];
						var mat = GetMaterial(val);
						if (mat.Default.Albedo.a == 0)
						{
							continue;
						}
						var coord = new VoxelCoordinate(x, y, z, Layer);
						var vox = new Voxel(coord, mat);
						mesh.Voxels[vox.Coordinate] = vox;
					}
				}
			}
			voxulLogger.Debug($"Imported {mesh.Voxels.Count} voxels from MagicaVoxel");
			mesh.Invalidate();
			return mesh;
		}

		private VoxelMaterial GetMaterial(byte palletIndex)
		{
			if (Pallete.TryGetValue(palletIndex, out var mat))
			{
				return mat;
			}
			var p = CsharpVoxReader.Chunks.Palette.DefaultPalette[palletIndex];
			p.ToARGB(out var a, out var r, out var g, out var b);
			var m = new VoxelMaterial
			{
				Default = new SurfaceData
				{
					Albedo = new UnityEngine.Color32(r, g, b, a)
				}
			};
			Pallete[palletIndex] = m;
			return m;
		}

		public void LoadModel(int sizeX, int sizeY, int sizeZ, byte[,,] data)
		{
			Size = new Vector3Int(sizeX, sizeY, sizeZ);
			Data = data;
		}

		public void LoadPalette(uint[] palette)
		{
			Pallete.Clear();
			for (var i = 0; i < palette.Length; i++)
			{
				var p = palette[i];
				p.ToARGB(out var a, out var r, out var g, out var b);
				Pallete[(byte)i] = new VoxelMaterial
				{
					Default = new SurfaceData
					{
						Albedo = new UnityEngine.Color32(r, g, b, a)
					}
				};
			}
		}

		public void NewGroupNode(int id, Dictionary<string, byte[]> attributes, int[] childrenIds)
		{
			throw new System.NotImplementedException();
		}

		public void NewLayer(int id, Dictionary<string, byte[]> attributes)
		{
			throw new System.NotImplementedException();
		}

		public void NewMaterial(int id, Dictionary<string, byte[]> attributes)
		{
			throw new System.NotImplementedException();
		}

		public void NewShapeNode(int id, Dictionary<string, byte[]> attributes, int[] modelIds, Dictionary<string, byte[]>[] modelsAttributes)
		{
			throw new System.NotImplementedException();
		}

		public void NewTransformNode(int id, int childNodeId, int layerId, Dictionary<string, byte[]>[] framesAttributes)
		{
			throw new System.NotImplementedException();
		}

		public void SetMaterialOld(int paletteId, MaterialOld.MaterialTypes type, float weight, MaterialOld.PropertyBits property, float normalized)
		{
			throw new System.NotImplementedException();
		}

		public void SetModelCount(int count)
		{
			throw new System.NotImplementedException();
		}
	}
}