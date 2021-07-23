using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Voxul;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.LevelOfDetail
{
	[ExecuteAlways]
	public class RenderPlaneVoxelLOD : MonoBehaviour
	{
		[Serializable]
		public class RenderPlane
		{
			public Vector3 Position;
			public EVoxelDirection Direction;
			public Vector2 Size = Vector2.one;
			[Range(1, 100)]
			public int CastDepth = 1;

			[HideInInspector]
			public Vector2 UVMin;
			[HideInInspector]
			public Vector2 UVMax;

			public VoxelFaceCoordinate GetFace(sbyte layer) => new VoxelFaceCoordinate 
			{ 
				Direction = Direction, 
				Layer = 0, 
				Offset = Position,
				Size = Size * 2,// - Vector2.one * VoxelCoordinate.LayerToScale(layer)
			};

			public override bool Equals(object obj)
			{
				return obj is RenderPlane plane &&
					   Position.Equals(plane.Position) &&
					   Direction == plane.Direction &&
					   Size.Equals(plane.Size) &&
					   CastDepth == plane.CastDepth &&
					   UVMin.Equals(plane.UVMin) &&
					   UVMax.Equals(plane.UVMax);
			}

			public override int GetHashCode()
			{
				int hashCode = 10673697;
				hashCode = hashCode * -1521134295 + Position.GetHashCode();
				hashCode = hashCode * -1521134295 + Direction.GetHashCode();
				hashCode = hashCode * -1521134295 + Size.GetHashCode();
				hashCode = hashCode * -1521134295 + CastDepth.GetHashCode();
				hashCode = hashCode * -1521134295 + UVMin.GetHashCode();
				hashCode = hashCode * -1521134295 + UVMax.GetHashCode();
				return hashCode;
			}

			public static bool operator ==(RenderPlane left, RenderPlane right)
			{
				return EqualityComparer<RenderPlane>.Default.Equals(left, right);
			}

			public static bool operator !=(RenderPlane left, RenderPlane right)
			{
				return !(left == right);
			}
		}

		private static MaterialPropertyBlock m_propertyBlock;

		public MeshRenderer Renderer => gameObject.GetOrAddComponent<MeshRenderer>();
		public MeshFilter Filter => gameObject.GetOrAddComponent<MeshFilter>();

		public int TextureResolution = 32;
		public sbyte Layer;
		public Texture2D AlbedoData;
		public VoxelRenderer Source;
		public List<RenderPlane> RenderPlanes = new List<RenderPlane>();

		public Material Mat;
		public Mesh Mesh;

		[ContextMenu("Rebuild")]
		public void Rebuild()
		{
			if (AlbedoData == null || AlbedoData.width != TextureResolution || AlbedoData.height != TextureResolution)
			{
				if (!AlbedoData)
					AlbedoData = new Texture2D(TextureResolution, TextureResolution);
				else
					AlbedoData.Resize(TextureResolution, TextureResolution);
			}
			AlbedoData.filterMode = FilterMode.Point;
			var intData = new IntermediateVoxelMeshData();
			intData.Initialise(null, null);
			foreach (var plane in RenderPlanes)
			{
				plane.UVMax = Vector2.one;
				RenderToTexture(plane, AlbedoData, Layer);
				intData.Faces.Add(plane.GetFace(Layer), 
					new VoxelFace { MaterialMode = EMaterialMode.Opaque, RenderMode = ERenderMode.Block, Surface = new SurfaceData { UVMode = EUVMode.Local } });
			}

			VoxelMeshWorker.ConvertFacesToMesh(intData);
			if(Mesh == null)
			{
				Mesh = new Mesh();
			}

			AlbedoData.Apply();
			intData.SetMesh(Mesh);
			Filter.mesh = Mesh;
			Renderer.sharedMaterial = Mat;
		}

		public static Vector2 RotateBy(Vector2 v, float a)
		{
			var ca = System.Math.Cos(a);
			var sa = System.Math.Sin(a);
			var rx = v.x * ca - v.y * sa;

			return new Vector2((float)rx, (float)(v.x * sa + v.y * ca));
		}

		protected void RenderToTexture(RenderPlane plane, Texture2D albedo, sbyte layer)
		{
			var scale = VoxelCoordinate.LayerToScale(layer);
			var directionVec = VoxelCoordinate.DirectionToVector3(plane.Direction.FlipDirection());

			var xMin = Mathf.RoundToInt(plane.UVMin.x * albedo.width);
			var yMin = Mathf.RoundToInt(plane.UVMin.y * albedo.height);
			var xMax = Mathf.RoundToInt(plane.UVMax.x * albedo.width);
			var yMax = Mathf.RoundToInt(plane.UVMax.y * albedo.height);

			var voxels = Source.Mesh.Voxels;

			var scaledWidth = plane.Size.x ;
			var scaledHeight = plane.Size.y ;
			var scaleOffset = 0;

			for (var u = xMin; u <= xMax; u++)
			{
				var uf = (u - xMin) / (float)(xMax - xMin);

				uf -= .5f;
				uf *= 2f;

				for (var v = yMin; v <= yMax; v++)
				{
					var vf = (v - yMin) / (float)(yMax - yMin);

					vf -= .5f;
					vf *= 2f;

					var normalized2DOffset = new Vector2(uf * scaledWidth + scaleOffset, vf * scaledHeight + scaleOffset);
					//normalized2DOffset = RotateBy(normalized2DOffset, 3 * Mathf.PI / 2f);

					var raycastPoint2D = plane.Position
						.SwizzleForDir(plane.Direction, out var swizzleDiscard)
						+ normalized2DOffset;
					var raycastPoint3D = raycastPoint2D.ReverseSwizzleForDir(swizzleDiscard, plane.Direction);

					var ray = new Ray(raycastPoint3D, directionVec * plane.CastDepth * scale);
					var worldRay = new Ray(transform.localToWorldMatrix.MultiplyPoint3x4(ray.origin), transform.localToWorldMatrix.MultiplyVector(ray.direction));
					if (!voxels.Keys.Raycast(ray, out var hitCoord))
					{
						Debug.DrawRay(worldRay.origin, worldRay.direction * 10, Color.red, 5);
						albedo.SetPixel(u, v, Color.clear);
						continue;
					}
					var vox = voxels[hitCoord];
					var surf = vox.Material.GetSurface(plane.Direction.FlipDirection());
					albedo.SetPixel(u, v, surf.Albedo);

					Debug.DrawRay(worldRay.origin, worldRay.direction * 10, surf.Albedo, 5);
				}
			}
		}

		private void OnWillRenderObject()
		{
			if (AlbedoData == null)
			{
				return;
			}
			if (m_propertyBlock == null)
			{
				m_propertyBlock = new MaterialPropertyBlock();
			}
			m_propertyBlock.SetTexture("_BaseMap", AlbedoData);
			Renderer.SetPropertyBlock(m_propertyBlock);
		}
	}
}