using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Voxul.Utilities;

namespace Voxul
{
    /// <summary>
    /// This is the handler for managing a MeshFilter, MeshRenderer and MeshCollider
    /// with voxel data.
    /// TODO: in order to better support in-scene selection, the first renderer submesh
    /// should be itself.
    /// </summary>
    [ExecuteAlways]
    public class VoxelRendererSubmesh : MonoBehaviour
    {
        private static MaterialPropertyBlock MaterialPropertyBlock { get; set; }
        public UnityEvent OnRender;

        public VoxelRenderer Parent;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public MeshCollider MeshCollider;

        private void Start()
        {
            SetPropertyBlock();
        }

        private void OnWillRenderObject()
        {
            OnRender?.Invoke();
            SetPropertyBlock();
        }

        public void SetupComponents(VoxelRenderer r, bool collider)
        {
            if (!this)
            {
                voxulLogger.Warning("Attempting to setup VoxelRendererSubmesh but it has been destroyed.");
                return;
            }
            Parent = r;
            gameObject.hideFlags = gameObject == Parent.gameObject ? Parent.gameObject.hideFlags : HideFlags.HideInHierarchy;
            this.hideFlags = Parent.hideFlags;
            if (gameObject != Parent.gameObject)
            {
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;
            }
            if (!MeshFilter)
            {
                MeshFilter = gameObject.GetOrAddComponent<MeshFilter>();
            }
            if (!MeshRenderer)
            {
                MeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
            }
            MeshFilter.hideFlags = Parent.hideFlags;
            MeshRenderer.hideFlags = Parent.hideFlags;
            if (collider)
            {
                if (!MeshCollider)
                {
                    MeshCollider = gameObject.GetOrAddComponent<MeshCollider>();
                }
                if (MeshCollider.convex)
                {
                    MeshCollider.convex = false;
                }
            }
            else if (MeshCollider)
            {
                MeshCollider.enabled = false;
            }
            SetPropertyBlock();
        }

        public void SetPropertyBlock()
        {
            if (MaterialPropertyBlock == null)
            {
                MaterialPropertyBlock = new MaterialPropertyBlock();
            }
            MeshRenderer.GetPropertyBlock(MaterialPropertyBlock);

            var spriteSheet = Parent.GetSpriteSheet();
            if (spriteSheet && spriteSheet.AlbedoTextureArray)
            {
                MaterialPropertyBlock.SetTexture("AlbedoSpritesheet", spriteSheet.AlbedoTextureArray);
            }
            MeshRenderer.SetPropertyBlock(MaterialPropertyBlock);
        }

        public Bounds Bounds => MeshRenderer.bounds;

        /*private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                return;
            }
            MeshFilter.SafeDestroy();
            MeshRenderer.SafeDestroy();
            MeshCollider.SafeDestroy();
        }*/

#if UNITY_EDITOR
        internal void SetDirty()
        {
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.EditorUtility.SetDirty(this);
            if (MeshRenderer)
                UnityEditor.EditorUtility.SetDirty(MeshRenderer);
            if (MeshFilter)
                UnityEditor.EditorUtility.SetDirty(MeshFilter);
            if (MeshCollider)
                UnityEditor.EditorUtility.SetDirty(MeshCollider);
        }
#endif
    }
}