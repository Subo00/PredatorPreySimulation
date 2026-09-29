using System;
using System.Collections.Generic;
using System.Linq;

// Port of SimulationParameters.java.
// IMPORTANT: use the SAME values the brains were trained with (see <problem> entries in the .zre).
[System.Serializable]
public class SimParams
{
    public int GridRadius = 8;    // visual + logic grid (copied from HexGrid.gridRadius)
    // Radius each brain was TRAINED with; used only to normalise its sensors
    public int PreySensorRadius = 8;   // evolvedPrey1.zre
    public int PredatorSensorRadius = 7;   // evolvedPredator1.zre
    public bool HorizontalWraparound = false; // old (9-input) version had hard walls
    public int MaximumSteps = 500;
    public double AgentInitEnergy = 100.0;
    public double EnergyDecayPredator = 2.0;
    public double EnergyDecayPrey = 1.0;
    public double EnergyGainCatch = 40.0;
    public int FoodEatAmount = 30;
    public double FoodEnergyGain = 15.0;
}

// Port of SimulationServer.java + the relevant parts of SimulationState.java.
// No FREVO, no fitness: the user places grass/agents, then calls Tick() repeatedly.
public class Simulation
{
    const double PRED_REPRO_THRESHOLD = 80.0;
    const double PREY_REPRO_THRESHOLD = 30.0;
    const double REPRO_ENERGY_RATE = 0.08;
    const int MAX_PREDATORS = 20;
    const int MAX_PREY = 60;

    public readonly SimParams P;
    public readonly SimGrid Grid;
    public readonly List<Agent> Predators = new List<Agent>();
    public readonly List<Agent> Prey = new List<Agent>();
    public int Step { get; private set; }

    readonly FrevoBrain predatorBrain, preyBrain;
    readonly Random rng;

    public Simulation(SimParams p, FrevoBrain predatorBrain, FrevoBrain preyBrain, int seed = 0)
    {
        P = p;
        this.predatorBrain = predatorBrain;
        this.preyBrain = preyBrain;
        rng = new Random(seed);
        Grid = new SimGrid(p.GridRadius, 0); // start with no food; user places grass
    }

    // ── placement (replaces placeAgents / seedFoodPatches) ───────────────────

    // Returns the new agent, or null if the tile is taken / outside the grid
    public Agent PlaceAgent(Agent.Role role, int q, int r)
    {
        var cell = Grid.GetCell(q, r);
        if (cell == null || cell.State != HexCell.CellState.Empty) return null;

        var list = role == Agent.Role.Predator ? Predators : Prey;
        var brain = role == Agent.Role.Predator ? predatorBrain : preyBrain;
        var agent = new Agent(role, list.Count, q, r, P.AgentInitEnergy, brain);
        list.Add(agent);
        cell.State = role == Agent.Role.Predator ? HexCell.CellState.Predator : HexCell.CellState.Prey;
        return agent;
    }

    public void SetFood(int q, int r, int amount) => Grid.GetCell(q, r)?.SetFood(amount);

    // ── main loop (one iteration of runSimulation's while loop) ──────────────

    public bool IsPlaying =>
        Step < P.MaximumSteps && Prey.Any(a => a.IsAlive) && Predators.Any(a => a.IsAlive);

    public void Tick()
    {
        // 1. sensors
        foreach (var a in Predators) if (a.IsAlive) CalculateSensors(a);
        foreach (var a in Prey) if (a.IsAlive) CalculateSensors(a);

        // 2. decide
        foreach (var a in Predators) if (a.IsAlive) a.Process();
        foreach (var a in Prey) if (a.IsAlive) a.Process();

        // 3. eat, then move
        ResolveHerbivoreEating();
        ApplyMovement(Predators);
        ApplyMovement(Prey);

        // 4. energy decay
        ApplyEnergyDecay();

        // 6. catches
        ResolveCatches();

        // 7. reproduction
        ResolveReproduction();

        // 8. grass growth
        Grid.TickFoodGrowth();

        Step++;
    }

    // ── sensors (old 9-input version) ────────────────────────────────────────
    // [0-2] self (q, r, energy)  [3-5] nearest opponent (dq, dr, dist)
    // [6-7] nearest ally (dq, dr)  [8] food on current cell

    void CalculateSensors(Agent a)
    {
        int radius = a.AgentRole == Agent.Role.Prey ? P.PreySensorRadius : P.PredatorSensorRadius;
        float diam = radius * 2;

        // self
        a.SensorSelf = new List<float>
        {
            (a.Q + radius) / diam,
            (a.R + radius) / diam,
            (float)(a.Energy / P.AgentInitEnergy)
        };

        // nearest opponent
        var opponents = a.AgentRole == Agent.Role.Predator ? Prey : Predators;
        var opp = FindNearest(a, opponents, null);
        a.SensorNearest = opp == null
            ? new List<float> { 0f, 0f, 1f }   // none left: "far away"
            : new List<float> { (opp.Q - a.Q) / diam, (opp.R - a.R) / diam, AxialDistance(a, opp) / diam };

        // nearest ally (excluding self)
        var allies = a.AgentRole == Agent.Role.Predator ? Predators : Prey;
        var ally = FindNearest(a, allies, a);
        a.SensorNeighbor = ally == null
            ? new List<float> { 0f, 0f }
            : new List<float> { (ally.Q - a.Q) / diam, (ally.R - a.R) / diam };

        // food (prey only)
        float food = 0f;
        if (a.AgentRole == Agent.Role.Prey)
        {
            var cell = Grid.GetCell(a.Q, a.R);
            if (cell != null) food = (float)cell.Food / HexCell.FOOD_MAX;
        }
        a.SensorFood = new List<float> { food };
    }

