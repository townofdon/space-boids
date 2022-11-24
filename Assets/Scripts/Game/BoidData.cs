using UnityEngine;

[System.Serializable]
public struct BoidData
{
    public bool isInitialized { get; private set; }
    public bool debug { get; private set; }
    public Vector2 position { get; private set; }
    public Vector2 velocity { get; private set; }

    public Vector2 cohesion;
    public Vector2 separation;
    public Vector2 alignment;
    public Vector2 avoidObstacles;
    public Vector2 avoidPredators;
    public Vector2 seekFood;
    public float maxObstacleCloseness;

    public float sightDistance { get; private set; }
    public float avoidanceStrength { get; private set; }
    public float avoidanceTension { get; private set; }

    public float awarenessLatency { get; private set; }
    public float timeSinceLastPerceived { get; private set; }
    public bool isAlive { get; private set; }
    public float neighborCountQuotient { get; private set; }
    public Boid.BoidType boidType { get; private set; }
    public Food.FoodType foodType { get; private set; }

    public float closestFoodDistance;
    public Vector2 closestFoodPosition;
    public Food closestFoodRef { private get; set; }

    bool didPerceive;
    int neighborsCount;

    // make sure to only modify these values, not read from them to avoid L2 cache misses
    Boid boidRef;

    public Boid Entity => boidRef;
    public Food ClosestFood => closestFoodRef;

    public void Init(Boid incoming)
    {
        isInitialized = true;
        boidRef = incoming;
        boidType = boidRef.Type;
        foodType = boidRef.GetFoodType();
        timeSinceLastPerceived = UnityEngine.Random.Range(0f, boidRef.StatAwarenessLatency);
    }

    public void BeforeFrame()
    {
        if (!isInitialized) throw new UnityException("BoidData is not initialized");
        didPerceive = false;

        cohesion = Vector2.zero;
        separation = Vector2.zero;
        alignment = Vector2.zero;
        avoidObstacles = Vector2.zero;
        avoidPredators = Vector2.zero;
        seekFood = Vector2.zero;

        position = boidRef.position;
        velocity = boidRef.velocity;
        isAlive = boidRef.IsAlive;
        debug = boidRef.IsDebugging;
        if (awarenessLatency == 0) CalcAwarenessLatency();
    }

    public bool CanPerceive()
    {
        return timeSinceLastPerceived >= awarenessLatency;
    }

    public void BeforePerceive()
    {
        neighborsCount = 0;
        neighborCountQuotient = 0f;
        maxObstacleCloseness = 0f;
        closestFoodDistance = float.MaxValue;
        closestFoodPosition = Vector2.zero;
        closestFoodRef = null;
    }

    public void AfterFoundNeighbor()
    {
        neighborsCount++;
    }

    public void AfterPerceive()
    {
        didPerceive = true;
        neighborCountQuotient = neighborsCount > 0 ? 1f / neighborsCount : 0f;
    }

    public void AfterFrame()
    {
        if (!isInitialized) throw new UnityException("BoidData is not initialized");
        if (didPerceive)
        {
            timeSinceLastPerceived = 0f;
            UpdateEntity();
            CalcAwarenessLatency();
        }
        else
        {
            timeSinceLastPerceived += Time.deltaTime * Simulation.speed;
        }
    }

    void UpdateEntity()
    {
        boidRef.SetCohesion(cohesion);
        boidRef.SetSeparation(separation);
        boidRef.SetAlignment(alignment);
        boidRef.SetAvoidObstacles(avoidObstacles);
        boidRef.SetAvoidPredators(avoidPredators);
        boidRef.SetSeekFood(seekFood);
        boidRef.SetMaxCloseness(maxObstacleCloseness);
        boidRef.SetClosestFood(closestFoodRef);
    }

    void CalcAwarenessLatency()
    {
        awarenessLatency = boidRef.StatAwarenessLatency + UnityEngine.Random.Range(-boidRef.StatAwarenessVariance * .5f, boidRef.StatAwarenessVariance * .5f);
        sightDistance = boidRef.StatSightDistance;
        avoidanceStrength = boidRef.StatAvoidanceStrength;
        avoidanceTension = boidRef.StatAvoidanceTension;
    }
}
