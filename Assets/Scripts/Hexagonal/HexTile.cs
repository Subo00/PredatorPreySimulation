using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HexTile : HexRenderer
{
    public int Q => x;   // HexRenderer's x/y now hold axial q/r
    public int R => y;

    

    void OnMouseEnter()
    {
        meshRenderer.material.color = Color.red;
    }

    void OnMouseExit()
    {
        meshRenderer.material.color = foodColor;
    }

    void OnMouseDown()
    {
        GameManager.Instance.OnTileClicked(this);
    }

    // White = no grass, green = full grass
    public void ShowFood(int food)
    {
        foodColor = Color.Lerp(defaultColor, Color.green, (float)food / HexCell.FOOD_MAX);
        meshRenderer.material.color = foodColor;
    }
}