    Agent FindNearest(Agent self, List<Agent> pool, Agent exclude)
    {
        Agent best = null;
        int bestDist = int.MaxValue;
        foreach (var o in pool)
        {
            if (!o.IsAlive || o == exclude) continue;
            int d = AxialDistance(self, o);
            if (d < bestDist) { bestDist = d; best = o; }
        }
        return best;
    }

    // ── eating ───────────────────────────────────────────────────────────────

    void ResolveHerbivoreEating()
    {
        foreach (var prey in Prey)
        {
            if (!prey.IsAlive || prey.IntendedMove != Agent.STAY_AND_EAT) continue;
            var cell = Grid.GetCell(prey.Q, prey.R);
            if (cell == null) continue;

            int consumed = cell.ConsumeFood(P.FoodEatAmount);
            prey.AddEnergy(consumed * P.FoodEnergyGain);
            prey.IntendedMove = Agent.STAY;
        }
    }

    // ── movement ─────────────────────────────────────────────────────────────

    void ApplyMovement(List<Agent> agents)
    {
        foreach (var a in agents)
        {
            if (!a.IsAlive) continue;
            int dir = a.IntendedMove;
            if (dir < 0 || dir > 5) continue;

            int[] d = HexCell.DIRECTIONS[dir];
            int nq = a.Q + d[0];
            int nr = a.R + d[1];

            // horizontal wraparound for pure left/right moves
            if (P.HorizontalWraparound && d[1] == 0 && !Grid.InBounds(nq, nr))
            {
                int[] w = Grid.WrapHorizontal(nq, nr);
                nq = w[0]; nr = w[1];
            }

            if (Grid.InBounds(nq, nr) && Grid.GetCell(nq, nr).State == HexCell.CellState.Empty)
            {
                Grid.GetCell(a.Q, a.R).State = HexCell.CellState.Empty;
                a.MoveTo(nq, nr);
                Grid.GetCell(nq, nr).State = a.AgentRole == Agent.Role.Predator
                    ? HexCell.CellState.Predator : HexCell.CellState.Prey;
            }
        }
    }

    // ── energy decay ─────────────────────────────────────────────────────────

    void ApplyEnergyDecay()
    {
        Decay(Predators, P.EnergyDecayPredator);
        Decay(Prey, P.EnergyDecayPrey);
    }

    void Decay(List<Agent> agents, double amount)
    {
        foreach (var a in agents)
        {
            if (!a.IsAlive) continue;
            a.AddEnergy(-amount);
            if (a.Energy <= 0)
            {
                a.Die();
                Grid.GetCell(a.Q, a.R).State = HexCell.CellState.Empty;
            }
        }
    }

    // ── catches ──────────────────────────────────────────────────────────────

    void ResolveCatches()
    {
        foreach (var pred in Predators)
        {
            if (!pred.IsAlive) continue;
            foreach (var p in Prey)
            {
                if (!p.IsAlive) continue;
                if (AxialDistance(pred, p) <= 1)
                {
                    p.Die();
                    Grid.GetCell(p.Q, p.R).State = HexCell.CellState.Empty;
                    pred.AddEnergy(P.EnergyGainCatch);
                    break; // one catch per predator per step
                }
            }
        }
    }

    // ── reproduction ─────────────────────────────────────────────────────────

    void ResolveReproduction()
    {
        var newPred = new List<Agent>();
        var newPrey = new List<Agent>();

        foreach (var a in Predators)
        {
            if (!a.IsAlive) continue;
            if (Predators.Count + newPred.Count >= MAX_PREDATORS) break;
            if (Accumulate(a, PRED_REPRO_THRESHOLD))
            {
                var child = SpawnOffspring(a, Agent.Role.Predator, Predators.Count + newPred.Count);
                if (child != null) newPred.Add(child);
            }
        }

        foreach (var a in Prey)
        {
            if (!a.IsAlive) continue;
            if (Prey.Count + newPrey.Count >= MAX_PREY) break;
            if (Accumulate(a, PREY_REPRO_THRESHOLD))
            {
                var child = SpawnOffspring(a, Agent.Role.Prey, Prey.Count + newPrey.Count);
                if (child != null) newPrey.Add(child);
            }
        }

        Predators.AddRange(newPred);
        Prey.AddRange(newPrey);
    }

    bool Accumulate(Agent a, double threshold)
    {
        a.AddReproductionEnergy((a.Energy / P.AgentInitEnergy) * REPRO_ENERGY_RATE * threshold);
        if (a.ReproductionAccumulator >= threshold)
        {
            a.ResetReproductionAccumulator();
            return true;
        }
        return false;
    }

    Agent SpawnOffspring(Agent parent, Agent.Role role, int id)
    {
        var parentCell = Grid.GetCell(parent.Q, parent.R);
        if (parentCell == null) return null;

        var empty = Grid.GetNeighbors(parentCell)
                        .Where(n => n.State == HexCell.CellState.Empty).ToList();
        if (empty.Count == 0) return null;

        var bc = empty[rng.Next(empty.Count)];
        var brain = role == Agent.Role.Predator ? predatorBrain : preyBrain;
        var child = new Agent(role, id, bc.Q, bc.R, parent.Energy * 0.5, brain);
        bc.State = role == Agent.Role.Predator ? HexCell.CellState.Predator : HexCell.CellState.Prey;
        return child;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static int AxialDistance(Agent a, Agent b) =>
        (Math.Abs(a.Q - b.Q) + Math.Abs(a.Q + a.R - b.Q - b.R) + Math.Abs(a.R - b.R)) / 2;
}