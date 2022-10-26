using UnityEngine;

public class Predator : MonoBehaviour
{
    public float scarinessMod = 1f;

    [HideInInspector] public Vector2 position => transform.position;

    void OnEnable()
    {
        Simulation.RegisterPredator(this);
    }

    void OnDisable()
    {
        Simulation.DeregisterPredator(this);
    }
}
