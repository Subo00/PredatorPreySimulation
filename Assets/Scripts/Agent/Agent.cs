using System.Collections.Generic;

// C# port of Agent.java. Plain class (no MonoBehaviour): simulation logic only.
// A separate MonoBehaviour will just read Q/R/Energy/IsAlive to draw the sprite.
public class Agent
{
    public enum Role { Predator, Prey }

    public const int STAY = -1;
    public const int STAY_AND_EAT = -2;

    // identity
    public Role AgentRole { get; }
    public int Id { get; }

    // position (axial)
    public int Q { get; private set; }
    public int R { get; private set; }

    // energy / alive
    public double Energy { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public double ReproductionAccumulator { get; private set; }

    // brain (replaces Controller / EvolvedController). Reset every step, so one brain per species can be shared.
    readonly FrevoBrain brain;

    // sensors (set by the simulation, read in Process)
    public List<float> SensorSelf { get; set; } = new List<float>();
    public List<float> SensorNearest { get; set; } = new List<float>(); // nearest opponent: dq, dr, dist
    public List<float> SensorNeighbor { get; set; } = new List<float>(); // nearest ally: dq, dr
    public List<float> SensorFood { get; set; } = new List<float>(); // prey only; 0 for predators

    // action (set in Process, consumed by the simulation)
    public int IntendedMove { get; set; } = STAY;

    public Agent(Role role, int id, int q, int r, double initialEnergy, FrevoBrain brain)
    {
        AgentRole = role;
        Id = id;
        Q = q;
        R = r;
        Energy = initialEnergy;
        this.brain = brain;
    }

    // energy / alive
    public void AddEnergy(double d) => Energy = System.Math.Max(0, Energy + d);
    public void Die() => IsAlive = false;
    public void CheckStarvation() { if (Energy <= 0) IsAlive = false; }

    public void AddReproductionEnergy(double d) => ReproductionAccumulator += d;
    public void ResetReproductionAccumulator() => ReproductionAccumulator = 0;

    // position
    public void MoveTo(int q, int r) { Q = q; R = r; }

    // Port of EvolvedController.process(): sensors -> brain -> IntendedMove
    public void Process()
    {
        brain.Reset(); // Java calls representation.reset() every step

        // Order from the old EvolvedController: self [3], nearest [3], neighbor [2], food [1] = 9 inputs
        var inputs = new List<float>();
        inputs.AddRange(SensorSelf);
        inputs.AddRange(SensorNearest);
        inputs.AddRange(SensorNeighbor);
        inputs.AddRange(SensorFood);

        float[] outputs = brain.Evaluate(inputs.ToArray());

        // Highest output above 0.1 wins; [0..5] = move, [6] = eat (prey only); else STAY
        int maxOutputs = AgentRole == Role.Prey ? 7 : 6;
        int bestAction = STAY;
        float bestVal = 0.1f;
        for (int i = 0; i < System.Math.Min(maxOutputs, outputs.Length); i++)
        {
            if (outputs[i] > bestVal)
            {
                bestVal = outputs[i];
                bestAction = (i == 6) ? STAY_AND_EAT : i;
            }
        }
        IntendedMove = bestAction;
    }

    public override string ToString() =>
        $"{AgentRole}#{Id} q={Q} r={R} energy={Energy:F1}" + (IsAlive ? "" : " [DEAD]");
}