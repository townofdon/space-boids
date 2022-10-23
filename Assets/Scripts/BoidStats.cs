using UnityEngine;

[CreateAssetMenu(fileName = "BoidStats")]
public class BoidStats : ScriptableObject
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;

    [Space]
    [Space]

    public float sightDistance = 5f;
    public float steeringVariance = .5f;
    public float steeringVarianceTimeStep = .3f;

    [Space]
    [Space]

    [Range(0f, 1f)] public float cohesion = .5f;
    [Range(0f, 1f)] public float separation = .5f;
    [Range(0f, 1f)] public float alignment = .5f;
}
