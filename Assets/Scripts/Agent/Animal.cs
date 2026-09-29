using UnityEngine;

// Visual only: follows its Agent (which lives in the Simulation).
public class Animal : MonoBehaviour
{
    public Agent Agent { get; private set; }
    HexGrid hexGrid;

    public void Init(Agent agent, HexGrid grid)
    {
        Agent = agent;
        hexGrid = grid;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        HexTile tile = hexGrid.GetTileAtPos(Agent.Q, Agent.R);
        if (tile != null)
            transform.position = tile.transform.position + Vector3.up;
    }
}