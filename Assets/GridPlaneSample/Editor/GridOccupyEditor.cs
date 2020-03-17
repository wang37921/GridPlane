using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


[CustomEditor(typeof(GridOccupy))]
public class GridOccupyEditor : Editor
{
    bool _isEditing = false;

    bool _isDraging;
    Vector2 _beginPos;
    Vector2 _endPos;

    HashSet<int> _brushingGrids = new HashSet<int>();

    private void OnEnable()
    {
        var gridOccupy = target as GridOccupy;
        var gridPlane = gridOccupy.GetComponentInParent<GridPlane>();
        gridOccupy.occupyCellIndexs.RemoveWhere(e => !gridPlane.IsValidCellIndex(e));
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
            DrawCell(occupyCellIndex, Color.red, Color.blue);

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
                        gridOccupy.occupyCellIndexs = new HashSet<int>(_brushingGrids);

                        _brushingGrids.Clear();
                        SceneView.currentDrawingSceneView.Repaint();
                    }
                    else
                    {
                        var clickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        var gridIndex = gridPlane.GetGrid(clickRay);
                        if (gridIndex != -1)
                        {
                            Undo.RecordObject(gridOccupy, "Cell Occupy");
                            if (gridOccupy.occupyCellIndexs.Contains(gridIndex))
                                gridOccupy.occupyCellIndexs.Remove(gridIndex);
                            else
                                gridOccupy.occupyCellIndexs.Add(gridIndex);
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
                        DrawCell(gridIndex, Color.green, Color.blue);
                        _brushingGrids.Add(gridIndex);
                    }
                }
            }
        }
    }

    void DrawCell(int cellIndex, Color fillup, Color outline)
    {
        var gridOccupy = target as GridOccupy;
        var gridPlane = gridOccupy.GetComponentInParent<GridPlane>();
        var gridCenterInWorld = gridPlane.GetGridCenter(cellIndex);
        var gridHalfSize = gridPlane.GridSize / 2.0f;
        Vector3[] verts = new Vector3[]
        {
            gridCenterInWorld - gridPlane.transform.right * gridHalfSize.x - gridPlane.transform.forward * gridHalfSize.y,
            gridCenterInWorld - gridPlane.transform.right * gridHalfSize.x + gridPlane.transform.forward * gridHalfSize.y,
            gridCenterInWorld + gridPlane.transform.right * gridHalfSize.x + gridPlane.transform.forward * gridHalfSize.y,
            gridCenterInWorld + gridPlane.transform.right * gridHalfSize.x - gridPlane.transform.forward * gridHalfSize.y,
        };
        var rectColor = fillup;
        rectColor.a = 0.2f;
        var outlineColor = outline;
        outlineColor.a = 0.5f;
        Handles.DrawSolidRectangleWithOutline(verts, rectColor, outlineColor);
    }
}
