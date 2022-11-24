using UnityEngine;

[System.Serializable]
public struct ObstacleData
{
    public bool isInitialized { get; private set; }
    public Vector2 position { get; private set; }
    public float avoidanceMaxRadius { get; private set; }
    public float avoidanceMinRadius { get; private set; }
    public float avoidanceMod { get; private set; }

    Obstacle obstacleRef;

    public void Init(Obstacle incoming)
    {
        isInitialized = true;
        obstacleRef = incoming;
    }

    public void Hydrate()
    {
        if (!isInitialized) throw new UnityException("ObstacleData not initialized!");
        position = obstacleRef.position;
        avoidanceMaxRadius = obstacleRef.avoidanceMaxRadius;
        avoidanceMinRadius = obstacleRef.avoidanceMinRadius;
        avoidanceMod = obstacleRef.avoidanceMod;
    }
}