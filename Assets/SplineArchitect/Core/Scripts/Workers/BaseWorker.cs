// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BaseWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 14-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Jobs;

namespace SplineArchitect.Workers
{
    public abstract class BaseWorker
    {
        public WorkerState workerState { get; protected set; }
        protected Spline spline;

        protected BaseWorker(Spline spline)
        {
            this.spline = spline;
            spline?.allWorkers.Add(this);
        }

        public void SetSpline(Spline spline)
        {
            this.spline?.allWorkers.Remove(this);
            this.spline = spline;
            spline.allWorkers.Add(this);
        }

        public abstract void Start();
        public abstract void Complete();
        public abstract void CompleteWithoutAssignData();
        public abstract int GetWorkCount();
        public abstract void DisposeNativeData();

        protected DeformJob CreateDeformJob(int splineObjectCount,
                                  NativeArray<Vector3> vertices,
                                  NativeArray<Vector3> meshNormals,
                                  NativeArray<Vector4> meshTangents,
                                  NativeHashMap<int, float4x4> localSpaces,
                                  NativeArray<int> localSpaceMap,
                                  NativeArray<bool> mirrorMap,
                                  NativeArray<NormalType> soNormalTypeMap,
                                  NativeArray<bool> alignToEndMap,
                                  NativeArray<bool> skipTangentsMap,
                                  NativeArray<SnapData> snapDatas)
        {
            Vector3 splineUp = spline.SplineType == SplineType.STATIC_2D ? -Vector3.forward : Vector2.up;

            DeformJob deformJob = new DeformJob()
            {
                splineObjectCount = splineObjectCount,
                vertices = vertices,
                meshNormals = meshNormals,
                meshTangents = meshTangents,
                localSpaces = localSpaces,
                localSpaceMap = localSpaceMap,
                soNormalTypeMap = soNormalTypeMap,
                alignToEndMap = alignToEndMap,
                skipTangentsMap = skipTangentsMap,
                mirrorMap = mirrorMap,
                splineUpDirection = splineUp,
                nativeSegments = spline.NativeSegmentsLocal,
                noises = spline.NativeNoises,
                splineLength = spline.Length,
                distanceMap = spline.DistanceMap,
                normalsArray = spline.NormalsLocal,
                positionMap = spline.PositionMapLocal,
                splineResolution = spline.GetSplineResolution(),
                loop = spline.Loop,
                splineType = spline.SplineType,
                snapDatas = snapDatas,
            };

            return deformJob;
        }

        protected FollowerJob CreateFollowerJob(NativeArray<Vector3> newLocalPositions,
                                                NativeArray<Quaternion> newLocalRotations,
                                                NativeArray<Vector3> localSplinePositions,
                                                NativeArray<Quaternion> localSplineRotations,
                                                NativeArray<Quaternion> combinedParentRotations,
                                                NativeHashMap<int, float4x4> localSpaces,
                                                NativeArray<int> localSpaceMap,
                                                NativeArray<bool> alignToEndMap,
                                                NativeArray<float> lockPositions,
                                                NativeArray<Vector3Int> followAxels,
                                                NativeArray<Vector3> rightDirs,
                                                NativeArray<Vector3> upDirs,
                                                NativeArray<Vector3> forwardDirs)
        {
            Vector3 splineUp = spline.SplineType == SplineType.STATIC_2D ? -Vector3.forward : Vector2.up;

            FollowerJob followerJob = new FollowerJob()
            {
                // Out data
                newLocalPositions = newLocalPositions,
                newLocalRotations = newLocalRotations,
                rightDirs = rightDirs,
                upDirs = upDirs,
                forwardDirs = forwardDirs,

                // In data
                splineLength = spline.Length,
                splineResolution = spline.GetSplineResolution(),
                loop = spline.Loop,
                splineUpDirection = splineUp,
                splineType = spline.SplineType,
                distanceMap = spline.DistanceMap,
                positionMap = spline.PositionMapLocal,
                normalsArray = spline.NormalsLocal,
                nativeSegments = spline.NativeSegmentsLocal,
                noises = spline.NativeNoises,
                localSpaces = localSpaces,
                localSpaceMap = localSpaceMap,
                alignToEndMap = alignToEndMap,
                followAxels = followAxels,
                lockPositions = lockPositions,
                localSplinePositions = localSplinePositions,
                combinedParentRotations = combinedParentRotations,
                localSplineRotations = localSplineRotations
            };

            return followerJob;
        }
    }
}
