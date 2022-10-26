using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NEXT STEPS:
// - get separation working - find closest 3?
// - add obstacles

public class Boid : MonoBehaviour
{
    public enum BoidType
    {
        Pod,
        Xenon,
        Freighter,
        Noble,
        Pirate,
    }

    [SerializeField] bool debug = false;
    [SerializeField] BoidType type;
    [SerializeField] BoidStats stats;
    [SerializeField] float size = 0.5f;

    // cached
    Rigidbody2D rb;
    ScreenStats _screenStats;
    ScreenStats screenStats
    {
        get { if (_screenStats == null) _screenStats = Utils.GetScreenStats(); return _screenStats; }
        set { _screenStats = value; }
    }

    // state
    float rotationSpeed = 360f;

    Boid leader;
    Boid[] neighbors = new Boid[100];
    int neighborCount = 0;
    float neighborCountQuotient = 1f;

    Obstacle[] obstaclesNearby = new Obstacle[100];
    int obstaclesNearbyCount = 0;

    Predator[] predatorsNearby = new Predator[20];
    int predatorsNearbyCount = 0;

    Vector2 neighborCentroidHeading;

    Vector2 cohesion;
    Vector2 separation;
    Vector2 alignment;
    Vector2 followTheLeader;
    Vector2 avoidance;

    float closeness;
    float maxCloseness;

    Vector2 sightVector = Vector2.up;
    Vector2 desiredHeading = Vector2.up;
    Quaternion desiredRotation = Quaternion.identity;
    float steeringVarianceTime = Mathf.Infinity;

    static bool didInvalidateScreenStats;

    // public
    public BoidType Type => type;
    public bool IsAlive => enabled && gameObject.activeSelf && isActiveAndEnabled;
    public bool IsLeader => neighborCount > 0 && leader == null;
    public Vector2 velocity => rb == null ? Vector2.zero : rb.velocity;
    public Vector2 heading => rb == null ? Vector2.zero : rb.heading();
    public Vector2 position => transform.position;
    public Boid CurrentlyFollowing => leader != null ? leader : this;

    int _debug_simulation_boids;
    int _debug_simulation_obstacles;

    public bool IsBehind(Boid other)
    {
        if (other == null) return false;
        return Vector2.Dot(other.position - position, velocity) > 0f;
    }

    public float ForwardnessTo(Boid other) { return ForwardnessTo(other.transform); }
    public float ForwardnessTo(Obstacle other) { return ForwardnessTo(other.transform); }
    public float ForwardnessTo(Transform other)
    {
        if (other == null) return 0f;
        return Vector2.Dot((Vector2)other.position - position, velocity);
    }

    public Vector2 LineTo(Boid other) { return LineTo(other.transform); }
    public Vector2 LineTo(Obstacle other) { return LineTo(other.transform); }
    public Vector2 LineTo(Transform other)
    {
        if (other == null) return Vector2.zero;
        return (Vector2)other.position - position;
    }

    public Vector2 HeadingTo(Boid other) { return HeadingTo(other.transform); }
    public Vector2 HeadingTo(Obstacle other) { return HeadingTo(other.transform); }
    public Vector2 HeadingTo(Predator other) { return HeadingTo(other.transform); }
    public Vector2 HeadingTo(Transform other)
    {
        return LineTo(other).normalized;
    }

    public Vector2 LineFrom(Boid other) { return LineFrom(other.transform); }
    public Vector2 LineFrom(Obstacle other) { return LineFrom(other.transform); }
    public Vector2 LineFrom(Predator other) { return LineFrom(other.transform); }
    public Vector2 LineFrom(Transform other)
    {
        if (other == null) return Vector2.zero;
        return position - (Vector2)other.position;
    }

    public Vector2 HeadingFrom(Boid other) { return HeadingFrom(other.transform); }
    public Vector2 HeadingFrom(Obstacle other) { return HeadingFrom(other.transform); }
    public Vector2 HeadingFrom(Predator other) { return HeadingFrom(other.transform); }
    public Vector2 HeadingFrom(Transform other)
    {
        return LineFrom(other).normalized;
    }

    public float DistanceTo(Boid other) { return DistanceTo(other.transform); }
    public float DistanceTo(Obstacle other) { return DistanceTo(other.transform); }
    public float DistanceTo(Predator other) { return DistanceTo(other.transform); }
    public float DistanceTo(Transform other)
    {
        if (other == null) return Mathf.Infinity;
        return Vector2.Distance(position, other.position);
    }

