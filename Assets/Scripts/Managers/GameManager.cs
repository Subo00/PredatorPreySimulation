using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MouseState { GRASS = 0, BUNNY = 1, FOX = 2, NONE = 3 }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    public GameObject foxPrefab;
    public GameObject bunnyPrefab;

    [Header("Scene")]
    public HexGrid hexGrid;

    [Header("Brains (.zre renamed to .xml)")]
    public TextAsset preyBrainFile;
    public TextAsset predatorBrainFile;
    public int brainIterations = 2;            // <representationentry key="iterations"> in the .zre

    [Header("Simulation")]
    public SimParams simParams = new SimParams(); // must match the values the brains were trained with
    public float tickInterval = 0.2f;

    [HideInInspector] public MouseState mouseState = MouseState.NONE;

    Simulation sim;
    readonly Dictionary<Agent, Animal> animals = new Dictionary<Agent, Animal>();
    bool isRunning;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        simParams.GridRadius = hexGrid.gridRadius; // keep logic grid and visual grid identical

        var preyBrain = FrevoBrain.LoadBest(preyBrainFile.text, brainIterations);
        var predatorBrain = FrevoBrain.LoadBest(predatorBrainFile.text, brainIterations);
        sim = new Simulation(simParams, predatorBrain, preyBrain);
    }

    public void ChangeMouseState(int state) { mouseState = (MouseState)state; }

    // ── called by HexTile when clicked ───────────────────────────────────────

    public void OnTileClicked(HexTile tile)
    {
        if (isRunning) return; // no editing while the sim runs

        switch (mouseState)
        {
            case MouseState.GRASS: SetFood(tile, HexCell.FOOD_MAX); break;
            case MouseState.NONE: SetFood(tile, 0); break;
            case MouseState.BUNNY: Spawn(sim.PlaceAgent(Agent.Role.Prey, tile.Q, tile.R)); break;
            case MouseState.FOX: Spawn(sim.PlaceAgent(Agent.Role.Predator, tile.Q, tile.R)); break;
        }
    }

    void SetFood(HexTile tile, int amount)
    {
        sim.SetFood(tile.Q, tile.R, amount);
        tile.ShowFood(amount);
    }

    // ── Run button ───────────────────────────────────────────────────────────

    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Run()
    {
        if (!isRunning) StartCoroutine(RunLoop());
    }

    IEnumerator RunLoop()
    {
        isRunning = true;
        while (sim.IsPlaying)
        {
            sim.Tick();
            SyncVisuals();
            yield return new WaitForSeconds(tickInterval);
        }
        isRunning = false;
        Debug.Log($"Simulation ended at step {sim.Step}");
    }

    // ── keep the scene in sync with the simulation ───────────────────────────

    void SyncVisuals()
    {
        SyncAgents(sim.Prey);
        SyncAgents(sim.Predators);

        foreach (var cell in sim.Grid.GetAllCells())
            hexGrid.GetTileAtPos(cell.Q, cell.R)?.ShowFood(cell.Food);
    }

    void SyncAgents(List<Agent> agents)
    {
        foreach (var agent in agents)
        {
            if (!animals.ContainsKey(agent))
            {
                if (agent.IsAlive) Spawn(agent);    // newborn from reproduction
                continue;
            }

            var animal = animals[agent];
            if (animal == null) continue;           // already removed

            if (!agent.IsAlive)
            {
                Destroy(animal.gameObject);
                animals[agent] = null;
            }
            else animal.UpdateVisual();
        }
    }

    void Spawn(Agent agent)
    {
        if (agent == null) return; // tile was taken

        var prefab = agent.AgentRole == Agent.Role.Prey ? bunnyPrefab : foxPrefab;
        var animal = Instantiate(prefab, transform).GetComponent<Animal>();
        animal.Init(agent, hexGrid);
        animals[agent] = animal;
    }
}