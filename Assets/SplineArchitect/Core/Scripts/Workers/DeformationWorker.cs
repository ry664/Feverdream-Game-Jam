// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: DeformationWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 02-07-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Workers
{
    internal class DeformationWorker : BaseWorker
    {
        public const int MAX_VERTICES = 30000;

        private struct SplineObjectSnapShot
        {
            public SplineObject so;
            public Vector3 localSplinePosition;
            public Quaternion localSplineRotation;
            public SnapData snapData;
            public bool mirrorDeformation;
            public bool alignToEnd;
            public bool skipTangents;
            public NormalType normalType;
            public int indexStart;
            public int indexEnd;

            public SplineObjectSnapShot(SplineObject so, int indexStart, int indexEnd)
            {
                snapData = new SnapData();
                mirrorDeformation = false;
                alignToEnd = false;
                skipTangents = false;
                normalType = NormalType.SPLINE_SPACE;

                if (so == null)
                {
                    this.so = null;
                    localSplinePosition = Vector3.zero;
                    localSplineRotation = Quaternion.identity;
                }
                else
                {
                    this.so = so;

                    // General
                    localSplinePosition = so.localSplinePosition;
                    localSplineRotation = so.localSplineRotation;
                    mirrorDeformation = so.MirrorDeformation;
                    alignToEnd = so.AlignToEnd;
                    skipTangents = so.SkipTangents;
                    normalType = so.NormalType;

                    // Snap data
                    if (so.SnapSettings.snapMode != SnapMode.NONE) 
                        snapData = so.CalculateSnapData();
                }

                this.indexStart = indexStart;
                this.indexEnd = indexEnd;
            }
        }

        private struct MeshContainerSnapShot
        {
            public MeshContainer mc;
            public Mesh instanceMesh;
            public Vector3[] originVertices;
            public Vector3[] originNormals;
            public Vector4[] originTangents;

            public MeshContainerSnapShot(MeshContainer mc, Mesh instanceMesh)
            {
                this.mc = mc;
                this.instanceMesh = instanceMesh;
                originVertices = HandleCachedResources.FetchOriginVertices(mc);
                originNormals = HandleCachedResources.FetchOriginNormals(mc);
                originTangents = HandleCachedResources.FetchOriginTangents(mc);
            }
        }

        public int totalVertices { get; private set; }

        private int oldTotalVertices = -1;
        private int oldSnapShotsCount = -1;

        private NativeArray<Vector3> vertices;
        private NativeArray<Vector3> meshNormals;
        private NativeArray<Vector4> meshTangents;
        private NativeHashMap<int, float4x4> localSpaces;
        private NativeArray<int> localSpaceMap;
        private NativeArray<bool> skipTangents;
        private NativeArray<bool> mirrorMap;
        private NativeArray<NormalType> soNormalTypeMap;
        private NativeArray<bool> alignToEndMap;
        private NativeArray<SnapData> snapDatas;

        private JobHandle jobHandle;
        private DeformJob deformJob;
        private HashSet<SplineObject> splineObjectsSet;
        private List<SplineObjectSnapShot> snapShots;
        private List<SplineObjectSnapShot> emptySnapShots;
        private List<MeshContainerSnapShot> mcSnapShots;

        public DeformationWorker(Spline spline = null) : base(spline)
        {
            splineObjectsSet = new HashSet<SplineObject>();
            snapShots = new List<SplineObjectSnapShot>();
            emptySnapShots = new List<SplineObjectSnapShot>();
            mcSnapShots = new List<MeshContainerSnapShot>();
        }

        public void Add(SplineObject so)
        {
#if UNITY_EDITOR
            if (so.SplineParent == null)
            {
                Debug.LogWarning($"[Spline Architect] Tried to add SplineObject {so.name} with null splineParent.");
                return;
            }

            if(workerState == WorkerState.WORKING)
            {
                Debug.LogWarning("DeforamtionWorker allready working! Can't add splineObject to worker.");
                return;
            }

            if (so.Type != SplineObjectType.DEFORMATION)
            {
                Debug.LogWarning($"Can't add {so.name} to Deformation Worker becouse it's not a deformation.");
                return;
            }
#endif

            if (splineObjectsSet.Contains(so))
                return;

            for(int i2 = 0; i2 < so.MeshContainerCount; i2++)
            {
                MeshContainer mc = so.GetMeshContainerAtIndex(i2);
                mc.UpdateKeys();
            }

            so.AdjustAutoResolution();

            int vertices = 0;
            Mesh meshFilterMesh = null;
            if(so.MeshContainerCount > 0)
                meshFilterMesh = HandleCachedResources.FetchInstanceMesh(so.GetMeshContainerAtIndex(0));

            int indexStart = mcSnapShots.Count;
            int indexEnd = mcSnapShots.Count;
            for (int i = 0; i < so.MeshContainerCount; i++)
            {
                MeshContainer mc = so.GetMeshContainerAtIndex(i);
                Mesh instanceMesh = HandleCachedResources.FetchInstanceMesh(mc);

                if (instanceMesh == null)
                    continue;

                //If mesh colliders uses the same mesh as the mesh filter, skip.
                if (i > 0 && meshFilterMesh != null && meshFilterMesh == instanceMesh)
                    continue;

                mcSnapShots.Add(new MeshContainerSnapShot(mc, instanceMesh));
                vertices += instanceMesh.vertexCount;
                indexEnd++;
            }

            if(vertices == 0) emptySnapShots.Add(new SplineObjectSnapShot(so, indexStart, indexEnd));
            else snapShots.Add(new SplineObjectSnapShot(so, indexStart, indexEnd));

            totalVertices += vertices;
            splineObjectsSet.Add(so);

            if (workerState == WorkerState.EMPTY)
                workerState = WorkerState.NOT_EMPTY;

            if (totalVertices > MAX_VERTICES)
                workerState = WorkerState.FULL;
        }

        public void Remove(SplineObject so)
        {
            for (int i = 0; i < snapShots.Count; i++)
            {
                SplineObject soCompare = snapShots[i].so;

                if (soCompare == so)
                {
                    SplineObjectSnapShot snapShot = new SplineObjectSnapShot(null, 0, 0);
                    snapShots[i] = snapShot;
                    return;
                }
            }

            for (int i = 0; i < emptySnapShots.Count; i++)
            {
                SplineObject soCompare = emptySnapShots[i].so;

                if (soCompare == so)
                {
                    SplineObjectSnapShot emptySnapShot = new SplineObjectSnapShot(null, 0, 0);
                    emptySnapShots[i] = emptySnapShot;
                    return;
                }
            }
        }

        public bool Contains(SplineObject so)
        {
            return splineObjectsSet.Contains(so);
        }

        public void Deform(SplineObject so)
        {
            Add(so);
            Complete();
        }

        public override void Start()
        {
            if (spline == null || spline.segments.Count < 2)
            {
                Reset();
                return;
            }

            if (workerState == WorkerState.EMPTY)
                return;

            if(workerState == WorkerState.WORKING)
                return;

            EnsureNativeCapacity();

            if (snapShots.Count > 0)
            {
                int offset = 0;

                for (int i = 0; i < snapShots.Count; i++)
                {
                    SplineObjectSnapShot snapShot = snapShots[i];
                    SplineObject so = snapShot.so;
#if UNITY_EDITOR
                    if (so == null)
                    {
                        Reset();
                        Debug.LogWarning("[Spline Architect] Found null splineObject, deformation job aborted!");
                        return;
                    }
#endif
                    localSpaces.Add(i, SplineObjectUtility.GetCombinedParentMatrixs(so));

                    //MeshContainers
                    for (int i2 = snapShot.indexStart; i2 < snapShot.indexEnd; i2++)
                    {
                        MeshContainerSnapShot mcSnapShot = mcSnapShots[i2];

                        if (mcSnapShot.instanceMesh == null)
                            continue;

                        Vector3[] originVertices = mcSnapShot.originVertices;
                        NativeArray<Vector3>.Copy(originVertices, 0, vertices, offset, originVertices.Length);

                        Vector3[] originNormals = mcSnapShot.originNormals;
                        NativeArray<Vector3>.Copy(originNormals, 0, meshNormals, offset, originNormals.Length);

                        Vector4[] originTangents = mcSnapShot.originTangents;
                        NativeArray<Vector4>.Copy(originTangents, 0, meshTangents, offset, originTangents.Length);

                        offset += originVertices.Length;
                    }

                    mirrorMap[i] = snapShot.mirrorDeformation;
                    skipTangents[i] = snapShot.skipTangents;
                    alignToEndMap[i] = snapShot.alignToEnd;
                    soNormalTypeMap[i] = so.NormalType;
                    snapDatas[i] = snapShot.snapData;
                    localSpaceMap[i] = offset;
                }

                deformJob = CreateDeformJob(snapShots.Count,
                            vertices,
                            meshNormals,
                            meshTangents,
                            localSpaces,
                            localSpaceMap,
                            mirrorMap,
                            soNormalTypeMap,
                            alignToEndMap,
                            skipTangents,
                            snapDatas);

                int batchCount = Mathf.Max(deformJob.vertices.Length / 1500, 1);
                jobHandle = deformJob.Schedule(deformJob.vertices.Length, batchCount);
            }

            workerState = WorkerState.WORKING;
        }

        public override void Complete()
        {
            if ((int)workerState > 1)
                Start();

            if (workerState != WorkerState.WORKING)
            {
                Reset();
                return;
            }

            jobHandle.Complete();

            int verticesId = 0;

            for (int i = 0; i < snapShots.Count; i++)
            {
                SplineObjectSnapShot snapShot = snapShots[i];
                SplineObject so = snapShot.so;

                if (so == null)
                    continue;
#if UNITY_EDITOR
                Vector3 combinedScale = SplineObjectUtility.GetCombinedParentScales(so);
                if (GeneralUtility.IsZero(combinedScale.x) ||
                    GeneralUtility.IsZero(combinedScale.y) ||
                    GeneralUtility.IsZero(combinedScale.z))
                    continue;
#endif
                so.transform.SetLocalPositionAndRotation(snapShot.localSplinePosition, snapShot.localSplineRotation);

                for (int i2 = snapShot.indexStart; i2 < snapShot.indexEnd; i2++)
                {
                    MeshContainerSnapShot mcSnapShot = mcSnapShots[i2];
                    Mesh originMesh = mcSnapShot.mc.GetOriginMesh();
                    Mesh instanceMesh = mcSnapShot.instanceMesh;

                    if (instanceMesh == null)
                        continue;

                    //Vertices
                    NativeArray<Vector3> vertices = deformJob.vertices.GetSubArray(verticesId, instanceMesh.vertexCount);

                    // Start bounds job
                    BoundsJob boundsJob = new BoundsJob(){
                        result = new NativeArray<Vector3>(2, Allocator.TempJob),
                        points = vertices};
                    JobHandle jobHandle = boundsJob.Schedule();

                    // Set vertecies
                    instanceMesh.SetVertices(vertices);

                    //Normals
                    if ((int)so.NormalType < 2)
                    {
                        NativeArray<Vector3> meshNormals = deformJob.meshNormals.GetSubArray(verticesId, instanceMesh.vertexCount);
                        instanceMesh.SetNormals(meshNormals);

                        if (!so.SkipTangents)
                        {
                            NativeArray<Vector4> meshTangents = deformJob.meshTangents.GetSubArray(verticesId, instanceMesh.vertexCount);
                            instanceMesh.SetTangents(meshTangents);
                        }
                    }
                    else if(so.NormalType == NormalType.UNITY_CALCULATED || so.NormalType == NormalType.UNITY_CALCULATED_SEAMLESS)
                    {
                        instanceMesh.RecalculateNormals();

                        if (!so.SkipTangents)
                            instanceMesh.RecalculateTangents();
                    }

                    if (mcSnapShot.mc.GetInstanceMesh() != instanceMesh || !mcSnapShot.mc.IsMeshFilter())
                        mcSnapShot.mc.SetInstanceMesh(instanceMesh);

                    //Updated colliders using the same mesh as meshFilter.
                    if (mcSnapShot.mc.IsMeshFilter())
                    {
                        for (int i3 = 1; i3 < so.MeshContainerCount; i3++)
                        {
                            MeshContainer mc2 = so.GetMeshContainerAtIndex(i3);
                            Mesh instanceMesh2 = mc2.GetInstanceMesh();
                            Mesh originMesh2 = mc2.GetOriginMesh();

                            if (instanceMesh2 == null || originMesh2 == null) 
                                continue;

                            if (originMesh == originMesh2 && 
                                mcSnapShot.mc.Resolution == mc2.Resolution && 
                                mcSnapShot.mc.OnlyZResolution == mc2.OnlyZResolution)
                            {
                                mc2.SetInstanceMesh(instanceMesh);
                            }
                        }
                    }

                    // Complete bounds job
                    jobHandle.Complete();
                    Bounds b = new Bounds();
                    b.SetMinMax(boundsJob.result[0], boundsJob.result[1]);
                    instanceMesh.bounds = b;
                    boundsJob.result.Dispose();

                    verticesId += instanceMesh.vertexCount;
                }
            }

            for (int i = 0; i < emptySnapShots.Count; i++)
            {
                SplineObjectSnapShot emptySnapShot = emptySnapShots[i];
                SplineObject emptySo = emptySnapShot.so;

                if (emptySo == null)
                    continue;

                emptySo.transform.SetLocalPositionAndRotation(emptySnapShot.localSplinePosition, emptySnapShot.localSplineRotation);
            }

            Reset();
        }

        public override void CompleteWithoutAssignData()
        {
            jobHandle.Complete();
            Reset();
        }

        public override int GetWorkCount()
        {
            return snapShots.Count + emptySnapShots.Count;
        }

        public override void DisposeNativeData()
        {
            if(vertices.IsCreated)
            {
                vertices.Dispose();
                meshNormals.Dispose();
                meshTangents.Dispose();
                localSpaces.Dispose();
                localSpaceMap.Dispose();
                mirrorMap.Dispose();
                skipTangents.Dispose();
                soNormalTypeMap.Dispose();
                alignToEndMap.Dispose();
                snapDatas.Dispose();
            }
        }

        private void Reset()
        {
            workerState = WorkerState.EMPTY;
            totalVertices = 0;

            if (localSpaces.IsCreated)
                localSpaces.Clear();

            splineObjectsSet.Clear();
            snapShots.Clear();
            emptySnapShots.Clear();
            mcSnapShots.Clear();
        }

        private void EnsureNativeCapacity()
        {
            if(totalVertices > oldTotalVertices || snapShots.Count > oldSnapShotsCount)
            {
                oldSnapShotsCount = snapShots.Count;
                oldTotalVertices = totalVertices;

                DisposeNativeData();

                //Out data
                vertices = new NativeArray<Vector3>(totalVertices, Allocator.Persistent);
                meshNormals = new NativeArray<Vector3>(totalVertices, Allocator.Persistent);
                meshTangents = new NativeArray<Vector4>(totalVertices, Allocator.Persistent);

                //In data
                localSpaceMap = new NativeArray<int>(snapShots.Count, Allocator.Persistent);
                mirrorMap = new NativeArray<bool>(snapShots.Count, Allocator.Persistent);
                skipTangents = new NativeArray<bool>(snapShots.Count, Allocator.Persistent);
                soNormalTypeMap = new NativeArray<NormalType>(snapShots.Count, Allocator.Persistent);
                alignToEndMap = new NativeArray<bool>(snapShots.Count, Allocator.Persistent);
                snapDatas = new NativeArray<SnapData>(snapShots.Count, Allocator.Persistent);
                localSpaces = new NativeHashMap<int, float4x4>(snapShots.Count, Allocator.Persistent);
            }
        }
    }
}
