using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [SerializeField] bool debug = false;
    [SerializeField] BoidStats stats;

    // cached
    Rigidbody2D rb;
    ScreenStats screenStats;

    // state
    Boid[] neighbors = new Boid[100];
    int neighborCount = 0;
    Vector2 desiredHeading = Vector2.up;
    Quaternion desiredRotation = Quaternion.identity;
    float steeringVarianceTime = Mathf.Infinity;

    // public
    public bool IsAlive => enabled && gameObject.activeSelf;
    public Vector2 velocity => rb == null ? Vector2.zero : rb.velocity;

    void OnEnable()
    {
        Simulation.RegisterBoid(this);
    }

    void OnDisable()
    {
        Simulation.DeregisterBoid(this);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        screenStats = Utils.GetScreenStats();
    }

    void Start()
    {
        if (rb == null) Debug.LogWarning($"You need to add a rigidbody2d to the Boid script for \"{gameObject.name}\"");
    }

    void Update()
    {
        desiredHeading = ReflectOffScreenEdges();
        desiredHeading = VaryHeading();
        PerceiveEnvironment();
        ChangeHeading();
        Move();
    }

    void PerceiveEnvironment()
    {
        neighborCount = Simulation.GetNeighbors(this, neighbors, stats);
        if (debug) Debug.Log(neighborCount);
    }

    void ChangeHeading()
    {
        desiredRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, desiredHeading));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, stats.rotationSpeed * Time.deltaTime);
    }

    void Move()
    {
        if (rb == null) return;
        rb.velocity = transform.up * stats.moveSpeed;
    }

    Vector2 ReflectOffScreenEdges()
    {
        if (transform.position.y > screenStats.upperRightBounds.y && Vector2.Dot(desiredHeading, Vector2.down) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.down) + Vector2.down).normalized;

        if (transform.position.y < screenStats.lowerLeftBounds.y && Vector2.Dot(desiredHeading, Vector2.up) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.up) + Vector2.up).normalized;

        if (transform.position.x > screenStats.upperRightBounds.x && Vector2.Dot(desiredHeading, Vector2.left) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.left) + Vector2.left).normalized;

        if (transform.position.x < screenStats.lowerLeftBounds.x && Vector2.Dot(desiredHeading, Vector2.right) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.right) + Vector2.right).normalized;

        return desiredHeading;
    }

    Vector2 VaryHeading()
    {
        if (steeringVarianceTime < stats.steeringVarianceTimeStep)
        {
            steeringVarianceTime += Time.deltaTime;
            return desiredHeading;
        }
        steeringVarianceTime = 0f;
        return (desiredHeading + UnityEngine.Random.insideUnitCircle * stats.steeringVariance).normalized;
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.cyan;
        if (neighborCount > 0) Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.sightDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(desiredHeading * stats.sightDistance));
    }
}
