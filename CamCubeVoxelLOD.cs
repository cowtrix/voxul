using Common;
using System;
using UnityEngine;

public class CamCubeVoxelLOD : VoxelLOD 
{
	[Range(sbyte.MinValue, sbyte.MaxValue)]
	public sbyte MinLayer = sbyte.MinValue;
	[Range(sbyte.MinValue, sbyte.MaxValue)]
	public sbyte MaxLayer = sbyte.MaxValue;

	public float Scale = 1;
	public int Resolution = 16;

	public Texture2DArray TexArray
	{
		get
		{
			if(string.IsNullOrEmpty(m_texArrayReference))
			{
				return null;
			}
			VoxelManager.Instance.TextureArrayData.Data.TryGetValue(m_texArrayReference.ToString(), out var val);
			return val;
		}
		set
		{
			if(string.IsNullOrEmpty(m_texArrayReference) || m_texArrayReference == default(Guid).ToString())
			{
				m_texArrayReference = Guid.NewGuid().ToString();
			}
			VoxelManager.Instance.TextureArrayData.Data[m_texArrayReference.ToString()] = value;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(VoxelManager.Instance.TextureArrayData);
#endif
		}
	}
	[SerializeField]
	private string m_texArrayReference;

	private void OnWillRenderObject()
	{
		if(TexArray == null)
		{
			return;
		}
		if (m_propertyBlock == null)
		{
			m_propertyBlock = new MaterialPropertyBlock();
		}
		m_propertyBlock.SetTexture("Texture2DArray_735baf00590d4834930f7fb73661afe6", TexArray);
		MeshRenderer.SetPropertyBlock(m_propertyBlock);
	}

	public override void Rebuild(VoxelRenderer renderer)
	{
		var layer = renderer.gameObject.layer;
		var rot = renderer.transform.rotation;

		var bounds = new RotationalBounds(renderer.Bounds.center, renderer.Bounds.size, Quaternion.Inverse(rot)).GetAxisBounds();
		MeshFilter.sharedMesh = VoxelManager.Instance.CubeMesh;
		var up = renderer.transform.up;
		transform.localScale = bounds.size * Scale;
		transform.position = bounds.center;

		var maxLayer = renderer.MaxLayer;
		renderer.MaxLayer = MaxLayer;
		var minLayer = renderer.MinLayer;
		renderer.MinLayer = MinLayer;
		renderer.Invalidate(false);

		MeshRenderer.sharedMaterial = Resources.Load<Material>("VoxelEngine/LODMaterial");

		try
		{
			var tArray = TexArray;
			if (tArray == null || tArray.width != Resolution || tArray.height != Resolution)
			{
				TexArray = new Texture2DArray(Resolution, Resolution, 6, TextureFormat.ARGB32, false);
				tArray = TexArray;
			}

			/*if(Normal.x < 0){ Index = 0; }
			if(Normal.x > 0){ Index = 1; }

			if(Normal.y < 0){ Index = 2; }
			if(Normal.y > 0){ Index = 3; }

			if(Normal.z < 0){ Index = 4; }
			if(Normal.z > 0){ Index = 5; } */

			float bump = 1.5f;
			var left = GetTex(bounds.center + rot * Vector3.left * bounds.extents.x * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.right, up),
				bounds.size.zyx() * Scale, Vector2.one * Resolution);
			tArray.SetPixels(left.GetPixels(), 0);
			left.SafeDestroy();

			var right = GetTex(bounds.center + rot * Vector3.right * bounds.extents.x * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.left, up),
				bounds.size.zyx() * Scale, Vector2.one * Resolution);
			tArray.SetPixels(right.GetPixels(), 1);
			right.SafeDestroy();

			var bottom = GetTex(bounds.center + rot * Vector3.down * bounds.extents.y * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.up, up) * Quaternion.Euler(0, 0, 180),
				bounds.size.xzy() * Scale, Vector2.one * Resolution);
			tArray.SetPixels(bottom.GetPixels(), 2);
			bottom.SafeDestroy();

			var top = GetTex(bounds.center + rot * Vector3.up * bounds.extents.y * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.down, up) * Quaternion.Euler(0, 0, 180),
				bounds.size.xzy() * Scale, Vector2.one * Resolution);
			tArray.SetPixels(top.GetPixels(), 3);
			top.SafeDestroy();

			var back = GetTex(bounds.center + rot * Vector3.back * bounds.extents.x * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.forward, -up),
				bounds.size * Scale, Vector2.one * Resolution);
			tArray.SetPixels(back.GetPixels(), 4);
			back.SafeDestroy();

			var front = GetTex(bounds.center + rot * Vector3.forward * bounds.extents.x * Scale * bump,
				rot * Quaternion.LookRotation(Vector3.back, up),
				bounds.size * Scale, Vector2.one * Resolution);
			tArray.SetPixels(front.GetPixels(), 5);
			front.SafeDestroy();

			tArray.filterMode = FilterMode.Point;
			tArray.Apply();
		}
		catch (Exception) { throw; }
		finally
		{
			renderer.gameObject.layer = layer;
			renderer.MaxLayer = maxLayer;
			renderer.MinLayer = minLayer;
			renderer.Invalidate(false);
		}
	}
}
