using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public bool debug = false;
    public float avoidanceMod = 1f;
    public float avoidanceMinRadius = 0.5f;
    public float avoidanceMaxRadius = 2f;

    [HideInInspector] public Vector2 position => transform.position;

    void OnEnable()
    {
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
