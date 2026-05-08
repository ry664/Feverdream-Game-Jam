// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ContextMenuItems.cs
//
// Author: Mikael Danielsson
// Date Created: 31-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

#if SA_UNITY_SPLINES
using UnityEngine.Splines;
#endif

namespace SplineArchitect.Ui
{
    public static class ContextMenuItems
    {
        [MenuItem("GameObject/Spline Architect/Spline", false, 100)]
        public static void CreateSpline()
        {
            Spline spline = EHandleSpline.CreatedForContext(new GameObject());
            if (Selection.activeTransform != null)
                EHandleUndo.SetTransformParent(spline.transform, Selection.activeTransform);

            Selection.activeTransform = spline.transform;

            EHandleUndo.RecordNow(spline);
            if(EHandleSceneView.GetCurrent().in2DMode) spline.splineType = SplineType.STATIC_2D;
        }

        [MenuItem("GameObject/Spline Architect/Spline Connector", false, 101)]
        public static void CreateSplineConnector()
        {
            SplineConnector splineConnector = EHandleSplineConnector.CreatedForContext(new GameObject());
            if(Selection.activeTransform != null)
                EHandleUndo.SetTransformParent(splineConnector.transform, Selection.activeTransform);

            Selection.activeTransform = splineConnector.transform;
        }

#if SA_UNITY_SPLINES
        [MenuItem("CONTEXT/Spline/Match closest Unity spline", false, 100)]
        private static void MatchClosestUnitySpline(MenuCommand command)
        {
            Match(EHandleSelection.selectedSpline);
            foreach (Spline spline in EHandleSelection.selectedSplines) Match(spline);

            void Match(Spline spline)
            {
                if (spline == null)
                    return;

                Vector3 splineCenterPoint = spline.GetCenter();
                UnityEngine.Splines.Spline closestUnitySpline = null;
                UnityEngine.Splines.SplineContainer closestSplineContainer = null;
                float disCheck = 99999;

                SplineContainer[] splineContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                foreach (SplineContainer container in splineContainers)
                {
                    foreach (UnityEngine.Splines.Spline unitySpline in container.Splines)
                    {
                        float3 point = float3.zero;

                        foreach (BezierKnot knot in unitySpline)
                        {
                            point += knot.Position;
                        }

                        point /= unitySpline.Count;
                        point = container.transform.TransformPoint(point);

                        float dis = Vector3.Distance(point, splineCenterPoint);

                        if (dis < disCheck)
                        {
                            closestUnitySpline = unitySpline;
                            disCheck = dis;
                            closestSplineContainer = container;
                        }
                    }
                }

                EHandleUndo.RecordNow(spline);
                for (int i = 0; i < closestUnitySpline.Count; i++)
                {
                    UnityEngine.Splines.BezierKnot knot = closestUnitySpline[i];

                    Vector3 anchor = knot.Position;
                    Vector3 tangentOut = (Vector3)knot.TangentOut;
                    Vector3 tangentIn = (Vector3)knot.TangentIn;
                    Vector3 tangentA = anchor + (Vector3)math.rotate(knot.Rotation, tangentOut);
                    Vector3 tangentB = anchor + (Vector3)math.rotate(knot.Rotation, tangentIn);

                    anchor = closestSplineContainer.transform.TransformPoint(anchor);
                    tangentA = closestSplineContainer.transform.TransformPoint(tangentA);
                    tangentB = closestSplineContainer.transform.TransformPoint(tangentB);

                    if (spline.SegmentCount - 1 < i)
                    {
                        spline.CreateSegment(anchor, tangentA, tangentB);
                    }
                    else
                    {
                        Segment segment = spline.GetSegmentAtIndex(i);
                        segment.SetPosition(ControlHandle.ANCHOR, anchor);
                        segment.SetPosition(ControlHandle.TANGENT_A, tangentA);
                        segment.SetPosition(ControlHandle.TANGENT_B, tangentB);
                    }
                }

                int dif = spline.SegmentCount - closestUnitySpline.Count;

                for (int i2 = dif; i2 > 0; i2--)
                {
                    spline.RemoveSegmentAt(spline.SegmentCount - 1);
                }

                spline.SetLoop(closestUnitySpline.Closed);
            }
        }

