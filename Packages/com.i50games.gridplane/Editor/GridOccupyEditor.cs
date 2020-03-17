using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace i50Games.GridPlane.Editor
{
    [CustomEditor(typeof(GridOccupy))]
    public class GridOccupyEditor : UnityEditor.Editor
    {
        bool _isEditing = false;

        bool _isDraging;
        Vector2 _beginPos;
        Vector2 _endPos;

        HashSet<int> _brushingCells = new HashSet<int>();

        private void OnEnable()
        {
            var gridOccupy = target as GridOccupy;
            var gridPlane = gridOccupy.GetComponentInParent<GridPlane>();
            gridOccupy.occupyCellIndexs.RemoveAll(e => !gridPlane.IsValidCellIndex(e));
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _isEditing = GUILayout.Toggle(_isEditing, EditorGUIUtility.IconContent("EditCollider"), GUI.skin.GetStyle("Button"), GUILayout.Width(35), GUILayout.Height(25));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }

        protected virtual void OnSceneGUI()
        {
            var gridOccupy = target as GridOccupy;
            var gridPlane = gridOccupy.GetComponentInParent<GridPlane>();

            // draw occupy cells
            foreach (var occupyCellIndex in gridOccupy.occupyCellIndexs)
                GridPlaneEditor.DrawCell(gridPlane, occupyCellIndex, Color.red, Color.blue);

            // edit occupy cells
            if (_isEditing)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                if (Event.current.button == 0)
                {
                    if (Event.current.type == EventType.MouseDown)
                        _beginPos = Event.current.mousePosition;
                    else if (Event.current.type == EventType.MouseDrag)
                    {
                        _endPos = Event.current.mousePosition;
                        _isDraging = true;
                    }
                    else if (Event.current.type == EventType.MouseUp)
                    {
                        if (_isDraging)
                        {
                            _isDraging = false;

                            Undo.RecordObject(gridOccupy, "Cell Occupy");
                            gridOccupy.occupyCellIndexs = new List<int>(_brushingCells);

                            _brushingCells.Clear();
                            SceneView.currentDrawingSceneView.Repaint();
                        }
                        else
                        {
                            var clickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            var cellIndex = gridPlane.GetCellIndex(clickRay);
                            if (cellIndex != -1)
                            {
                                Undo.RecordObject(gridOccupy, "Cell Occupy");
                                if (gridOccupy.occupyCellIndexs.Contains(cellIndex))
                                    gridOccupy.occupyCellIndexs.Remove(cellIndex);
                                else
                                    gridOccupy.occupyCellIndexs.Add(cellIndex);
                                SceneView.currentDrawingSceneView.Repaint();
                            }
                        }
                    }
                }
                if (_isDraging)
                {
                    Handles.BeginGUI();
                    var bodyColor = Color.gray;
                    bodyColor.a = 0.5f;

                    var dis = _endPos - _beginPos;
                    var dragRectInGUI = new Rect() { size = new Vector2(Mathf.Abs(dis.x), Mathf.Abs(dis.y)), center = Vector2.Lerp(_beginPos, _endPos, 0.5f) };

                    EditorGUI.DrawRect(dragRectInGUI, bodyColor);
                    Handles.EndGUI();
                    SceneView.currentDrawingSceneView.Repaint();

                    for (var cellIndex = 0; cellIndex < gridPlane.Cells.Length; ++cellIndex)
                    {
                        var cellCenterInWorld = gridPlane.GetCellCenter(cellIndex);
                        var cellCenterInGUI = HandleUtility.WorldToGUIPoint(cellCenterInWorld);
                        if (dragRectInGUI.Contains(cellCenterInGUI))
                        {
                            GridPlaneEditor.DrawCell(gridPlane, cellIndex, Color.green, Color.blue);
                            _brushingCells.Add(cellIndex);
                        }
                    }
                }
            }
        }
    }

}
