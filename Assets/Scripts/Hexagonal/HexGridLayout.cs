using System.Collections.Generic;
using UnityEngine;

// Builds the SAME grid as the Java sim: a hexagon-shaped grid of radius R in axial (q, r) coordinates.
// Pointy-top hexes, so horizontal rows share the same r (matches the Java horizontal wraparound).
public class HexGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridRadius = 8;              // must equal gridRadius the brains were trained with

    [Header("Tile Settings")]
    public float outerSize = 1.0f;
    public float innerSize = 0f;
    public float height = 1f;
    public Material material;

    protected readonly Dictionary<Vector2Int, HexTile> tiles = new Dictionary<Vector2Int, HexTile>();

    private void OnEnable()
    {
        LayoutGrid();
    }

    private void LayoutGrid()
    {
        int R = gridRadius;
        for (int q = -R; q <= R; q++)
        {
            int rMin = Mathf.Max(-R, -q - R);
            int rMax = Mathf.Min(R, -q + R);
            for (int r = rMin; r <= rMax; r++)
            {
                GameObject tile = new GameObject($"Hex q:{q}, r:{r}", typeof(HexTile));
                tile.transform.position = AxialToWorld(q, r);

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = false;   // pointy-top, like the Java sim
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetCoordinates(q, r);   // x = q, y = r from now on
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();

                tile.transform.SetParent(transform, true);
                tiles[new Vector2Int(q, r)] = tile.GetComponent<HexTile>();
            }
        }
    }

    // Axial (q, r) -> world position, pointy-top
    public Vector3 AxialToWorld(int q, int r)
    {
        float x = outerSize * Mathf.Sqrt(3f) * (q + r / 2f);
        float z = outerSize * 1.5f * r;
        return new Vector3(x, 0, -z);
    }
}