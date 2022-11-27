using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Obstacle : MonoBehaviour
{
    public bool debug = false;
    public float avoidanceMod = 1f;
    public float avoidanceMinRadius = 0.5f;
    public float avoidanceMaxRadius = 2f;

    [Space]
    [Space]

    [Range(0, 10)] public int shadowLOD = 5;
    [Range(0, 10)] public int physicsLOD = 5;

    [HideInInspector] public Vector2 position => transform.position;

    float avoidanceOrientation = 1f;

    public float AvoidanceOrientation => avoidanceOrientation;

    ShadowCaster2D shadowCaster;
    Rigidbody2D rigidbody;

    void OnEnable()
    {
        avoidanceOrientation = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        Simulation.RegisterObstacle(this);
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        Simulation.DeregisterObstacle(this);
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void Awake()
    {
        shadowCaster = GetComponent<ShadowCaster2D>();
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        CheckLOD();
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        switch (eventType)
        {
            case GlobalEvent.type.DEGRADE_LOD:
                CheckLOD();
                break;
        }
    }

    void CheckLOD()
    {
        if (shadowLOD > Perf.LOD) TurnOffShadows();
        if (physicsLOD > Perf.LOD) TurnOffPhysics();
    }

    void TurnOffShadows()
    {
        shadowCaster.enabled = false;
    }

    void TurnOffPhysics()
    {
        rigidbody.isKinematic = true;
        rigidbody.simulated = false;
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.yellow.toAlpha(0.4f);
        Gizmos.DrawWireSphere(transform.position, avoidanceMinRadius);
        Gizmos.DrawWireSphere(transform.position, avoidanceMaxRadius);
    }
}
