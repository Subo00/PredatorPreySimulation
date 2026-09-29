using UnityEngine;

// Visual grid (tiles in the scene). The simulation logic lives in SimGrid (port of HexGrid.java).
public class HexGrid : HexGridLayout
{
    public HexTile GetTileAtPos(int q, int r)
    {
        tiles.TryGetValue(new Vector2Int(q, r), out HexTile tile);
        return tile;
    }
}
