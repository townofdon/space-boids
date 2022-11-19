using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] bool useJobs;

    public GameState state;

    public bool UseJobs { get => useJobs; }

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

#if (!UNITY_EDITOR && UNITY_WEBGL)
        useJobs = false;
#endif

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
