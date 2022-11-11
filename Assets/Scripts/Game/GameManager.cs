using UnityEngine;

public class GameManager : MonoBehaviour
{
    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    private void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
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
