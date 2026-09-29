using System;

// C# port of HexCell.java (logic only, no Unity). One cell of the simulation grid.
public class HexCell
{
    // food constants
    public const int    FOOD_MAX              = 100;
    public const int    FOOD_SPREAD_THRESHOLD = 80;
    public const double FOOD_GROWTH_RATE      = 0.08;

    public enum CellState { Empty, Prey, Predator }

    // Axial directions; index = Agent.IntendedMove
    public static readonly int[][] DIRECTIONS =
    {
        new[] { 1,  0},   // 0 – right
        new[] { 1, -1},   // 1 – upper-right
        new[] { 0, -1},   // 2 – upper-left
        new[] {-1,  0},   // 3 – left
        new[] {-1,  1},   // 4 – lower-left
        new[] { 0,  1},   // 5 – lower-right
    };

    public int Q { get; }
    public int R { get; }
    public CellState State { get; set; } = CellState.Empty;
    public int Food { get; private set; }

    int nextFood;

    public HexCell(int q, int r, int initialFood)
    {
        Q = q;
        R = r;
        Food = Math.Min(Math.Max(initialFood, 0), FOOD_MAX);
    }

    public void SetFood(int f) => Food = Math.Min(Math.Max(f, 0), FOOD_MAX);

    // Removes up to `amount` food, returns how much was actually removed
    public int ConsumeFood(int amount)
    {
        int consumed = Math.Min(Food, amount);
        Food -= consumed;
        return consumed;
    }

    // Logistic growth + spread from rich neighbours (double-buffered, see SimGrid.TickFoodGrowth)
    internal void ComputeNextFood(HexCell[] neighbours)
    {
        double logisticGain = FOOD_GROWTH_RATE * Food * (1.0 - (double)Food / FOOD_MAX);

        double spread = 0;
        foreach (var n in neighbours)
            if (n.Food >= FOOD_SPREAD_THRESHOLD)
                spread += (double)n.Food / FOOD_SPREAD_THRESHOLD;

        int raw = Food + (int)(logisticGain + spread);
        nextFood = Math.Min(Math.Max(raw, 0), FOOD_MAX);
    }

    internal void ApplyNextFood() => Food = nextFood;

    public int[][] NeighborCoords()
    {
        var result = new int[6][];
        for (int i = 0; i < 6; i++)
            result[i] = new[] { Q + DIRECTIONS[i][0], R + DIRECTIONS[i][1] };
        return result;
    }

    public override string ToString() => $"HexCell({Q}, {R}) [{State}, food={Food}]";
}
