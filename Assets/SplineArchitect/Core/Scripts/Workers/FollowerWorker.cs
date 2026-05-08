// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: FollowerWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 23-12-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Jobs;

namespace SplineArchitect.Workers
{
    internal class FollowerWorker : BaseWorker
    {
        // Out data
        private NativeArray<Vector3> newLocalPositions;
        private NativeArray<Quaternion> newLocalRotations;

        // In data
        private NativeArray<int> localSpaceMap;
        private NativeArray<Quaternion> combinedParentRotations;
        private NativeArray<Quaternion> localSplineRotations;
        private NativeArray<Vector3> localSplinePositions;
        private NativeHashMap<int, float4x4> localSpaces;
        private NativeArray<bool> alignToEndMap;
        private NativeArray<float> lockPositions;
        private NativeArray<Vector3Int> followAxels;

        // Empty data
        private NativeArray<Vector3> rightDirs;
        private NativeArray<Vector3> upDirs;
        private NativeArray<Vector3> forwardDirs;

        private JobHandle jobHandle;
        private FollowerJob followerJob;
        private List<SplineObject> splineObjects;
        private HashSet<SplineObject> splineObjectsSet;
        private int oldsplineObjectsCount = -1;

        public FollowerWorker(Spline spline = null) : base(spline)
        {
            splineObjects = new List<SplineObject>();
            splineObjectsSet = new HashSet<SplineObject>();
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
                Debug.LogWarning($"FollowerWorker allready working! Can't add splineObject {so.name} to worker.");
                return;
            }

            if (so.Type != SplineObjectType.FOLLOWER)
            {
                Debug.LogWarning($"Can't add {so.name} so Follower Worker becouse becouse it's not a follower.");
                return;
            }
#endif

            if(splineObjectsSet.Contains(so))
                return;

            splineObjects.Add(so);
            splineObjectsSet.Add(so);
            workerState = WorkerState.NOT_EMPTY;
        }

        public void Remove(SplineObject so)
        {
            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so2 = splineObjects[i];

                if (so2 == so)
                {
                    splineObjects[i] = null;
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

            if (workerState == WorkerState.EMPTY || workerState == WorkerState.WORKING)
                return;

            EnsureNativeCapacity();

            if (spline.RootSplineObjectCount == spline.AllSplineObjectCount)
            {
                localSpaces.Add(0, float4x4.identity);
                for (int i = 0; i < splineObjects.Count; i++)
                {
                    SplineObject so = splineObjects[i];
                    localSpaceMap[i] = 0;
                    alignToEndMap[i] = so.AlignToEnd;
                    combinedParentRotations[i] = Quaternion.identity;
                    localSplineRotations[i] = so.localSplineRotation;
                    localSplinePositions[i] = so.localSplinePosition;
                    lockPositions[i] = so.LockPosition;
                    followAxels[i] = so.FollowAxels;
                }
            }
            else
            {
                for (int i = 0; i < splineObjects.Count; i++)
                {
                    SplineObject so = splineObjects[i];

                    int combinedParentHashCodes = SplineObjectUtility.GetCombinedParentHashCodes(so);
                    if (!localSpaces.ContainsKey(combinedParentHashCodes))
                        localSpaces.Add(combinedParentHashCodes, SplineObjectUtility.GetCombinedParentMatrixs(so.SoParent));

                    localSpaceMap[i] = combinedParentHashCodes;
                    alignToEndMap[i] = so.AlignToEnd;
                    combinedParentRotations[i] = SplineObjectUtility.GetCombinedParentRotations(so.SoParent);
                    localSplineRotations[i] = so.localSplineRotation;
                    localSplinePositions[i] = so.localSplinePosition;
                    lockPositions[i] = so.LockPosition;
                    followAxels[i] = so.FollowAxels;
                }
            }

            followerJob = CreateFollowerJob(
                newLocalPositions,
                newLocalRotations,
                localSplinePositions,
                localSplineRotations,
                combinedParentRotations,
                localSpaces,
                localSpaceMap,
                alignToEndMap,
                lockPositions,
                followAxels,
                rightDirs,
                upDirs,
                forwardDirs
            );

            jobHandle = followerJob.Schedule(splineObjects.Count, 1);
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

            for (int i = 0; i < splineObjects.Count; i++)
            {
                SplineObject so = splineObjects[i];

                if (so == null)
                    continue;

                so.transform.SetLocalPositionAndRotation(followerJob.newLocalPositions[i], followerJob.newLocalRotations[i]);
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
            return splineObjects.Count;
        }

        public override void DisposeNativeData()
        {
            jobHandle.Complete();

            if (localSplinePositions.IsCreated)
            {
                // Out data
                newLocalPositions.Dispose();
                newLocalRotations.Dispose();

                // In data
                localSpaceMap.Dispose();
                alignToEndMap.Dispose();
                localSpaces.Dispose();
                combinedParentRotations.Dispose();
                localSplineRotations.Dispose();
                localSplinePositions.Dispose();
                followAxels.Dispose();
                lockPositions.Dispose();

                // Empty data
                rightDirs.Dispose();
                upDirs.Dispose();
                forwardDirs.Dispose();
            }
        }

        private void Reset()
        {
            if(localSpaces.IsCreated)
                localSpaces.Clear();

            splineObjects.Clear();
            splineObjectsSet.Clear();
            workerState = WorkerState.EMPTY;
        }

        private void EnsureNativeCapacity()
        {
            if (oldsplineObjectsCount < splineObjects.Count)
            {
                oldsplineObjectsCount = splineObjects.Count;

                DisposeNativeData();

                // Out data
                newLocalPositions = new NativeArray<Vector3>(splineObjects.Count, Allocator.Persistent);
                newLocalRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);

                // In data
                localSpaceMap = new NativeArray<int>(splineObjects.Count, Allocator.Persistent);
                alignToEndMap = new NativeArray<bool>(splineObjects.Count, Allocator.Persistent);
                localSpaces = new NativeHashMap<int, float4x4>(splineObjects.Count, Allocator.Persistent);
                localSplineRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);
                localSplinePositions = new NativeArray<Vector3>(splineObjects.Count, Allocator.Persistent);
                combinedParentRotations = new NativeArray<Quaternion>(splineObjects.Count, Allocator.Persistent);
                followAxels = new NativeArray<Vector3Int>(splineObjects.Count, Allocator.Persistent);
                lockPositions = new NativeArray<float>(splineObjects.Count, Allocator.Persistent);

                // Empty data
                rightDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
                upDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
                forwardDirs = new NativeArray<Vector3>(0, Allocator.Persistent);
            }
        }
    }
}
