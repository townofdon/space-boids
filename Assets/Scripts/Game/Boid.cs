using System.Collections;
using UnityEngine;
using FMODUnity;

// NOTES FOR FUTURE DON
// I think the solution here is to jobify the individual Boid scripts.
// This way each boid can have distinct Neighbors {NativeArray<bool>}.
// Each CheckFnc will simply ignore `Neighbors[other] == false`.
// 
// ACKSTCHUALLY, making BoidManager not fire off one huge single-threaded job
// will also help tremendounsly. E.g. separate out coherence, separation, etc.
// This will also allow us to find all the neighbors in a first pass and
// insert them into a two-dimensional array.
// Use a 1D array and index it as array[y * width + x]? That's all that the 2D array is doing for you under the hood anyway.


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

    public Food.FoodType GetFoodType()
    {
        switch (type)
        {
            case BoidType.Pod:
                return Food.FoodType.Pod;
            case BoidType.Xenon:
                return Food.FoodType.Xenon;
            case BoidType.Freighter:
                return Food.FoodType.Freighter;
            default:
                return Food.FoodType.Pod;
        }
    }

    // cached
    Rigidbody2D rb;
    CircleCollider2D circle;
    StudioEventEmitter emitter;
    GameManager gameManager;
    Player player;

    // state
    float rotationSpeed = 360f;
    float awarenessLatency = 0.2f;
    float timeSinceLastPerceived = float.MaxValue;

    Boid leader;
    Boid[] neighbors = new Boid[100];
    int neighborCount = 0;
    float neighborCountQuotient = 1f;

    Obstacle[] obstaclesNearby = new Obstacle[100];
    int obstaclesNearbyCount = 0;

    Predator[] predatorsNearby = new Predator[20];
    int predatorsNearbyCount = 0;

    Food[] foods = new Food[20];
    Food closestFood;
    int foodsCount = 0;

    Vector2 neighborCentroidHeading;

    Vector2 cohesion;
    Vector2 separation;
    Vector2 alignment;
    Vector2 followTheLeader;
    Vector2 makeWayForLeader;
    Vector2 avoidObstacles;
    Vector2 avoidPredators;
    Vector2 seekFood;

    public void SetCohesion(Vector2 value)
    {
        cohesion = value;
    }
    public void SetSeparation(Vector2 value)
    {
        separation = value;
    }
    public void SetAlignment(Vector2 value)
    {
        alignment = value;
    }
    public void SetFollowTheLeader(Vector2 value)
    {
        followTheLeader = value;
    }
    public void SetAvoidObstacles(Vector2 value)
    {
        avoidObstacles = value;
    }
    public void SetAvoidPredators(Vector2 value)
    {
        avoidPredators = value;
    }
    public void SetSeekFood(Vector2 value)
    {
        seekFood = value;
    }

    public float SightDistance => stats.sightDistance;

    float forwardness;
    float closeness;
    float maxCloseness;
    float rotationVariance = 0f;

    Vector2 sightVector = Vector2.up;
    Vector2 desiredHeading = Vector2.up;
    Quaternion desiredRotation = Quaternion.identity;
    float steeringVarianceTime = Mathf.Infinity;

    RaycastHit2D wallHit;
    Ray2D wallCheckRay = new Ray2D();

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

    public bool IsOtherAlive(Boid other)
    {
        if (other == null) return false;
        return other.IsAlive;
    }

    public float ForwardnessTo(Boid other) { return ForwardnessTo(other.transform); }
    public float ForwardnessTo(Obstacle other) { return ForwardnessTo(other.transform); }
    public float ForwardnessTo(Transform other)
    {
        if (other == null) return 0f;
        return Vector2.Dot((Vector2)other.position - position, velocity);
    }
    public float ForwardnessTo(Vector2 other)
    {
        if (other == null) return 0f;
        return Vector2.Dot(other - position, velocity);
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
    public float DistanceTo(Vector2 other)
    {
        return Vector2.Distance(position, other);
    }

    public bool CanSee(Boid other) { return CanSee(other.transform); }
    public bool CanSee(Obstacle other) { return CanSee(other.transform); }
    public bool CanSee(Predator other) { return CanSee(other.transform); }
    public bool CanSee(Transform other)
    {
        if (other == null) return false;
        if (Mathf.Abs(other.position.x - position.x) > stats.sightDistance) return false;
        if (Mathf.Abs(other.position.y - position.y) > stats.sightDistance) return false;
        if (ForwardnessTo(other) < -0.75) return false;
        return Vector2.Distance(transform.position, other.position) <= stats.sightDistance;
    }

    void OnEnable()
    {
        Simulation.RegisterBoid(this);
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        Simulation.DeregisterBoid(this);
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        if (eventType == GlobalEvent.type.SIMULATION_START) StartCoroutine(PlayOrStopAudio());
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        emitter = GetComponent<StudioEventEmitter>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    void Start()
    {
        if (rb == null) Debug.LogWarning($"You need to add a rigidbody2d to the Boid script for \"{gameObject.name}\"");
        desiredHeading = transform.up;
        awarenessLatency = stats.awarenessLatency + UnityEngine.Random.Range(0f, stats.awarenessVariance);
        timeSinceLastPerceived = UnityEngine.Random.Range(0f, awarenessLatency);
        rotationVariance = UnityEngine.Random.Range(-stats.rotationVariance, stats.rotationVariance);
        StartCoroutine(LookForWalls());
    }

    void Profile(System.Action action, string actionName)
    {
        float time = Time.realtimeSinceStartup;
        action();
        if (debug) Debug.Log(actionName.PadLeft(30, '_') + " >> " + ((Time.realtimeSinceStartup - time) * 1000f) + "ms");
    }

    void Update()
    {
        if (gameManager.UseJobs)
        {
            desiredHeading += cohesion * CalcStat(stats.cohesion, gameManager.state.cohesion);
            desiredHeading += alignment * CalcStat(stats.alignment, gameManager.state.alignment);
            desiredHeading += seekFood * CalcStat(stats.seekFood, gameManager.state.seekFood);
            desiredHeading += followTheLeader * stats.followTheLeader;
            desiredHeading += separation * CalcStat(stats.separation, gameManager.state.separation);
            desiredHeading += avoidObstacles * 2f;
            desiredHeading += avoidPredators * CalcStat(stats.runFromPredators, gameManager.state.avoidPredators) * 4f;
            desiredHeading = VaryHeading();
            desiredHeading = AvoidWalls();
            ChangeHeading();
            Move();
        }
        else
        {
            Awareness();
            PerceiveEnvironment();
            Cohesion();
            Alignment();
            SeekFood();
            FollowTheLeader();
            Separation();
            Avoidance();
            RunFromPredators();
            desiredHeading = VaryHeading();
            if (stats.reflectOffScreenEdges) desiredHeading = ReflectOffScreenEdges();
            desiredHeading = AvoidWalls();
            ChangeHeading();
            Move();
            CheckFoodChomp();
            DebugSimulation();
            PostAwareness();
        }
    }

    void Awareness()
    {
        if (timeSinceLastPerceived < awarenessLatency)
        {
            awarenessLatency = stats.awarenessLatency + UnityEngine.Random.Range(0f, stats.awarenessVariance);
            timeSinceLastPerceived += Time.deltaTime * Simulation.speed;
        }
    }

    void PostAwareness()
    {
        if (timeSinceLastPerceived >= awarenessLatency) timeSinceLastPerceived = 0f;
    }

    void PerceiveEnvironment()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        neighborCount = Simulation.GetNeighbors(this, neighbors);
        leader = Simulation.GetLeader(this, neighbors, neighborCount);
        neighborCountQuotient = neighborCount == 0 ? 1f : 1f / neighborCount;
        obstaclesNearbyCount = Simulation.GetObstaclesNearby(this, obstaclesNearby);
        predatorsNearbyCount = Simulation.GetPredatorsNearby(this, predatorsNearby);
        foodsCount = Simulation.GetFoods(this, foods);
        closestFood = Simulation.GetClosestFood(this, foods, foodsCount);
    }

    void Cohesion()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        cohesion = Vector2.zero;
        if (CalcStat(stats.cohesion, gameManager.state.cohesion) <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            if (!IsOtherAlive(neighbors[i])) continue;
            cohesion += neighbors[i].position;
        }
        cohesion *= neighborCountQuotient;
        neighborCentroidHeading = (cohesion - position);
        cohesion = neighborCentroidHeading.normalized;
        desiredHeading += cohesion * CalcStat(stats.cohesion, gameManager.state.cohesion);
    }

    void Separation()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        separation = Vector2.zero;
        closeness = 0f;
        if (CalcStat(stats.separation, gameManager.state.separation) <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            if (!IsOtherAlive(neighbors[i])) continue;
            closeness = Mathf.Clamp01((stats.sightDistance - DistanceTo(neighbors[i])) / stats.sightDistance);
            closeness = Mathf.Clamp01(stats.closenessMod.Evaluate(closeness));
            separation += HeadingFrom(neighbors[i]) * closeness;
        }
        desiredHeading += separation * CalcStat(stats.separation, gameManager.state.separation);
    }

    void Alignment()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        alignment = Vector2.zero;
        forwardness = 0f;
        if (CalcStat(stats.alignment, gameManager.state.alignment) <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        for (int i = 0; i < neighborCount; i++)
        {
            if (!IsOtherAlive(neighbors[i])) continue;
            forwardness = ForwardnessTo(neighbors[i]);
            alignment += neighbors[i].velocity * forwardness;
        }
        alignment *= neighborCountQuotient;
        alignment = alignment.normalized;
        desiredHeading += alignment * CalcStat(stats.alignment, gameManager.state.alignment);
    }

    void FollowTheLeader()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        // I'm not convinced this is working as expected...
        followTheLeader = Vector2.zero;
        if (stats.followTheLeader <= 0) return;
        if (neighborCount == 0) return;
        if (leader == null) return;
        followTheLeader = leader.position - leader.velocity - position;
        followTheLeader = followTheLeader.normalized;
        desiredHeading += followTheLeader * stats.followTheLeader;
    }

    void Avoidance()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        avoidObstacles = Vector2.zero;
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
            closeness = closeness * stats.avoidanceStrength * obstaclesNearby[i].avoidanceMod;
            avoidObstacles += HeadingFrom(obstaclesNearby[i]) * closeness;
        }
        rotationSpeed = stats.rotationSpeed * Mathf.Lerp(1f, stats.avoidanceRotationMod, maxCloseness);
        desiredHeading += avoidObstacles * 2f;
        // if (timeSinceLastPerceived < awarenessLatency) return;
        // avoidObstacles = Vector2.zero;
        // closeness = 0f;
        // forwardness = 0f;
        // maxCloseness = 0f;
        // Vector2 projectedPoint;
        // if (stats.avoidance <= 0) return;
        // if (obstaclesNearbyCount == 0) return;
        // for (int i = 0; i < obstaclesNearbyCount; i++)
        // {
        //     projectedPoint = obstaclesNearby[i].position - (Vector2)transform.up * obstaclesNearby[i].avoidanceMinRadius;
        //     forwardness = ForwardnessTo(obstaclesNearby[i]);
        //     if (forwardness <= .25f) continue;
        //     if (DistanceTo(obstaclesNearby[i]) >= obstaclesNearby[i].avoidanceMaxRadius) continue;
        //     forwardness = Mathf.Clamp01(Mathf.Clamp01(stats.forwardnessMod.Evaluate((forwardness - 0.66f) * 3.33f)));
        //     // get closeness as a value from MIN to (MAX - MIN)
        //     closeness = obstaclesNearby[i].avoidanceMaxRadius - (DistanceTo(obstaclesNearby[i]) - obstaclesNearby[i].avoidanceMinRadius);
        //     // get closeness where 0 => MIN, 1 => MAX avoidance radius
        //     closeness = Mathf.Clamp01(closeness / (obstaclesNearby[i].avoidanceMaxRadius - obstaclesNearby[i].avoidanceMinRadius));
        //     if (closeness > maxCloseness) maxCloseness = closeness;
        //     avoidObstacles += (projectedPoint + (Vector2)transform.right * obstaclesNearby[i].AvoidanceOrientation * velocity.magnitude) * (1f - closeness) * forwardness;
        //     closeness = Mathf.Pow(closeness, stats.avoidanceTension);
        //     closeness = closeness * stats.avoidanceStrength * obstaclesNearby[i].avoidanceMod;
        //     avoidObstacles += HeadingFrom(obstaclesNearby[i]) * closeness + projectedPoint + (Vector2)transform.right;
        // }
        // rotationSpeed = stats.rotationSpeed * Mathf.Lerp(1f, stats.avoidanceRotationMod, maxCloseness) + rotationVariance;
        // desiredHeading += avoidObstacles * 2f;
    }

    void SeekFood()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        seekFood = Vector2.zero;
        if (CalcStat(stats.seekFood, gameManager.state.seekFood) <= 0) return;
        if (foodsCount == 0) return;
        if (closestFood == null) return;
        seekFood = (closestFood.transform.position - transform.position).normalized;
        desiredHeading += seekFood * CalcStat(stats.seekFood, gameManager.state.seekFood);
    }

    void RunFromPredators()
    {
        if (timeSinceLastPerceived < awarenessLatency) return;
        avoidPredators = Vector2.zero;
        closeness = 0f;
        if (CalcStat(stats.runFromPredators, gameManager.state.avoidPredators) <= 0) return;
        if (predatorsNearbyCount == 0) return;
        for (int i = 0; i < predatorsNearbyCount; i++)
        {
            closeness = stats.sightDistance - DistanceTo(predatorsNearby[i]);
            closeness = Mathf.Clamp01(closeness / stats.sightDistance);
            closeness = closeness * stats.predatorFleeMod.Evaluate(closeness) * predatorsNearby[i].scarinessMod;
            avoidPredators += HeadingFrom(predatorsNearby[i]) * closeness;
        }
        desiredHeading += avoidPredators * CalcStat(stats.runFromPredators, gameManager.state.avoidPredators) * 4f;
    }

    void ChangeHeading()
    {
        desiredHeading = float.IsNaN(desiredHeading.x) ? Vector2.down : desiredHeading.normalized;
        desiredRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, desiredHeading));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Simulation.speed * Time.deltaTime);
    }

    void Move()
    {
        if (rb == null) return;
        rb.velocity = transform.up * stats.moveSpeed * Simulation.speed;
    }

    void CheckFoodChomp()
    {
        if (closestFood == null) return;
        float foodChompDistance = .4f;
        if (Mathf.Abs(closestFood.transform.position.x - position.x) > foodChompDistance) return;
        if (Mathf.Abs(closestFood.transform.position.y - position.y) > foodChompDistance) return;
        if (Vector2.Distance(closestFood.transform.position, transform.position) <= foodChompDistance) closestFood.GetChomped();
    }

    float CalcStat(float stat, float globalStat)
    {
        if (globalStat >= 1f)
        {
            return Mathf.Lerp(stat, 1f, globalStat - 1f);
        }
        return stat * globalStat;
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
        if (position.y > Utils.GetScreenStats().upperRightBounds.y) return desiredHeading.normalized * 0.2f + Vector2.down;
        if (position.y < Utils.GetScreenStats().lowerLeftBounds.y) return desiredHeading.normalized * 0.2f + Vector2.up;
        if (position.x > Utils.GetScreenStats().upperRightBounds.x) return desiredHeading.normalized * 0.2f + Vector2.left;
        if (position.x < Utils.GetScreenStats().lowerLeftBounds.x) return desiredHeading.normalized * 0.2f + Vector2.right;

        sightVector = position + velocity * (CIRCUMFERENCE_TO_RADIUS_QUOTIENT * 2f) + velocity.normalized * size;

        if (sightVector.y > Utils.GetScreenStats().upperRightBounds.y && Vector2.Dot(desiredHeading, Vector2.down) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.down) + Vector2.down).normalized;

        if (sightVector.y < Utils.GetScreenStats().lowerLeftBounds.y && Vector2.Dot(desiredHeading, Vector2.up) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.up) + Vector2.up).normalized;

        if (sightVector.x > Utils.GetScreenStats().upperRightBounds.x && Vector2.Dot(desiredHeading, Vector2.left) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.left) + Vector2.left).normalized;

        if (sightVector.x < Utils.GetScreenStats().lowerLeftBounds.x && Vector2.Dot(desiredHeading, Vector2.right) <= 0f)
            return (Vector2.Reflect(desiredHeading, Vector2.right) + Vector2.right).normalized;

        return desiredHeading;
    }

    Vector2 AvoidWalls()
    {
        if (!wallHit) return desiredHeading;
        if (timeSinceLastPerceived == 0 || timeSinceLastPerceived >= awarenessLatency) return desiredHeading;
        if (wallHit.collider == null) return desiredHeading;
        float wallAvoidance = stats.wallAvoidanceMod.Evaluate((stats.sightDistance - wallHit.distance) / stats.sightDistance);
        rotationSpeed = stats.rotationSpeed * Mathf.Lerp(1f, stats.avoidanceRotationMod, wallAvoidance) + rotationVariance;
        return Vector2.Lerp(desiredHeading, wallHit.normal, wallAvoidance);
    }

    void RaycastWalls()
    {
        wallCheckRay.origin = transform.position;
        wallCheckRay.direction = transform.up;
        wallHit = Physics2D.Raycast(wallCheckRay.origin, wallCheckRay.direction, stats.sightDistance, stats.wallLayer);
    }

    IEnumerator LookForWalls()
    {
        while (true)
        {
            yield return new WaitForSeconds(stats.wallRaycastLatency + UnityEngine.Random.Range(0f, stats.wallRaycastVariance));
            RaycastWalls();
        }
    }

    bool IsOutsideAudioBounds()
    {
        if (Mathf.Abs(position.x - player.transform.position.x) > Utils.GetScreenStats().screenWidth * stats.audioScreenCutoff) return true;
        if (Mathf.Abs(position.y - player.transform.position.y) > Utils.GetScreenStats().screenHeight * stats.audioScreenCutoff) return true;
        return false;
    }

    IEnumerator PlayOrStopAudio()
    {
        while (true)
        {
            if (IsOutsideAudioBounds() || UnityEngine.Random.Range(0f, 1f) > 0.95f)
            {
                emitter.Stop();
            }
            else if (!emitter.IsPlaying())
            {
                emitter.Play();
            }
            yield return new WaitForSeconds(stats.audioLatency);
        }
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
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(wallCheckRay.origin, wallCheckRay.origin + wallCheckRay.direction * stats.sightDistance);
        if (leader != null)
        {
            Gizmos.color = Color.yellow.toAlpha(0.5f);
            Gizmos.DrawSphere(leader.transform.position, 0.5f);
        }
        if (Utils.GetScreenStats() != null)
        {
            Gizmos.color = Color.blue;
            Vector2 TL = new Vector2(Utils.GetScreenStats().lowerLeftBounds.x, Utils.GetScreenStats().upperRightBounds.y);
            Vector2 TR = new Vector2(Utils.GetScreenStats().upperRightBounds.x, Utils.GetScreenStats().upperRightBounds.y);
            Vector2 BL = new Vector2(Utils.GetScreenStats().lowerLeftBounds.x, Utils.GetScreenStats().lowerLeftBounds.y);
            Vector2 BR = new Vector2(Utils.GetScreenStats().upperRightBounds.x, Utils.GetScreenStats().lowerLeftBounds.y);
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
        DrawStat(avoidObstacles, Color.red.toAlpha(0.5f));

        for (int i = 0; i < obstaclesNearbyCount; i++)
        {
            if (obstaclesNearby[i] == null || !obstaclesNearby[i].isActiveAndEnabled) continue;
            Gizmos.color = Color.red.toAlpha(0.4f);
            // Gizmos.DrawLine(transform.position, obstaclesNearby[i].position);
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
        if (!debug) return;
        _debug_simulation_boids = Simulation._debug_boids.Count;
        _debug_simulation_obstacles = Simulation._debug_obstacles.Count;
    }
}
