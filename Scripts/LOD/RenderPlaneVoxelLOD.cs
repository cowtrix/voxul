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
			public Rect Rect
			{
				get
				{
					var bounds = new Bounds(new VoxelCoordinate(MinVec3, Layer).ToVector3(), Vector3.zero);
					bounds.Encapsulate(new VoxelCoordinate(MaxVec3, Layer).ToVector3());
					var swizzleSize = bounds.size.SwizzleForDir(Direction, out _);
					var swizzlePos = bounds.center.SwizzleForDir(Direction, out _);

					return new Rect(swizzlePos, swizzleSize);
				}
			}

			public Vector3Int MinVec3 => Min.ReverseSwizzleForDir(Offset, Direction);
			public Vector3Int MaxVec3 => Max.ReverseSwizzleForDir(Offset, Direction);

			public EVoxelDirection Direction;
			public Vector2Int Min, Max;
			public int Offset;
			[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
			public sbyte Layer;
			[Range(1, 100)]
			public int CastDepth = 1;
			public bool FlipX, FlipY;
			public Texture2D Albedo;

			public VoxelFaceCoordinate GetFace()
			{
				var bounds = new Bounds(new VoxelCoordinate(MinVec3, Layer).ToVector3(), Vector3.zero);
				bounds.Encapsulate(new VoxelCoordinate(MaxVec3, Layer).ToVector3());

				var swizzleSize = bounds.size.SwizzleForDir(Direction, out _);

				return new VoxelFaceCoordinate
				{
					Direction = Direction,
					Layer = 0,
					Offset = bounds.center,
					Size = swizzleSize,
				};
			}

			public Matrix4x4 GetMatrix()
			{
				var bounds = new VoxelCoordinate(MinVec3, Layer).ToBounds();
				bounds.Encapsulate(new VoxelCoordinate(MaxVec3, Layer).ToBounds());
				var dirVec = VoxelCoordinate.DirectionToVector3(Direction.FlipDirection()) * VoxelCoordinate.LayerToScale(Layer);
				var rot = Quaternion.LookRotation(dirVec);
				var swizzleSize = bounds.size.SwizzleForDir(Direction, out _);

				if (Direction == EVoxelDirection.XNeg || Direction == EVoxelDirection.XPos)
				{
					//swizzleSize = new Vector2(swizzleSize.y, swizzleSize.x);
					rot *= Quaternion.Euler(0, 0, -90);
				}

				return Matrix4x4.TRS(
					bounds.center - dirVec * .5f,
					rot,
					new Vector3(swizzleSize.x, swizzleSize.y, 1)
					);
			}
		}

		private void OnValidate()
		{
			if (!Source)
			{
				Source = GetComponentInParent<VoxelRenderer>();
			}
		}

		public void SnapPlane(RenderPlane plane)
		{
			if (plane == null)
			{
				return;
			}
			var layerScale = VoxelCoordinate.LayerToScale(plane.Layer);
			var objectBounds = Source.Mesh.Voxels.Keys.GetBounds();
			var size = objectBounds.size.ReverseSwizzleForDir(0, plane.Direction)
				- (Vector3.one * layerScale).ReverseSwizzleForDir(0, plane.Direction);
			var newBounds = new Bounds(objectBounds.center, size);
			plane.Min = VoxelCoordinate.FromVector3(newBounds.min, plane.Layer)
				.ToRawVector3Int()
				.SwizzleForDir(plane.Direction, out _)
				.RoundToVector2Int();
			plane.Max = VoxelCoordinate.FromVector3(newBounds.max, plane.Layer)
				.ToRawVector3Int()
				.SwizzleForDir(plane.Direction, out _)
				.RoundToVector2Int();
		}

		public void Rebake(RenderPlane plane)
		{
			var minX = plane.Min.x;
			var maxX = plane.Max.x;
			var minY = plane.Min.y;
			var maxY = plane.Max.y;

			var width = maxX - minX + 1;
			var height = maxY - minY + 1;

			if (plane.Albedo == null || !plane.Albedo)
			{
				plane.Albedo = new Texture2D(width, height);
			}
			else if (plane.Albedo.width != width || plane.Albedo.height != height)
			{
				plane.Albedo.Resize(width, height);
			}

			plane.Albedo.filterMode = FilterMode.Point;
			plane.Albedo.wrapMode = TextureWrapMode.Clamp;

			var scale = VoxelCoordinate.LayerToScale(plane.Layer);
			var directionVec = VoxelCoordinate.DirectionToVector3(plane.Direction.FlipDirection());
			var voxels = Source.Mesh.Voxels;

			for (var x = minX; x <= maxX; ++x)
			{
				for (var y = minY; y <= maxY; ++y)
				{
					var p2Dswizzled = new Vector2(x, y).ReverseSwizzleForDir(plane.Offset, plane.Direction) * scale;

					var p = VoxelCoordinate.FromVector3(p2Dswizzled, plane.Layer);
					var pVecLocal = p.ToVector3() - directionVec * scale;
					var dirVec = VoxelCoordinate.DirectionToVector3(plane.Direction.FlipDirection());
					var ray = new Ray(pVecLocal, dirVec);
					var col = Color.clear;
					var castDistance = plane.CastDepth * scale;
					//var worldRay = new Ray(transform.localToWorldMatrix.MultiplyPoint3x4(ray.origin), transform.localToWorldMatrix.MultiplyVector(ray.direction));

					if (voxels.Keys.RaycastLocal(ray, castDistance, out var vox))
					{
						var mat = voxels[vox].Material.GetSurface(plane.Direction);

						//DebugHelper.DrawPoint(worldRay.origin, .1f, mat.Albedo, 3);
						//Debug.DrawLine(worldRay.origin, worldRay.origin + worldRay.direction.normalized * castDistance, mat.Albedo, 3);
						col = mat.Albedo.WithAlpha(1);
					}
					else
					{
						//DebugHelper.DrawPoint(worldRay.origin, .1f, Color.red, 3);
						//Debug.DrawLine(worldRay.origin, worldRay.origin + worldRay.direction.normalized * castDistance, Color.red, 3);
					}

					var texX = x - minX;
					var texY = y - minY;

					if (plane.FlipX)
					{
						texX = width - texX - 1;
					}
					if (plane.FlipY)
					{
						texY = height - texY - 1;
					}

					//Debug.Log($"w = {width}\th = {height}\nx = {x}\ty = {y}\t{texX}\t{texY}\tcol = {col}", plane.Albedo);
					plane.Albedo.SetPixel(texX, texY, col);
				}
			}
			plane.Albedo.Apply();
		}

		private static MaterialPropertyBlock m_propertyBlock;

		public MeshRenderer Renderer => gameObject.GetOrAddComponent<MeshRenderer>();
		public MeshFilter Filter => gameObject.GetOrAddComponent<MeshFilter>();

		public VoxelRenderer Source;
		public List<RenderPlane> RenderPlanes = new List<RenderPlane>();

		public Material Material;
		public Mesh Mesh;
		public Vector3 MeshRotation;

		private Matrix4x4[] m_trsArrayCache;

		private void Update()
		{
			if (m_trsArrayCache == null || m_trsArrayCache.Length != RenderPlanes.Count)
			{
				m_trsArrayCache = new Matrix4x4[RenderPlanes.Count];
			}
			if (m_propertyBlock == null)
			{
				m_propertyBlock = new MaterialPropertyBlock();
			}
			for (int i = 0; i < RenderPlanes.Count; i++)
			{
				var plane = RenderPlanes[i];
				m_propertyBlock.Clear();
				if (plane.Albedo)
				{
					m_propertyBlock.SetTexture("Albedo", plane.Albedo);
				}
				m_trsArrayCache[i] = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(MeshRotation)) * plane.GetMatrix();
				Graphics.DrawMesh(Mesh, m_trsArrayCache[i], Material, 0, null, 0, m_propertyBlock);
			}
		}
	}
}