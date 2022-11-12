using UnityEngine;

public class GameManager : MonoBehaviour
{

    public GameState state;

    public void SetCohesion(float val)
    {
        state.cohesion = val;
    }

    public void SetSeparation(float val)
    {
        state.separation = val;
    }

    public void SetAlignment(float val)
    {
        state.alignment = val;
    }

    public void SetAvoidPredators(float val)
    {
        state.avoidPredators = val;
    }

    public void SetSeekFood(float val)
    {
        state.seekFood = val;
    }

    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void Awake()
    {
        state = new GameState();
    }

    void Start()
    {
        Simulation.SetSimulationSpeed(0f);
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        // if (eventType == GlobalEvent.type.SIMULATION_START) Simulation.Start();
    }
}
