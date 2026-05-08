// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MeshContainer.cs
//
// Author: Mikael Danielsson
// Date Created: 06-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    [Serializable]
    public partial class MeshContainer
    {
        public const int dataUsage = 24 + 
                                     8 + 8 + 8 + 8 + 64 + 64 + 4;

        // Stored data
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshCollider meshCollider;
        [SerializeField] private Mesh originMesh;
        [SerializeField] private long timestamp;
        [SerializeField] private int resolution;
        [SerializeField] private float autoBias = 1;
        [SerializeField] private bool onlyZResolution;
        [SerializeField] private bool autoResolution;

        // Runtime data
        [NonSerialized] private string cachedSceneName;

        // Runtime, keys
        [NonSerialized] private string meshKey;
        [NonSerialized] private string dataKey;

        // Runtime, monitor values
#if UNITY_6000_4_OR_NEWER
        [NonSerialized] private ulong monitorTransformId;
        [NonSerialized] private ulong monitorOriginMeshId;
#else
        [NonSerialized] private int monitorTransformId;
        [NonSerialized] private int monitorOriginMeshId;
#endif
        [NonSerialized] private int monitorResolutionId;
        [NonSerialized] private bool monitorOnlyZResolutionId;
        [NonSerialized] internal int monitorResolution;
        [NonSerialized] internal bool monitorOnlyZResolution;

        /// <summary>
        /// Adds vertices and triangles to the instance mesh derived from the original mesh.
        /// A mesh’s highest resolution value is usually around 14, but can vary depending on the mesh structure.
        /// </summary>
        public int Resolution
        {
            get => resolution;
            set => resolution = value;
        }
        /// <summary>
        /// Adds detail only along the Z axis, improving smoothness while keeping vertex count lower. 
        /// Requires the mesh’s Z axis to align with the spline direction. Do not rotate meshes when this option is enabled.
        /// </summary>
        public bool OnlyZResolution
        {
            get => onlyZResolution;
            set => onlyZResolution = value;
        }
        /// <summary>
        /// Enables auto resolution. The mesh resolution increases based on the curvature of the spline. 
        /// </summary>
        public bool AutoResolution
        {
            get => autoResolution;
            set => autoResolution = value;
        }
        public float AutoBias
        {
            get => autoBias;
            set
            {
                value = Mathf.Clamp(value, -10, 1.9999f);
                value = Mathf.Round(value * 100) / 100;
                autoBias = value;
            }
        }

        internal MeshContainer(Component component)
        {
            MeshFilter meshFilter = component as MeshFilter;
            MeshCollider meshCollider = component as MeshCollider;

            if (meshFilter == null && meshCollider == null)
                throw new InvalidOperationException($"Both MeshFilter and MeshCollider cant be null.");
            else if (meshFilter != null && meshCollider != null)
                throw new InvalidOperationException($"Can't contain a valid MeshCollider and MeshFilter. Can only contain one of them.");

            this.meshFilter = meshFilter;
            this.meshCollider = meshCollider;

            if (meshFilter != null)
                originMesh = meshFilter.sharedMesh;

            if (meshCollider != null)
                originMesh = meshCollider.sharedMesh;

#if UNITY_EDITOR
            EnsureValidOriginMesh();
            TryUpdateTimestamp();
#endif
        }

        public void RenderMesh(bool value)
        {
            if (meshFilter != null)
            {
                if(meshFilter.TryGetComponent(out MeshRenderer meshRender))
                    meshRender.enabled = value;
            }
        }

        public bool IsMeshRendered()
        {
            if(meshFilter != null)
            {
                if(meshFilter.TryGetComponent(out MeshRenderer meshRender))
                    return meshRender.enabled;
            }

            return false;
        }

        internal void SetInstanceMesh(Mesh instanceMesh)
        {
#if UNITY_EDITOR
            if (originMesh == null)
                return;

            if (instanceMesh == originMesh)
            {
                Debug.LogError("[Spline Architect] InstanceMesh and OriginMesh is the same!");
                return;
            }
#endif

            if (meshFilter != null) 
                meshFilter.sharedMesh = instanceMesh;
            else if (meshCollider != null) 
                meshCollider.sharedMesh = instanceMesh;
#if UNITY_EDITOR
            else
                Debug.LogError($"[Spline Architect] Could not find MeshFilter or MeshCollider for: {instanceMesh.name}");
#endif
        }

        internal void SetOriginMesh(Mesh originMesh)
        {
#if UNITY_EDITOR
            string path = GeneralUtility.GetAssetPathOnlyEditor(originMesh);
            if (path == "")
            {
                if(originMesh != null) 
                    Debug.LogError($"[Spline Architect] Can't set origin mesh! {originMesh.name} does not have an asset path!");
                else
                    Debug.LogError($"[Spline Architect] Can't set origin mesh! OriginMesh does not have an asset path!");
                return;
            }
#endif

            this.originMesh = originMesh;
        }

        public Mesh GetInstanceMesh()
        {
            if (meshCollider != null && meshCollider.sharedMesh != null)
                return meshCollider.sharedMesh;
            else if (meshFilter != null && meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh;
            else 
                return null;
        }

        public Mesh GetOriginMesh()
        {
            return originMesh;
        }

        internal void SetInstanceMeshToOriginMesh()
        {
            if (meshFilter != null)
                meshFilter.sharedMesh = originMesh;
            else if (meshCollider != null)
                meshCollider.sharedMesh = originMesh;
#if UNITY_EDITOR
            else
                Debug.LogError($"[Spline Architect] Could not find MeshFilter or MeshCollider for: {originMesh.name}");
#endif
        }

        internal Component GetMeshContainerComponent()
        {
            if (meshFilter != null) 
                return meshFilter;
            else 
                return meshCollider;
        }

        internal bool IsMeshFilter()
        {
            if (meshFilter != null) return true;
            return false;
        }

        internal bool Contains(Component component)
        {
            if (component == null)
                return false;

            if (meshFilter == component) return true;
            if (meshCollider == component) return true;
            return false;
        }

        internal bool MeshContainerExist()
        {
            if (meshCollider != null) return true;
            if (meshFilter != null) return true;
            return false;
        }

        internal string GetMeshKey()
        {
#if UNITY_6000_4_OR_NEWER
            ulong transformId = EntityId.ToULong(GetMeshContainerComponent().transform.GetEntityId());
            ulong originMeshId = EntityId.ToULong(originMesh.GetEntityId());
            if (string.IsNullOrEmpty(meshKey) || monitorTransformId != transformId ||
                                                 monitorOriginMeshId != originMeshId ||
                                                 monitorResolutionId != resolution ||
                                                 monitorOnlyZResolutionId != onlyZResolution)
            {
                UpdateKeys();
            }
#else
            if (string.IsNullOrEmpty(meshKey) || monitorTransformId != GetMeshContainerComponent().transform.GetInstanceID() ||
                                                 monitorOriginMeshId != originMesh.GetInstanceID() ||
                                                 monitorResolutionId != resolution ||
                                                 monitorOnlyZResolutionId != onlyZResolution)
            {
                UpdateKeys();
            }
#endif

            return meshKey;
        }

        internal string GetDataKey()
        {
#if UNITY_6000_4_OR_NEWER
            ulong originMeshId = EntityId.ToULong(originMesh.GetEntityId());
            if (string.IsNullOrEmpty(dataKey) || monitorOriginMeshId != originMeshId ||
                                                 monitorResolutionId != resolution ||
                                                 monitorOnlyZResolutionId != onlyZResolution)
                UpdateKeys();

            return dataKey;
#else
            if (string.IsNullOrEmpty(dataKey) || monitorOriginMeshId != originMesh.GetInstanceID() ||
                                                 monitorResolutionId != resolution || 
                                                 monitorOnlyZResolutionId != onlyZResolution)
                UpdateKeys();

            return dataKey;
#endif
        }

        internal string GetCachedSceneName()
        {
            if (cachedSceneName == null)
                cachedSceneName = GetMeshContainerComponent().gameObject.scene.name;

            return cachedSceneName;
        }

        internal void UpdateKeys()
        {
#if UNITY_6000_4_OR_NEWER
            monitorTransformId = EntityId.ToULong(GetMeshContainerComponent().transform.GetEntityId());
            monitorOriginMeshId = EntityId.ToULong(originMesh.GetEntityId());
#else
            monitorTransformId = GetMeshContainerComponent().transform.GetInstanceID();
            monitorOriginMeshId = originMesh.GetInstanceID();
#endif
            monitorResolutionId = resolution;
            monitorOnlyZResolutionId = onlyZResolution;
            string sceneName = GetCachedSceneName();
            long tstamp = timestamp;

#if !UNITY_EDITOR
            tstamp = 0;
#endif

            ulong instanceMeshHash = CreateHashForMeshKey((ulong)monitorTransformId, (ulong)monitorOriginMeshId, tstamp, monitorResolutionId, monitorOnlyZResolutionId);
            meshKey = HandleCachedResources.FeatchStringKey(instanceMeshHash);
            if(meshKey == null)
            {
                meshKey = $"{monitorTransformId}*{monitorOriginMeshId}*{tstamp}*{resolution}*{(onlyZResolution ? 1 : 0)}";
                HandleCachedResources.AddStringKey(instanceMeshHash, meshKey, sceneName);
            }

            ulong originalMeshHash = CreateHashForDataKey(sceneName, (ulong)monitorOriginMeshId, tstamp, monitorResolutionId, monitorOnlyZResolutionId);
            dataKey = HandleCachedResources.FeatchStringKey(originalMeshHash);
            if(dataKey == null)
            {
                dataKey = $"{sceneName}*{monitorOriginMeshId}*{tstamp}*{resolution}*{(onlyZResolution ? 1 : 0)}";
                HandleCachedResources.AddStringKey(originalMeshHash, dataKey, sceneName);
            }
        }

        internal ulong CreateHashForMeshKey()
        {
            long tstamp = timestamp;

#if !UNITY_EDITOR
            tstamp = 0;
#endif

#if UNITY_6000_4_OR_NEWER
            ulong transformId = EntityId.ToULong(GetMeshContainerComponent().transform.GetEntityId());
            ulong uriginMeshId = EntityId.ToULong(originMesh.GetEntityId());
            return CreateHashForMeshKey(transformId, uriginMeshId, tstamp, resolution, onlyZResolution);
#else
            int transformId = GetMeshContainerComponent().transform.GetInstanceID();
            int uriginMeshId = originMesh.GetInstanceID();
            return CreateHashForMeshKey((ulong)transformId, (ulong)uriginMeshId, tstamp, resolution, onlyZResolution);
#endif
        }

        private ulong CreateHashForMeshKey(ulong transformsInstanceId,
                                ulong originMeshId,
                                long timestamp,
                                int resolution,
                                bool onlyZ)
        { 
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;

            hash ^= 2UL;
            hash *= prime;

            hash ^= transformsInstanceId;
            hash *= prime;

            hash ^= originMeshId;
            hash *= prime;

            hash ^= (ulong)timestamp;
            hash *= prime;

            hash ^= (ulong)resolution;
            hash *= prime;

            hash ^= (ulong)(onlyZ ? 1 : 0);
            hash *= prime;

            return hash;
        }

        private ulong CreateHashForDataKey(string sceneName,
                                ulong originMeshId,
                                long timestamp,
                                int resolution,
                                bool onlyZ)
        { 
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;

            hash ^= 1UL;
            hash *= prime;

            hash ^= (ulong)sceneName.GetHashCode();
            hash *= prime;

            hash ^= originMeshId;
            hash *= prime;

            hash ^= (ulong)timestamp;
            hash *= prime;

            hash ^= (ulong)resolution;
            hash *= prime;

            hash ^= (ulong)(onlyZ ? 1 : 0);
            hash *= prime;

            return hash;
        }
    }
}