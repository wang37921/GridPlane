using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace i50Games.GridPlane.Editor
{
    [CustomEditor(typeof(GridPlane))]
    public class GridPlaneEditor : UnityEditor.Editor
    {
        const string GizmosFurniture = "i50Games/GridPlane/Furniture";
        const string GizmosBuilding = "i50Games/GridPlane/Building";
        const string GizmosPlant = "i50Games/GridPlane/Plant";

        bool _isEditing = false;
        CellUsage _editingUsages;
        HashSet<int> _brushingCells = new HashSet<int>();

        private void OnEnable()
        {
            var gridPlane = target as GridPlane;
            if (gridPlane.Cells == null && (gridPlane.Row > 0 || gridPlane.Column > 0))
                gridPlane.UpdateDimension(gridPlane.Row, gridPlane.Column);
        }

        public override void OnInspectorGUI()
        {
            var gridPlane = target as GridPlane;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _isEditing = GUILayout.Toggle(_isEditing, EditorGUIUtility.IconContent("EditCollider"), GUI.skin.GetStyle("Button"), GUILayout.Width(35), GUILayout.Height(25));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_isEditing)
            {
                GUILayout.BeginHorizontal();

                var hasPlant = GUILayout.Toggle(_editingUsages.HasFlag(CellUsage.Plant), "作物", GUI.skin.button);
                if (hasPlant)
                    _editingUsages |= CellUsage.Plant;
                else
                    _editingUsages &= ~CellUsage.Plant;

                var hasBuilding = GUILayout.Toggle(_editingUsages.HasFlag(CellUsage.Building), "建筑", GUI.skin.button);
                if (hasBuilding)
                    _editingUsages |= CellUsage.Building;
                else
                    _editingUsages &= ~CellUsage.Building;

                var hasFurniture = GUILayout.Toggle(_editingUsages.HasFlag(CellUsage.Furniture), "家具", GUI.skin.button);
                if (hasFurniture)
                    _editingUsages |= CellUsage.Furniture;
                else
                    _editingUsages &= ~CellUsage.Furniture;

                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            GUILayout.Label("Dimension");
            var row = EditorGUILayout.IntField(gridPlane.Row, GUILayout.Width(60));
            GUILayout.Label("x", GUILayout.Width(15));
            var col = EditorGUILayout.IntField(gridPlane.Column, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            if (row != gridPlane.Row || col != gridPlane.Column)
            {
                Undo.RecordObject(gridPlane, "Update Grid Dimension");
                gridPlane.UpdateDimension(row, col);
            }

            serializedObject.Update();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cell Size");
            var cellWidth = serializedObject.FindProperty("_cellSize.x");
            cellWidth.floatValue = EditorGUILayout.IntField((int)cellWidth.floatValue, GUILayout.Width(60));
            GUILayout.Label("x", GUILayout.Width(15));
            var cellHeight = serializedObject.FindProperty("_cellSize.y");
            cellHeight.floatValue = EditorGUILayout.IntField((int)cellHeight.floatValue, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        bool _isDraging;
        Vector2 _beginPos;
        Vector2 _endPos;

        protected virtual void OnSceneGUI()
        {
            var gridPlane = target as GridPlane;
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

                            foreach (var cellIndex in _brushingCells)
                            {
                                Undo.RecordObject(gridPlane, "Cell Usage");
                                gridPlane.Cells[cellIndex].UsageFlag = _editingUsages;
                            }
                            _brushingCells.Clear();
                        }
                        else
                        {
                            var clickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            var cellIndex = gridPlane.GetCellIndex(clickRay);
                            if (cellIndex != -1)
                            {
                                Undo.RecordObject(gridPlane, "Cell Usage");
                                gridPlane.Cells[cellIndex].UsageFlag = _editingUsages;
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
                        var gridCenterInGUI = HandleUtility.WorldToGUIPoint(cellCenterInWorld);
                        if (dragRectInGUI.Contains(gridCenterInGUI))
                        {
                            DrawCell(gridPlane, cellIndex, Color.green, Color.blue);
                            _brushingCells.Add(cellIndex);
                        }
                    }
                }
            }
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Active)]
        public static void DrawGizmos(GridPlane gridPlane, GizmoType gizmoType)
        {
            Gizmos.color = Color.black;
            for (var row = 0; row <= gridPlane.Row; ++row)
                Gizmos.DrawLine(gridPlane.RowBeginPosition(row), gridPlane.RowEndPosition(row));
            for (var col = 0; col <= gridPlane.Column; ++col)
                Gizmos.DrawLine(gridPlane.ColumnBeginPosition(col), gridPlane.ColumnEndPosition(col));
            Gizmos.color = Color.white;

            for (var cellIndex = 0; cellIndex != gridPlane.Cells.Length; ++cellIndex)
            {
                var cell = gridPlane.Cells[cellIndex];
                var cellOrigin = gridPlane.GetCellOrigin(cellIndex) + new Vector3(1, 0, 1) * 0.25f;
                if (cell != null)
                {
                    if (cell.UsageFlag.HasFlag(CellUsage.Plant))
                        Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 0), GizmosPlant);
                    if (cell.UsageFlag.HasFlag(CellUsage.Furniture))
                        Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 1), GizmosFurniture);
                    if (cell.UsageFlag.HasFlag(CellUsage.Building))
                        Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 2), GizmosBuilding);
                }
                else
                {
                    Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 0), GizmosPlant);
                    Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 1), GizmosFurniture);
                    Gizmos.DrawIcon(GetCellItemPosition(cellOrigin, gridPlane.CellSize.x, gridPlane.CellSize.y, Vector3.right, Vector3.forward, 3, 2), GizmosBuilding);
                }
            }
        }

        static Vector3 GetCellItemPosition(Vector3 originPos, float cellWidth, float cellHeight,
            Vector3 mainAxis, Vector3 crossAxis,
            int itemCount, int itemIndex)
        {
            var rectDimension = 1;
            while (true)
            {
                if (itemCount <= rectDimension * rectDimension)
                    break;
                else
                    ++rectDimension;
            }
            var row = itemIndex / rectDimension;
            var col = itemIndex % rectDimension;
            return originPos + mainAxis * col / rectDimension * cellWidth + crossAxis * row / rectDimension * cellHeight;
        }
        public static void DrawCell(GridPlane gridPlane, int cellIndex, Color fillup, Color outline)
        {
            var cellCenterInWorld = gridPlane.GetCellCenter(cellIndex);
            var cellHalfSize = gridPlane.CellSize / 2.0f;
            Vector3[] verts = new Vector3[]
            {
            cellCenterInWorld - gridPlane.transform.right * cellHalfSize.x - gridPlane.transform.forward * cellHalfSize.y,
            cellCenterInWorld - gridPlane.transform.right * cellHalfSize.x + gridPlane.transform.forward * cellHalfSize.y,
            cellCenterInWorld + gridPlane.transform.right * cellHalfSize.x + gridPlane.transform.forward * cellHalfSize.y,
            cellCenterInWorld + gridPlane.transform.right * cellHalfSize.x - gridPlane.transform.forward * cellHalfSize.y,
            };
            var rectColor = fillup;
            rectColor.a = 0.2f;
            var outlineColor = outline;
            outlineColor.a = 0.5f;
            Handles.DrawSolidRectangleWithOutline(verts, rectColor, outlineColor);
        }
    }

}