using UnityEngine;

[CreateAssetMenu(fileName = "BoidStats")]
public class BoidStats : ScriptableObject
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float awarenessLatency = 0.2f;

    [Space]
    [Space]

    public bool reflectOffScreenEdges;
    public float sightDistance = 5f;
    public float steeringVariance = .5f;
    public float steeringVarianceTimeStep = .3f;

    [Space]
    [Space]

    [Range(0f, 1f)] public float cohesion = .5f;
    [Range(0f, 1f)] public float separation = .5f;
    [Range(0f, 1f)] public float alignment = .5f;
    [Range(0f, 1f)] public float followTheLeader = .5f;
    [Range(0f, 1f)] public float avoidance = .5f;
    [Range(0f, 1f)] public float runFromPredators = 1f;

    [Space]
    [Space]

    public AnimationCurve closenessMod = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public float avoidanceTension = 2f;
    public float avoidanceStrength = 1f;
    public float avoidanceRotationMod = 2f;

    public AnimationCurve predatorFleeMod = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Space]
    [Space]

    public float wallRaycastLatency = 0.2f;
    public float wallRaycastVariance = 0.1f;
    public LayerMask wallLayer;
    public AnimationCurve wallAvoidanceMod = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}