        [MenuItem("CONTEXT/Spline/Convert To Unity Spline", false, 101)]
        private static void ConvertToUnitySpline(MenuCommand command)
        {
            Spline selectedSpline = EHandleSelection.selectedSpline;

            if (selectedSpline == null)
                return;

            // Check if spline container allready exists
            UnityEngine.Splines.SplineContainer splineContainer = selectedSpline.gameObject.GetComponent<UnityEngine.Splines.SplineContainer>();
            if (splineContainer == null)
                splineContainer = Undo.AddComponent<UnityEngine.Splines.SplineContainer>(selectedSpline.gameObject);

            Undo.RecordObject(splineContainer, $"Converted splines to a Unity Spline.");
            splineContainer.Spline = CreateUnitySpline(selectedSpline);

            for (int i = selectedSpline.AllSplineObjectCount - 1; i >= 0; i--)
            {
                SplineObject so = selectedSpline.GetSplineObjectAtIndex(i);
                Undo.DestroyObjectImmediate(so.gameObject);
            }
            Selection.activeGameObject = selectedSpline.gameObject;
            Undo.DestroyObjectImmediate(selectedSpline);

            foreach (Spline spline in EHandleSelection.selectedSplines)
            {
                splineContainer.AddSpline(CreateUnitySpline(spline));
                Undo.DestroyObjectImmediate(spline.gameObject);
            }

            UnityEngine.Splines.Spline CreateUnitySpline(Spline spline)
            {
                UnityEngine.Splines.Spline unitySpline = new UnityEngine.Splines.Spline();

                int segmentCount = spline.SegmentCount;

                for (int i = 0; i < segmentCount; i++)
                {
                    Segment segment = spline.GetSegmentAtIndex(i);

                    if (spline.Loop && (i + 2) > segmentCount)
                        break;

                    Vector3 anchor = segment.GetPosition(ControlHandle.ANCHOR);
                    Vector3 tangentA = segment.GetPosition(ControlHandle.TANGENT_A);
                    Vector3 tangentB = segment.GetPosition(ControlHandle.TANGENT_B);

                    anchor = splineContainer.transform.InverseTransformPoint(anchor);
                    tangentA = splineContainer.transform.InverseTransformPoint(tangentA);
                    tangentB = splineContainer.transform.InverseTransformPoint(tangentB);

                    Vector3 tangentIn = tangentB - anchor;
                    Vector3 tangentOut = tangentA - anchor;

                    UnityEngine.Splines.BezierKnot knot = new UnityEngine.Splines.BezierKnot(
                        anchor, tangentIn, tangentOut);

                    unitySpline.Add(knot);
                }

                unitySpline.Closed = spline.Loop;
                unitySpline.SetTangentMode(TangentMode.Continuous);
                return unitySpline;
            }
        }

        [MenuItem("CONTEXT/SplineContainer/Convert To Spline Architect spline", false, 101)]
        private static void ConvertToSpline(MenuCommand command)
        {
            UnityEngine.Splines.SplineContainer splineContainer = command.context as UnityEngine.Splines.SplineContainer;

            if (splineContainer == null || splineContainer.Splines.Count == 0)
                return;

            int count = 0;

            Spline splineToSelect = null;

            foreach (UnityEngine.Splines.Spline unitySpline in splineContainer.Splines)
            {
                if (unitySpline == null || unitySpline.Count < 2)
                    continue;

                Spline spline;
                if (count == 0)
                {
                    spline = EHandleUndo.AddComponent<Spline>(splineContainer.gameObject);
                    splineToSelect = spline;
                }
                else
                {
                    GameObject go = new GameObject($"Spline ({count})");
                    EHandleUndo.RegisterCreatedObject(go);
                    spline = EHandleUndo.AddComponent<Spline>(go);
                }

                EHandleUndo.RecordNow(spline);
                for (int i = 0; i < unitySpline.Count; i++)
                {
                    UnityEngine.Splines.BezierKnot knot = unitySpline[i];

                    Vector3 anchor = knot.Position;
                    Vector3 tangentOut = (Vector3)knot.TangentOut;
                    Vector3 tangentIn = (Vector3)knot.TangentIn;

                    Vector3 tangentA = anchor + (Vector3)math.rotate(knot.Rotation, tangentOut);
                    Vector3 tangentB = anchor + (Vector3)math.rotate(knot.Rotation, tangentIn);

                    anchor = splineContainer.transform.TransformPoint(anchor);
                    tangentA = splineContainer.transform.TransformPoint(tangentA);
                    tangentB = splineContainer.transform.TransformPoint(tangentB);
                    
                    spline.CreateSegment(anchor, tangentA, tangentB);
                }

                spline.SetLoop(unitySpline.Closed);

                count++;
            }

            for (int i = splineContainer.gameObject.GetComponentCount() - 1; i >= 0; i--)
            { 
                Component component = splineContainer.gameObject.GetComponentAtIndex(i);

                if (component is Spline || component is Transform || component is SplineContainer)
                    continue;

                Undo.DestroyObjectImmediate(component);
            }

            Undo.DestroyObjectImmediate(splineContainer);

            if (splineToSelect != null)
                Selection.activeGameObject = splineToSelect.gameObject;
        }
#endif
    }
}
