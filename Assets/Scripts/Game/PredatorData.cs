using UnityEngine;

public struct PredatorData
{
    public Vector2 position { get; private set; }
    public float scarinessMod { get; private set; }

    bool isInitialized;
    Predator predatorRef;

    public void Init(Predator incoming)
    {
        isInitialized = true;
        predatorRef = incoming;
    }

    public void Hydrate()
    {
        if (!isInitialized) throw new UnityException("PredatorData not initialized");
        position = predatorRef.position;
        scarinessMod = predatorRef.scarinessMod;
    }
}
