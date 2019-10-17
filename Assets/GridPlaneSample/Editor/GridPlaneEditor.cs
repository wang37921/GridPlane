using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


[CustomEditor(typeof(GridPlane))]
public class GridPlaneEditor : Editor
{
    bool _isEditing = false;
    GridUsage _editingUsages;
    HashSet<int> _brushingGrids = new HashSet<int>();

    private void OnEnable()
    {
        var gridPlane = target as GridPlane;
        if (gridPlane.Grids == null && (gridPlane.Row > 0 || gridPlane.Column > 0))
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

            var hasPlant = GUILayout.Toggle(_editingUsages.HasFlag(GridUsage.Plant), "作物", GUI.skin.button);
            if (hasPlant)
                _editingUsages |= GridUsage.Plant;
            else
                _editingUsages &= ~GridUsage.Plant;

            var hasBuilding = GUILayout.Toggle(_editingUsages.HasFlag(GridUsage.Building), "建筑", GUI.skin.button);
            if (hasBuilding)
                _editingUsages |= GridUsage.Building;
            else
                _editingUsages &= ~GridUsage.Building;

            var hasFurniture = GUILayout.Toggle(_editingUsages.HasFlag(GridUsage.Furniture), "家具", GUI.skin.button);
            if (hasFurniture)
                _editingUsages |= GridUsage.Furniture;
            else
                _editingUsages &= ~GridUsage.Furniture;

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
        GUILayout.Label("Grid Size");
        var gridWidth = serializedObject.FindProperty("_gridSize.x");
        gridWidth.floatValue = EditorGUILayout.IntField((int)gridWidth.floatValue, GUILayout.Width(60));
        GUILayout.Label("x", GUILayout.Width(15));
        var gridHeight = serializedObject.FindProperty("_gridSize.y");
        gridHeight.floatValue = EditorGUILayout.IntField((int)gridHeight.floatValue, GUILayout.Width(60));
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

                        foreach (var gridIndex in _brushingGrids)
                        {
                            Undo.RecordObject(gridPlane, "Grid Usage");
                            gridPlane.Grids[gridIndex].UsageFlag = _editingUsages;
                        }
                        _brushingGrids.Clear();
                    }
                    else
                    {
                        var clickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        var gridIndex = gridPlane.GetGrid(clickRay);
                        if (gridIndex != -1)
                        {
                            Undo.RecordObject(gridPlane, "Grid Usage");
                            gridPlane.Grids[gridIndex].UsageFlag = _editingUsages;
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

                for (var gridIndex = 0; gridIndex < gridPlane.Grids.Length; ++gridIndex)
                {
                    var gridCenterInWorld = gridPlane.GetGridCenter(gridIndex);
                    var gridCenterInGUI = HandleUtility.WorldToGUIPoint(gridCenterInWorld);
                    if (dragRectInGUI.Contains(gridCenterInGUI))
                    {
                        var gridHalfSize = gridPlane.GridSize / 2.0f;
                        Vector3[] verts = new Vector3[]
                        {
                            gridCenterInWorld - gridPlane.transform.right * gridHalfSize.x - gridPlane.transform.forward * gridHalfSize.y,
                            gridCenterInWorld - gridPlane.transform.right * gridHalfSize.x + gridPlane.transform.forward * gridHalfSize.y,
                            gridCenterInWorld + gridPlane.transform.right * gridHalfSize.x + gridPlane.transform.forward * gridHalfSize.y,
                            gridCenterInWorld + gridPlane.transform.right * gridHalfSize.x - gridPlane.transform.forward * gridHalfSize.y,
                        };
                        var rectColor = Color.green;
                        rectColor.a = 0.2f;
                        var outlineColor = Color.blue;
                        outlineColor.a = 0.5f;
                        Handles.DrawSolidRectangleWithOutline(verts, rectColor, outlineColor);
                        _brushingGrids.Add(gridIndex);
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

        for (var gridIndex = 0; gridIndex != gridPlane.Grids.Length; ++gridIndex)
        {
            var grid = gridPlane.Grids[gridIndex];
            var gridCenter = gridPlane.GetGridCenter(gridIndex);
            if (grid != null)
            {
                if (grid.UsageFlag.HasFlag(GridUsage.Plant))
                    Gizmos.DrawIcon(gridCenter - gridPlane.transform.right * 0.3f, "plant");
                if (grid.UsageFlag.HasFlag(GridUsage.Furniture))
                    Gizmos.DrawIcon(gridCenter, "furniture");
                if (grid.UsageFlag.HasFlag(GridUsage.Building))
                    Gizmos.DrawIcon(gridCenter + gridPlane.transform.right * 0.3f, "building");
            }
            else
            {
                Gizmos.DrawIcon(gridCenter - gridPlane.transform.right * 0.3f, "plant");
                Gizmos.DrawIcon(gridCenter, "furniture");
                Gizmos.DrawIcon(gridCenter + gridPlane.transform.right * 0.3f, "building");
            }
        }
    }
}
