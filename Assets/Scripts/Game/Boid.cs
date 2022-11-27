using System.Collections;
using UnityEngine;
using FMODUnity;

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

    [Space]
    [Space]

    [SerializeField] int lightLOD = 3;
    [SerializeField] int trailLOD = 3;
    [SerializeField] Material unlitMaterial;
    [SerializeField] SpriteRenderer shipSprite;
    [SerializeField] TrailRenderer trail;

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
    Vector2 cohesion;
    Vector2 separation;
    Vector2 alignment;
    Vector2 followTheLeader;
    Vector2 makeWayForLeader;
    Vector2 avoidObstacles;
    Vector2 avoidPredators;
    Vector2 avoidWalls;
    Vector2 seekFood;
    Food closestFood;
    float maxCloseness;
    float rotationSpeed = 360f;
    float rotationVariance = 0f;
    Vector2 sightVector = Vector2.up;
    Vector2 desiredHeading = Vector2.up;
    Quaternion desiredRotation = Quaternion.identity;
    float steeringVarianceTime = Mathf.Infinity;
    RaycastHit2D wallHit;
    Ray2D wallCheckRay = new Ray2D();
    float timeSinceLastWallHit = float.MaxValue;

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
    public void SetMaxCloseness(float value)
    {
        maxCloseness = value;
    }
    public void SetClosestFood(Food value)
    {
        closestFood = value;
    }

    // public
    public BoidType Type => type;
    public bool IsAlive => enabled && gameObject.activeSelf && isActiveAndEnabled;
    public Vector2 velocity => rb == null ? Vector2.zero : rb.velocity;
    public Vector2 heading => rb == null ? Vector2.zero : rb.heading();
    public Vector2 position => transform.position;

    public float StatSightDistance => stats.sightDistance;
    public float StatAwarenessLatency => stats.awarenessLatency;
    public float StatAwarenessVariance => stats.awarenessVariance;
    public float StatAvoidanceStrength => stats.avoidanceStrength;
    public float StatAvoidanceTension => stats.avoidanceTension;

    public bool IsDebugging => debug;

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
        if (eventType == GlobalEvent.type.DEGRADE_LOD) CheckLOD();
    }

    void CheckLOD()
    {
        if (lightLOD > Perf.LOD) TurnOffLights();
        if (trailLOD > Perf.LOD) TurnOffTrail();
    }

    void TurnOffLights()
    {
        shipSprite.material = unlitMaterial;
    }

    void TurnOffTrail()
    {
        trail.enabled = false;
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
        rotationVariance = UnityEngine.Random.Range(-stats.rotationVariance, stats.rotationVariance);
        StartCoroutine(LookForWalls());
        CheckLOD();
    }

    void Update()
    {
        desiredHeading += cohesion * CalcStat(stats.cohesion, gameManager.state.cohesion);
        desiredHeading += alignment * CalcStat(stats.alignment, gameManager.state.alignment);
        desiredHeading += seekFood * CalcStat(stats.seekFood, gameManager.state.seekFood, 2f);
        desiredHeading += followTheLeader * stats.followTheLeader;
        desiredHeading += separation * CalcStat(stats.separation, gameManager.state.separation);
        desiredHeading += avoidObstacles * 2f;
        desiredHeading += avoidPredators * CalcStat(stats.runFromPredators, gameManager.state.avoidPredators, 5f) * 4f;
        rotationSpeed = stats.rotationSpeed * Mathf.Lerp(1f, stats.avoidanceRotationMod, maxCloseness);
        desiredHeading = VaryHeading();
        if (stats.reflectOffScreenEdges) desiredHeading = ReflectOffScreenEdges();
        desiredHeading = AvoidWalls();
        ChangeHeading();
        Move();
        CheckFoodChomp();
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

    float CalcStat(float stat, float globalStat, float extraMod = 1f)
    {
        if (globalStat >= 1f)
        {
            return Mathf.Lerp(stat, 1f, globalStat - 1f) * Mathf.Lerp(1f, extraMod, globalStat - 1f);
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

    const float WALL_HIT_FALLOFF = 2f;

    Vector2 AvoidWalls()
    {
        timeSinceLastWallHit += Time.deltaTime * Simulation.speed;
        float falloff = Mathf.InverseLerp(0f, WALL_HIT_FALLOFF, timeSinceLastWallHit);
        return desiredHeading + Vector2.Lerp(avoidWalls, Vector2.zero, falloff);
    }

    void RaycastWalls()
    {
        wallCheckRay.origin = transform.position;
        wallCheckRay.direction = transform.up;
        wallHit = Physics2D.Raycast(wallCheckRay.origin, wallCheckRay.direction, stats.sightDistance, stats.wallLayer);
        if (wallHit && wallHit.collider != null)
        {
            float wallAvoidance = Mathf.Lerp(0.2f, 10f, Easing.InOutQuad(1f - Mathf.InverseLerp(0f, stats.sightDistance, wallHit.distance)));
            avoidWalls = Vector2.Reflect(velocity, wallHit.normal) * wallAvoidance;
            timeSinceLastWallHit = 0f;
        }
        else
        {
            avoidWalls = Vector2.zero;
        }
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
            if (IsOutsideAudioBounds())
            {
                emitter.Stop();
            }
            else if (!emitter.IsPlaying())
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0f, stats.audioDelay));
                emitter.Play();
            }
            yield return new WaitForSeconds(stats.audioLatency);
        }
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, stats.sightDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(desiredHeading * stats.sightDistance));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(wallCheckRay.origin, wallCheckRay.origin + wallCheckRay.direction * stats.sightDistance);
        DrawStat(separation, Color.magenta.toAlpha(0.4f));
        DrawStat(alignment, Color.green.toAlpha(0.4f));
        DrawStat(cohesion, Color.cyan.toAlpha(0.3f));
        DrawStat(avoidObstacles, Color.yellow.toAlpha(0.5f));
        DrawStat(avoidPredators, Color.red.toAlpha(0.5f));
    }

    void DrawStat(Vector3 steeringMod, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(transform.position, transform.position + steeringMod);
    }
}
