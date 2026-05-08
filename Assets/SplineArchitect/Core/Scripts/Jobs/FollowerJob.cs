// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: FollowerJob.cs
//
// Author: Mikael Danielsson
// Date Created: 06-04-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using SplineArchitect.Utility;
using UnityEngine.Jobs;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct FollowerJob : IJobParallelFor
    {
        public NativeArray<Vector3> newLocalPositions;
        public NativeArray<Quaternion> newLocalRotations;
        public NativeArray<Vector3> rightDirs;
        public NativeArray<Vector3> upDirs;
        public NativeArray<Vector3> forwardDirs;

        // Spline
        [ReadOnly] public float splineLength;
        [ReadOnly] public float splineResolution;
        [ReadOnly] public bool loop;
        [ReadOnly] public Vector3 splineUpDirection;
        [ReadOnly] public SplineType splineType;
        [ReadOnly] public NativeList<float> distanceMap;
        [ReadOnly] public NativeList<Vector3> positionMap;
        [ReadOnly] public NativeList<Vector3> normalsArray;
        [ReadOnly] public NativeArray<NativeSegment> nativeSegments;
        [ReadOnly] public NativeArray<NoiseLayer> noises;

        // Follower
        [ReadOnly] public NativeArray<Vector3> localSplinePositions;
        [ReadOnly] public NativeArray<Quaternion> localSplineRotations;
        [ReadOnly] public NativeArray<Quaternion> combinedParentRotations;
        [ReadOnly] public NativeHashMap<int, float4x4> localSpaces;
        [ReadOnly] public NativeArray<int> localSpaceMap;
        [ReadOnly] public NativeArray<bool> alignToEndMap;
        [ReadOnly] public NativeArray<float> lockPositions;
        [ReadOnly] public NativeArray<Vector3Int> followAxels;

        public void Execute(int i)
        {
            Vector3 localSplinePosition = localSplinePositions[i];
            Vector3 deformPoint = localSplinePosition;
            float4x4 localSpace = localSpaces[localSpaceMap[i]];
            bool alignToEnd = alignToEndMap[i];
            bool lockPosition = lockPositions[i] > 0.01f;

            if (lockPosition)
                deformPoint = Vector3.zero;

            (Vector3, Vector3, Vector3, Vector3) tupleData = DeformPointGetNormals(i, deformPoint, localSpace, alignToEnd);

            Vector3 newLocalPosition = tupleData.Item1;
            Vector3 xDirection = tupleData.Item2;
            Vector3 yDirection = tupleData.Item3;
            Vector3 zDirection = tupleData.Item4;

            if (rightDirs.Length > 0) rightDirs[i] = xDirection;
            if (upDirs.Length > 0) upDirs[i] = yDirection;
            if (forwardDirs.Length > 0) forwardDirs[i] = zDirection;

            // Set new rotation
            if (lockPosition)
            {
                Vector3 forward = -zDirection;
                if (splineType == SplineType.DYNAMIC)
                    forward = -forward;

                Vector3 lockPoint = newLocalPosition;
                lockPoint += forward * localSplinePosition.z;
                lockPoint += yDirection * localSplinePosition.y;
                lockPoint += xDirection * localSplinePosition.x;

                Vector3 splinePosition = math.transform(localSpace, localSplinePosition);
                float fixedTime2 = SplineUtilityNative.TimeToFixedTime(distanceMap, splineResolution, splinePosition.z / splineLength, loop);
                (Vector3, Vector3, Vector3) normals2 = GetNormals(fixedTime2, alignToEnd);
                Quaternion newLocalRotation = Quaternion.LookRotation(-normals2.Item3, normals2.Item2) *
                                              localSplineRotations[i];

                if (!GeneralUtility.IsEqual(lockPositions[i], 1, 0.01f))
                {
                    (Vector3, Vector3, Vector3, Vector3) tupleData2 = DeformPointGetNormals(i, localSplinePosition, localSpace, alignToEnd);
                    lockPoint = Vector3.Lerp(tupleData2.Item1, lockPoint, lockPositions[i]);
                }

                newLocalRotations[i] = newLocalRotation;
                newLocalPositions[i] = lockPoint;
            }
            else
            {
                Vector3 forward = -zDirection;
                if (splineType == SplineType.DYNAMIC) 
                    forward = -forward;

                Quaternion localSplineRotation = Quaternion.LookRotation(forward, yDirection);
                //Set new local rotation. Order is relevant!
                Quaternion newLocalRotation = Quaternion.Inverse(combinedParentRotations[i]) *
                                  localSplineRotation *
                                  (combinedParentRotations[i] * localSplineRotations[i]);

                int axels = followAxels[i].x + followAxels[i].y + followAxels[i].z;
                if (axels != 3)
                {
                    //Save old rotation euler
                    Vector3 euler = localSplineRotations[i].eulerAngles;

                    //Set world space rotation or splineSpace rotation
                    if (followAxels[i].x == 1)
                        euler.x = newLocalRotation.eulerAngles.x;
                    if (followAxels[i].y == 1)
                        euler.y = newLocalRotation.eulerAngles.y;
                    if (followAxels[i].z == 1)
                        euler.z = newLocalRotation.eulerAngles.z;

                    newLocalRotation = Quaternion.Euler(euler);
                }

                newLocalRotations[i] = newLocalRotation;
                newLocalPositions[i] = newLocalPosition;
            }
        }

        private (Vector3, Vector3, Vector3, Vector3) DeformPointGetNormals(int i, Vector3 deformPoint, float4x4 localSpace, bool alignToEnd)
        {
            // To spline space
            deformPoint = math.transform(localSpace, deformPoint);

            if (alignToEnd) deformPoint.z = splineLength - deformPoint.z;
            float time = deformPoint.z / splineLength;
            float fixedTime = SplineUtilityNative.TimeToFixedTime(distanceMap, splineResolution, time, loop);

            int segment = Mathf.Clamp(SplineUtility.GetSegmentIndex(nativeSegments.Length, fixedTime), 1, nativeSegments.Length - 1);
            float segmentTime = SplineUtility.GetSegmentTime(segment, nativeSegments.Length, fixedTime);
            float constrastedSegementTime = SplineUtilityNative.GetContrastedSegementTime(nativeSegments, segment, segmentTime);
            if (float.IsNaN(constrastedSegementTime)) constrastedSegementTime = 1;

            //Is exstension
            Vector3 splinePoint;
            if (!loop && (time >= 1 || time <= 0)) splinePoint = SplineUtilityNative.GetPositionExtended(nativeSegments, splineLength, time);
            else splinePoint = SplineUtilityNative.GetPositionFast(positionMap, nativeSegments, splineResolution, fixedTime);

            (Vector3, Vector3, Vector3) normals = GetNormals(fixedTime, alignToEnd);
            Vector3 xDirection = normals.Item1;
            Vector3 yDirection = normals.Item2;
            Vector3 zDirection = normals.Item3;

            //Saddle skew
            Vector2 saddleSkew = SplineUtilityNative.GetSadleSkew(nativeSegments, segment, constrastedSegementTime);
            deformPoint.y += saddleSkew.y * (deformPoint.x * deformPoint.x);
            deformPoint.x += saddleSkew.x * (deformPoint.y * deformPoint.y) * -Mathf.Sign(deformPoint.x);

            //Scale
            Vector2 scale = SplineUtilityNative.GetScale(nativeSegments, segment, constrastedSegementTime);
            deformPoint.x *= scale.x;
            deformPoint.y *= scale.y;

            //NoiseLayer
            float noiseModification = SplineUtilityNative.GetNoise(nativeSegments, segment, constrastedSegementTime);
            deformPoint.y += NoiseUtility.GetNoiseValue(noises, deformPoint, noiseModification);

            //Rotation
            Quaternion rotation = Quaternion.Inverse(SplineUtilityNative.GetZRotation(nativeSegments, -zDirection, fixedTime, constrastedSegementTime));
            xDirection = rotation * xDirection;
            yDirection = rotation * yDirection;

            //Calculate the new world position
            deformPoint = splinePoint + xDirection * deformPoint.x + yDirection * deformPoint.y;

            // Set new position
            Vector3 newLocalPosition = math.transform(math.inverse(localSpace), deformPoint);

            return (newLocalPosition, xDirection, yDirection, zDirection);
        }

        private (Vector3, Vector3, Vector3) GetNormals(float fixedTime, bool alignToEnd)
        {
            Vector3 xDirection;
            Vector3 yDirection;
            Vector3 zDirection;

            //Static normals
            if (splineType != SplineType.DYNAMIC)
            {
                //Get spline direction
                zDirection = SplineUtilityNative.GetDirection(nativeSegments, fixedTime);
                if (alignToEnd) zDirection = -zDirection;

                //Calculate directions
                xDirection = Vector3.Cross(zDirection, -splineUpDirection).normalized;
                yDirection = Vector3.Cross(xDirection, -zDirection).normalized;
                zDirection = -zDirection;
            }
            //Dynamic normals
            else
            {
                float n = fixedTime * (normalsArray.Length / 3);
                int normalIndex = (int)math.floor(n);

                //Note: Need to look in to this more later. Why is this needed?
                if (normalIndex < 0 || (normalIndex * 3) + 1 >= normalsArray.Length)
                    normalIndex = Mathf.Clamp(normalIndex, 0, (normalsArray.Length / 3) - 1);

                //Get directions
                xDirection = normalsArray[normalIndex * 3];
                yDirection = normalsArray[normalIndex * 3 + 1];
                zDirection = normalsArray[normalIndex * 3 + 2];

                if (alignToEnd)
                {
                    xDirection = -xDirection;
                    zDirection = -zDirection;
                }
            }

            return (xDirection, yDirection, zDirection);
        }
    }
}
