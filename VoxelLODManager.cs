using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(VoxelRenderer))]
[RequireComponent(typeof(LODGroup))]
public class VoxelLODManager : MonoBehaviour
{
    private LODGroup m_lodGroup => GetComponent<LODGroup>();
    private VoxelRenderer m_renderer => GetComponent<VoxelRenderer>();

	[Range(0, 1)]
	public float TopLODLevel = .1f;
    public VoxelLOD[] LODs;


	public LOD GetUnityLod(IEnumerable<VoxelLOD> lods)
	{
		return new LOD
		{
			screenRelativeTransitionHeight = lods.First().ScreenRelativeTransitionHeight,
			renderers = lods.Select(l => l.MeshRenderer).ToArray(),
		};
	}

	[ContextMenu("Rebuild")]
    public void Rebuild()
	{
		var prevLayer = gameObject.layer;
		gameObject.layer = 31;
        LODs = GetComponentsInChildren<VoxelLOD>().Where(l => l.transform.parent == transform).ToArray();
		foreach(var l in LODs)
		{
			l.MeshRenderer.enabled = false;
		}
		for (int i = 0; i < LODs.Length; i++)
		{
			VoxelLOD lod = LODs[i];
			lod.Rebuild(m_renderer);
		}

		var subLods = LODs
			.GroupBy(l => l.ScreenRelativeTransitionHeight)
			.Select(l => GetUnityLod(l));

		var lods = new[] {
			new LOD(TopLODLevel, new[] { m_renderer.MeshRenderer }) 
		}.Concat(subLods)
		.OrderByDescending(l => l.screenRelativeTransitionHeight);

		m_lodGroup.SetLODs(lods.ToArray());
		foreach (var l in LODs)
		{
			l.MeshRenderer.enabled = true;
		}
		gameObject.layer = prevLayer;
	}
}
