using UnityEngine;

[System.Flags]
public enum GridUsage
{
    None = 0,
    Furniture = 1,
    Building = 2,
    Plant = 4,
}

[System.Serializable]
public class GridData
{
    public GridUsage UsageFlag;
}

public class GridPlane : MonoBehaviour
{
    [SerializeField]
    int _row = 1;
    public int Row => _row;
    [SerializeField]
    int _column = 1;
    public int Column => _column;
    [SerializeField]
    Vector2 _gridSize = Vector2.one;
    public Vector2 GridSize => _gridSize;

    // NOTE: Unity does not serialize multidimensional array!
    public GridData[] Grids;

    public static int Dimension2Index(int row, int col, int rowIndex, int colIndex)
    {
        if (rowIndex >= row || colIndex >= col)
            return -1;
        else
            return rowIndex * col + colIndex;
    }

    public static bool Index2Dimension(int row, int column, int index, out int rowIndex, out int columnIndex)
    {
        rowIndex = index / column;
        columnIndex = index % column;
        return rowIndex < row && columnIndex < column;
    }

    public void UpdateDimension(int row, int col)
    {
        var grids = new GridData[row * col];
        for (var rowIndex = 0; rowIndex < row; ++rowIndex)
        {
            for (var colIndex = 0; colIndex < col; ++colIndex)
            {
                var elementIndex = Dimension2Index(row, col, rowIndex, colIndex);
                var oldElementIndex = Dimension2Index(_row, _column, rowIndex, colIndex);
                if (oldElementIndex != -1 && Grids?.Length> oldElementIndex)
                    grids[elementIndex] = Grids[oldElementIndex];
                else
                    grids[elementIndex] = new GridData { UsageFlag = GridUsage.Plant|GridUsage.Furniture|GridUsage.Building };
            }
        }
        Grids = grids;
        _row = row;
        _column = col;
    }

    public Vector3 RowBeginPosition(int row)
    {
        return transform.position + transform.forward * row * GridSize.y;
    }

    public Vector3 RowEndPosition(int row)
    {
        return RowBeginPosition(row) + transform.right * Column * GridSize.x;
    }

    public Vector3 ColumnBeginPosition(int col)
    {
        return transform.position + transform.right * col * GridSize.x;
    }

    public Vector3 ColumnEndPosition(int col)
    {
        return ColumnBeginPosition(col) + transform.forward * Row * GridSize.y;
    }

    public Vector3 GetGridOriginInLocal(int row, int col)
    {
        var localPos = new Vector3(col * GridSize.x, 0.0f, row * GridSize.y);
        return transform.TransformPoint(localPos);
    }

    public Vector3 GetGridOrigin(int cellIndex)
    {
        if (Index2Dimension(Row, Column, cellIndex, out int rowIndex, out int columnIndex))
            return GetGridOrigin(rowIndex, columnIndex);
        return default;
    }
    public Vector3 GetGridOrigin(int row, int col)
    {
        return transform.TransformPoint(GetGridOriginInLocal(row, col));
    }
    public Vector3 GetGridCenterInLocal(int row, int col)
    {
        return GetGridOriginInLocal(row, col) + new Vector3(0.5f * _gridSize.x, 0.0f, 0.5f * _gridSize.y);
    }

    public Vector3 GetGridCenter(int row, int col)
    {
        return transform.TransformPoint(GetGridCenterInLocal(row, col));
    }

    public Vector3 GetGridCenter(int gridIndex)
    {
        if (Index2Dimension(Row, Column, gridIndex, out int rowIndex, out int columnIndex))
            return GetGridCenter(rowIndex, columnIndex);
        else
            return transform.position;
    }

    public bool GetGrid(Vector3 worldPos, out int row, out int col)
    {
        var localPos = transform.InverseTransformPoint(worldPos);
        row = Mathf.FloorToInt(localPos.y / _gridSize.y);
        col = Mathf.FloorToInt(localPos.x / _gridSize.x);
        return row >=0 && row < _row && col >= 0 && col < _column;
    }

    public int GetGrid(Vector3 worldPos)
    {
        var localPos = transform.InverseTransformPoint(worldPos);
        var rowIndex = Mathf.FloorToInt(localPos.z / _gridSize.y);
        var colIndex = Mathf.FloorToInt(localPos.x / _gridSize.x);
        var gridIndex = Dimension2Index(Row, Column, rowIndex, colIndex);
        return gridIndex < Grids.Length ? gridIndex : -1;
    }

    public int GetGrid(Ray ray)
    {
        var plane = new Plane(transform.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            var point = ray.GetPoint(enter);
            return GetGrid(point);
        }
        else
            return -1;
    }

    public bool GetGrid(Ray ray, out int row, out int col)
    {
        var plane = new Plane(transform.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            var point = ray.GetPoint(enter);
            return GetGrid(point, out row, out col);
        }
        row = 0; col = 0;
        return false;
    }
}
