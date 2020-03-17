using UnityEngine;

namespace i50Games.GridPlane
{
    [System.Flags]
    public enum CellUsage
    {
        None = 0,
        Furniture = 1,
        Building = 2,
        Plant = 4,
    }

    [System.Serializable]
    public class CellData
    {
        public CellUsage UsageFlag;
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
        Vector2 _cellSize = Vector2.one;
        public Vector2 CellSize => _cellSize;

        // NOTE: Unity does not serialize multidimensional array!
        public CellData[] Cells;

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
            var cells = new CellData[row * col];
            for (var rowIndex = 0; rowIndex < row; ++rowIndex)
            {
                for (var colIndex = 0; colIndex < col; ++colIndex)
                {
                    var elementIndex = Dimension2Index(row, col, rowIndex, colIndex);
                    var oldElementIndex = Dimension2Index(_row, _column, rowIndex, colIndex);
                    if (oldElementIndex != -1 && Cells?.Length > oldElementIndex)
                        cells[elementIndex] = Cells[oldElementIndex];
                    else
                        cells[elementIndex] = new CellData { UsageFlag = CellUsage.Plant | CellUsage.Furniture | CellUsage.Building };
                }
            }
            Cells = cells;
            _row = row;
            _column = col;
        }

        public Vector3 RowBeginPosition(int row)
        {
            return transform.position + transform.forward * row * CellSize.y;
        }

        public Vector3 RowEndPosition(int row)
        {
            return RowBeginPosition(row) + transform.right * Column * CellSize.x;
        }

        public Vector3 ColumnBeginPosition(int col)
        {
            return transform.position + transform.right * col * CellSize.x;
        }

        public Vector3 ColumnEndPosition(int col)
        {
            return ColumnBeginPosition(col) + transform.forward * Row * CellSize.y;
        }

        public Vector3 GetCellOriginInLocal(int row, int col)
        {
            var localPos = new Vector3(col * CellSize.x, 0.0f, row * CellSize.y);
            return transform.TransformPoint(localPos);
        }

        public Vector3 GetCellOrigin(int cellIndex)
        {
            if (Index2Dimension(Row, Column, cellIndex, out int rowIndex, out int columnIndex))
                return GetCellOrigin(rowIndex, columnIndex);
            return default;
        }
        public Vector3 GetCellOrigin(int row, int col)
        {
            return transform.TransformPoint(GetCellOriginInLocal(row, col));
        }
        public Vector3 GetCellCenterInLocal(int row, int col)
        {
            return GetCellOriginInLocal(row, col) + new Vector3(0.5f * _cellSize.x, 0.0f, 0.5f * _cellSize.y);
        }

        public Vector3 GetCellCenter(int row, int col)
        {
            return transform.TransformPoint(GetCellCenterInLocal(row, col));
        }

        public Vector3 GetCellCenter(int cellIndex)
        {
            if (Index2Dimension(Row, Column, cellIndex, out int rowIndex, out int columnIndex))
                return GetCellCenter(rowIndex, columnIndex);
            else
                return transform.position;
        }

        public bool GetCell(Vector3 worldPos, out int row, out int col)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            row = Mathf.FloorToInt(localPos.y / _cellSize.y);
            col = Mathf.FloorToInt(localPos.x / _cellSize.x);
            return row >= 0 && row < _row && col >= 0 && col < _column;
        }

        public int GetCellIndex(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            var rowIndex = Mathf.FloorToInt(localPos.z / _cellSize.y);
            var colIndex = Mathf.FloorToInt(localPos.x / _cellSize.x);
            var cellIndex = Dimension2Index(Row, Column, rowIndex, colIndex);
            return cellIndex < Cells.Length ? cellIndex : -1;
        }

        public int GetCellIndex(Ray ray)
        {
            var plane = new Plane(transform.up, transform.position);
            if (plane.Raycast(ray, out float enter))
            {
                var point = ray.GetPoint(enter);
                return GetCellIndex(point);
            }
            else
                return -1;
        }

        public bool GetCell(Ray ray, out int row, out int col)
        {
            var plane = new Plane(transform.up, transform.position);
            if (plane.Raycast(ray, out float enter))
            {
                var point = ray.GetPoint(enter);
                return GetCell(point, out row, out col);
            }
            row = 0; col = 0;
            return false;
        }

        public bool IsValidCellIndex(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < _row * _column;
        }
    }

}
