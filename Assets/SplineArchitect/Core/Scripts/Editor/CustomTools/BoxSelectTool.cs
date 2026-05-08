// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BoxSelectTool.cs
//
// Author: Mikael Danielsson
// Date Created: 11-04-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Ui;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using SplineUtility = SplineArchitect.Utility.SplineUtility;

namespace SplineArchitect.CustomTools
{
    public class BoxSelectTool
    {
        private const float dragMinSize = 16;

        public static bool active;

        private static Vector2 startPoint;
        private static bool startPointSet;
        private static bool deselectControlPoint = true;
        private static Rect selectionRect;
        private static List<int> anchorContainer = new List<int>();
        private static int oldSelectedControlPoint;
        private static bool anchorContainerIsSet;
        private static bool commandActive;
        private static int hotControlId;
        private static GUIContent controlPointsOnlyText = new GUIContent("Control points only");
        private static GUIContent outsideSceneViewTest = new GUIContent("Outside scene view");

        public static bool IsUpdating(Event e)
        {
            if (PositionTool.activePart != PositionTool.ActivePart.NONE)
                return false;

            Spline selectedSpline = EHandleSelection.selectedSpline;
            hotControlId = GUIUtility.GetControlID(FocusType.Passive);

            // Deactivate
            if ((e.type == EventType.MouseUp && GUIUtility.hotControl == hotControlId) || selectedSpline == null)
            {
                if(active)
                {
#if UNITY_6000_0_OR_NEWER
                    if(selectedSpline != null)
                        e.Use();
#elif UNITY_EDITOR_WIN 
                    if(selectedSpline != null)
                        e.Use();
#else
                    EHandleSceneView.RepaintCurrent();
#endif
                }

                startPointSet = false;
                active = false;
                deselectControlPoint = true;
                anchorContainerIsSet = false;
                selectionRect = new Rect();
                commandActive = false;

                if(GUIUtility.hotControl == hotControlId)
                    GUIUtility.hotControl = 0;

                return false;
            }

            if(e.type != EventType.MouseDrag || e.button != 0)
                return false;

            SplineObject selectedSplineObject = EHandleSelection.selectedSplineObject;

            // Set hot control
            if(selectedSpline != null && selectedSplineObject == null && GUIUtility.hotControl == 0)
            {
                GUIUtility.hotControl = hotControlId;
            }

            // Run logic
            if (hotControlId == GUIUtility.hotControl)
            {
                // Get start point on box
                if (!startPointSet)
                {
                    startPoint = e.mousePosition;
                    startPointSet = true;
                }

                // Need to be over min drag else not active
                if (Mathf.Abs(startPoint.x - e.mousePosition.x) > dragMinSize || Mathf.Abs(startPoint.y - e.mousePosition.y) > dragMinSize)
                {
                    active = true;                
                }

                e.Use();

                //Stop if box is to small
                if (!active)
                    return false;

                if (!anchorContainerIsSet)
                {
                    anchorContainer.Clear();
                    anchorContainer.AddRange(selectedSpline.selectedAnchors);
                    oldSelectedControlPoint = selectedSpline.selectedControlPoint;
                    anchorContainerIsSet = true;
                    commandActive = EHandleModifier.CtrlActive(e);
                }

                EHandleUndo.RecordNow(selectedSpline, "Control point multiselect", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);
                selectedSpline.selectedAnchors.Clear();

                if (commandActive)
                {
                    selectedSpline.selectedAnchors.AddRange(anchorContainer);

                    if (selectedSpline.selectedControlPoint != oldSelectedControlPoint)
                    {
                        selectedSpline.selectedControlPoint = oldSelectedControlPoint;
                        EHandleTool.ActivatePositionToolForControlPoint(selectedSpline);
                    }
                }

                if (!commandActive && deselectControlPoint) 
                    selectedSpline.selectedControlPoint = 0;

                if (!EHandleSceneView.MousePositionInsideSeneView(EHandleSceneView.GetCurrent(), e))
                    return false;

                for (int i = 0; i < selectedSpline.SegmentCount; i++)
                {
                    Segment s = selectedSpline.GetSegmentAtIndex(i);
                    Vector2 guiPoint = HandleUtility.WorldToGUIPoint(s.GetPosition(ControlHandle.ANCHOR));

                    if (selectionRect.Contains(guiPoint))
                    {
                        int controlPointId = SplineUtility.SegmentIndexToControlPointId(i, ControlHandle.ANCHOR);
                        bool containsOriginal = anchorContainer.Contains(controlPointId);
                        bool containsInCurrent = selectedSpline.selectedAnchors.Contains(controlPointId);

                        if (!commandActive)
                        {
                            if (selectedSpline.selectedControlPoint == 0)
                            {
                                selectedSpline.selectedControlPoint = controlPointId;
                                EHandleTool.ActivatePositionToolForControlPoint(selectedSpline);
                            }
                            else
                                selectedSpline.selectedAnchors.Add(controlPointId);
                        }
                        else
                        {
                            if (controlPointId == oldSelectedControlPoint)
                                selectedSpline.selectedControlPoint = 0;
                            else if (!containsOriginal && !containsInCurrent)
                                selectedSpline.selectedAnchors.Add(controlPointId);
                            else if (containsOriginal && containsInCurrent)
                                selectedSpline.selectedAnchors.Remove(controlPointId);
                        }
                        WindowExtended.RepaintAll();
                    }
                }

                if (selectedSpline.selectedControlPoint == 0 && selectedSpline.selectedAnchors.Count > 0)
                {
                    selectedSpline.selectedControlPoint = selectedSpline.selectedAnchors[0];
                    selectedSpline.selectedAnchors.Remove(selectedSpline.selectedControlPoint);
                    EHandleTool.ActivatePositionToolForControlPoint(selectedSpline);
                }

                return true;
            }

            return false;
        }

        public static void Draw(Event e)
        {
            if (!active)
                return;

            bool mousePointInsideSceneView = EHandleSceneView.MousePositionInsideSeneView(EHandleSceneView.GetCurrent(), e);

            selectionRect = GetRect(startPoint, e.mousePosition);
            Rect innerRect = new Rect(selectionRect.x + 1, 
                                      selectionRect.y + 1, 
                                      selectionRect.width - 2, 
                                      selectionRect.height - 2);

            Handles.BeginGUI();
            Color oldColor = GUI.color;

            // Draw box
            if(!mousePointInsideSceneView) GUI.color = new Color(1, 0.4f, 0.2f, 1);
            GUI.Box(selectionRect, GUIContent.none, EditorStyles.selectionRect);

            // Info text
            GUIStyle style = GUI.skin.label;
            GUIContent infoText = controlPointsOnlyText;
            if (!mousePointInsideSceneView) infoText = outsideSceneViewTest;
            Vector2 size = style.CalcSize(infoText);
            Rect labelRect = new Rect(selectionRect.x, selectionRect.y - size.y, size.x, size.y);

            GUI.color = new Color(0, 0, 0, 0.66f);
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(labelRect, infoText, style);

            GUI.color = oldColor;
            Handles.EndGUI();

            Rect GetRect(Vector2 a, Vector2 b)
            {
                Vector2 min = Vector2.Min(a, b);
                Vector2 max = Vector2.Max(a, b);
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
    }
}