    public bool CanSee(Boid other) { return CanSee(other.transform); }
    public bool CanSee(Obstacle other) { return CanSee(other.transform); }
    public bool CanSee(Predator other) { return CanSee(other.transform); }
    public bool CanSee(Transform other)
    {
        return Vector2.Distance(transform.position, other.position) <= stats.sightDistance;
    }

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
        // if (!didInitSimulation)
        // {
        //     didInitSimulation = true;
        //     Simulation.Init();
        // }
    }

    void Start()
    {
        if (rb == null) Debug.LogWarning($"You need to add a rigidbody2d to the Boid script for \"{gameObject.name}\"");
        StartCoroutine(InvalidateScreenStats());
        desiredHeading = transform.up;
    }

    void Update()
    {
        PerceiveEnvironment();
        Cohesion();
        Alignment();
        FollowTheLeader();
        Separation();
        Avoidance();
        RunFromPredators();
        desiredHeading = VaryHeading();
        desiredHeading = ReflectOffScreenEdges();
        ChangeHeading();
        Move();
        DebugSimulation();
    }

    void PerceiveEnvironment()
    {
        neighborCount = Simulation.GetNeighbors(this, neighbors);
        leader = Simulation.GetLeader(this, neighbors, neighborCount);
        neighborCountQuotient = neighborCount == 0 ? 1f : 1f / neighborCount;
        obstaclesNearbyCount = Simulation.GetObstaclesNearby(this, obstaclesNearby);
        predatorsNearbyCount = Simulation.GetPredatorsNearby(this, predatorsNearby);
        rotationSpeed = stats.rotationSpeed;
    }

    void Cohesion()
    {
        cohesion = Vector2.zero;
        if (stats.cohesion <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            cohesion += neighbors[i].position;
        }
        cohesion *= neighborCountQuotient;
        neighborCentroidHeading = (cohesion - position);
        cohesion = neighborCentroidHeading.normalized;
        desiredHeading += cohesion * stats.cohesion;
    }

    void Separation()
    {
        separation = Vector2.zero;
        closeness = 0f;
        if (stats.separation <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            closeness = Mathf.Clamp01((stats.sightDistance - DistanceTo(neighbors[i])) / stats.sightDistance);
            closeness = Mathf.Clamp01(stats.closenessMod.Evaluate(closeness));
            separation += HeadingFrom(neighbors[i]) * closeness;
        }
        desiredHeading += separation * stats.separation;
    }

    void Alignment()
    {
        alignment = Vector2.zero;
        if (stats.alignment <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            alignment += neighbors[i].velocity;
        }
        alignment *= neighborCountQuotient;
        alignment = alignment.normalized;
        desiredHeading += alignment * stats.alignment;
    }

    void FollowTheLeader()
    {
        followTheLeader = Vector2.zero;
        if (stats.followTheLeader <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        followTheLeader = leader.position - position;
        followTheLeader = followTheLeader.normalized;
        desiredHeading += followTheLeader * stats.followTheLeader;
    }

    void Avoidance()
    {
        avoidance = Vector2.zero;
        closeness = 0f;
        maxCloseness = 0f;
        if (stats.avoidance <= 0) return;
        if (obstaclesNearbyCount == 0) return;
        for (int i = 0; i < obstaclesNearbyCount; i++)
        {
            if (DistanceTo(obstaclesNearby[i]) >= obstaclesNearby[i].avoidanceMaxRadius) continue;
            // get closeness as a value from MIN to (MAX - MIN)
            closeness = obstaclesNearby[i].avoidanceMaxRadius - (DistanceTo(obstaclesNearby[i]) - obstaclesNearby[i].avoidanceMinRadius);
            // get closeness where 0 => MIN, 1 => MAX avoidance radius
            closeness = Mathf.Clamp01(closeness / (obstaclesNearby[i].avoidanceMaxRadius - obstaclesNearby[i].avoidanceMinRadius));
            closeness = Mathf.Pow(closeness, stats.avoidanceTension);
            if (closeness > maxCloseness) maxCloseness = closeness;
            closeness = closeness * stats.avoidanceStrength * obstaclesNearby[i].avoidanceMod;
            avoidance += HeadingFrom(obstaclesNearby[i]) * closeness;
        }
        rotationSpeed = stats.rotationSpeed * Mathf.Lerp(1f, stats.avoidanceRotationMod, maxCloseness);
        desiredHeading += avoidance * stats.avoidance * 2f;
    }

    void RunFromPredators()
    {
        avoidance = Vector2.zero;
        closeness = 0f;
        if (stats.runFromPredators <= 0) return;
        if (predatorsNearbyCount == 0) return;
        for (int i = 0; i < predatorsNearbyCount; i++)
        {
            closeness = stats.sightDistance - DistanceTo(predatorsNearby[i]);
            closeness = Mathf.Clamp01(closeness / stats.sightDistance);
            closeness = closeness * stats.predatorFleeMod.Evaluate(closeness) * predatorsNearby[i].scarinessMod;
            avoidance += HeadingFrom(predatorsNearby[i]) * closeness;
        }
        desiredHeading += avoidance * stats.avoidance * 4f;
    }

    void ChangeHeading()
    {
        desiredHeading = desiredHeading.normalized;
        desiredRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, desiredHeading));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    void Move()
    {
        if (rb == null) return;
        rb.velocity = transform.up * stats.moveSpeed;
    }

    Vector2 VaryHeading()
    {
        if (steeringVarianceTime < stats.steeringVarianceTimeStep)
        {
            steeringVarianceTime += Time.deltaTime;
            return desiredHeading;
        }
        steeringVarianceTime = 0f;
        return desiredHeading + UnityEngine.Random.insideUnitCircle * stats.steeringVariance;
    }

    const float REVERSE_TIME_QUOTIENT = 1 / 180f;
    const float CIRCUMFERENCE_TO_RADIUS_QUOTIENT = 1f / (2f * Mathf.PI);

    Vector2 ReflectOffScreenEdges()
    {
        if (position.y > screenStats.upperRightBounds.y) return desiredHeading.normalized * 0.2f + Vector2.down;
        if (position.y < screenStats.lowerLeftBounds.y) return desiredHeading.normalized * 0.2f + Vector2.up;
        if (position.x > screenStats.upperRightBounds.x) return desiredHeading.normalized * 0.2f + Vector2.left;
        if (position.x < screenStats.lowerLeftBounds.x) return desiredHeading.normalized * 0.2f + Vector2.right;

        sightVector = position + velocity * (CIRCUMFERENCE_TO_RADIUS_QUOTIENT * 2f) + velocity.normalized * size;

        if (sightVector.y > screenStats.upperRightBounds.y && Vector2.Dot(desiredHeading, Vector2.down) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.down) + Vector2.down).normalized;

        if (sightVector.y < screenStats.lowerLeftBounds.y && Vector2.Dot(desiredHeading, Vector2.up) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.up) + Vector2.up).normalized;

        if (sightVector.x > screenStats.upperRightBounds.x && Vector2.Dot(desiredHeading, Vector2.left) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.left) + Vector2.left).normalized;

        if (sightVector.x < screenStats.lowerLeftBounds.x && Vector2.Dot(desiredHeading, Vector2.right) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.right) + Vector2.right).normalized;

        return desiredHeading;
    }

    IEnumerator InvalidateScreenStats()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (!didInvalidateScreenStats)
        {
            didInvalidateScreenStats = true;
            Utils.InvalidateScreenStats();
        }
        screenStats = null;
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, size);
        Gizmos.color = Color.cyan;
        if (neighborCount > 0) Gizmos.color = Color.yellow;
        if (obstaclesNearbyCount > 0) Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.sightDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(desiredHeading * stats.sightDistance));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)(sightVector));
        if (leader != null)
        {
            Gizmos.color = Color.yellow.toAlpha(0.5f);
            Gizmos.DrawSphere(leader.transform.position, 0.5f);
        }
        if (screenStats != null)
        {
            Gizmos.color = Color.blue;
            Vector2 TL = new Vector2(screenStats.lowerLeftBounds.x, screenStats.upperRightBounds.y);
            Vector2 TR = new Vector2(screenStats.upperRightBounds.x, screenStats.upperRightBounds.y);
            Vector2 BL = new Vector2(screenStats.lowerLeftBounds.x, screenStats.lowerLeftBounds.y);
            Vector2 BR = new Vector2(screenStats.upperRightBounds.x, screenStats.lowerLeftBounds.y);
            Gizmos.DrawLine(TL, TR);
            Gizmos.DrawLine(TR, BR);
            Gizmos.DrawLine(BR, BL);
            Gizmos.DrawLine(BL, TL);
        }
        for (int i = 0; i < neighborCount; i++)
        {
            if (neighbors[i] == null || !neighbors[i].IsAlive) continue;
            if (leader != null && neighbors[i] == leader) continue;
            Gizmos.color = Color.blue.toAlpha(0.4f);
            Gizmos.DrawLine(transform.position, neighbors[i].position);
            Gizmos.DrawSphere(neighbors[i].position, 0.5f);
        }
        DrawStat(separation, Color.magenta.toAlpha(0.4f));
        DrawStat(alignment, Color.green.toAlpha(0.4f));
        DrawStat(cohesion, Color.cyan.toAlpha(0.3f));
        DrawStat(neighborCentroidHeading, Color.cyan.toAlpha(0.3f));
        DrawStat(avoidance, Color.red.toAlpha(0.5f));

        for (int i = 0; i < obstaclesNearbyCount; i++)
        {
            if (obstaclesNearby[i] == null || !obstaclesNearby[i].isActiveAndEnabled) continue;
            Gizmos.color = Color.red.toAlpha(0.4f);
            Gizmos.DrawLine(transform.position, obstaclesNearby[i].position);
            Gizmos.DrawSphere(obstaclesNearby[i].position, 0.5f);
        }
    }

    void DrawStat(Vector3 steeringMod, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(transform.position, transform.position + steeringMod);
    }

    void DebugSimulation()
    {
        _debug_simulation_boids = Simulation._debug_boids.Count;
        _debug_simulation_obstacles = Simulation._debug_obstacles.Count;
    }
}
