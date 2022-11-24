using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public bool debug = false;
    public float avoidanceMod = 1f;
    public float avoidanceMinRadius = 0.5f;
    public float avoidanceMaxRadius = 2f;

    [HideInInspector] public Vector2 position => transform.position;

    float avoidanceOrientation = 1f;

    public float AvoidanceOrientation => avoidanceOrientation;

    void OnEnable()
    {
        avoidanceOrientation = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        Simulation.RegisterObstacle(this);
    }

    void OnDisable()
    {
        Simulation.DeregisterObstacle(this);
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.yellow.toAlpha(0.4f);
        Gizmos.DrawWireSphere(transform.position, avoidanceMinRadius);
        Gizmos.DrawWireSphere(transform.position, avoidanceMaxRadius);
    }
}
