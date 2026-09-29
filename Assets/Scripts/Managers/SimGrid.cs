using System;
using System.Collections.Generic;

// C# port of HexGrid.java (logic only; renamed so it doesn't clash with the Unity HexGrid).
// Filled hexagon of the given radius in axial coordinates.
public class SimGrid
{
    readonly Dictionary<(int, int), HexCell> cells = new Dictionary<(int, int), HexCell>();
    public int Radius { get; }

    public SimGrid(int radius, int initialFood)
    {
        Radius = radius;
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Math.Max(-radius, -q - radius);
            int r2 = Math.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
                cells[(q, r)] = new HexCell(q, r, initialFood);
        }
    }

    // Double-buffered: every cell sees last tick's food values
    public void TickFoodGrowth()
    {
        foreach (var cell in cells.Values) cell.ComputeNextFood(GetNeighbors(cell));
        foreach (var cell in cells.Values) cell.ApplyNextFood();
    }

    public HexCell GetCell(int q, int r) => cells.TryGetValue((q, r), out var c) ? c : null;
    public bool InBounds(int q, int r) => cells.ContainsKey((q, r));
    public IEnumerable<HexCell> GetAllCells() => cells.Values;
    public int Size => cells.Count;

    public HexCell[] GetNeighbors(HexCell cell)
    {
        var result = new List<HexCell>(6);
        foreach (var c in cell.NeighborCoords())
        {
            var n = GetCell(c[0], c[1]);
            if (n != null) result.Add(n);
        }
        return result.ToArray();
    }

    // Left/right wraparound within the same row (cylinder topology)
    public int[] WrapHorizontal(int q, int r)
    {
        int qMin = Math.Max(-Radius, -r - Radius);
        int qMax = Math.Min(Radius, -r + Radius);
        if (q > qMax) return new[] { qMin, r };
        if (q < qMin) return new[] { qMax, r };
        return new[] { q, r };
    }
}
