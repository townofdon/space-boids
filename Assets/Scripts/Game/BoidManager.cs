using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

// The meat of the algorithm exists in this file. You might be asking why various
// lists from Simulation.cs are copied into arrays of structs. The answer is
// CPU cache optimization.
//
// Because structs are value types (as opposed to reference types), an
// array of structs lends itself to being cached by the L1, L2, or L3 cache.
// This optimization provides a HUGE performance boost.
//
// Simply using structs in place of classes is not enough... you must also
// try and access the data sequentially if possible. This is why each
// array below is traversed one-by-one, and calls to reference types all
// take place either in beforeFrame() or afterFrame() to try and minimize the
// number of cache misses.
//
// This article explains this a lot better: https://www.jacksondunstan.com/articles/3860

public class BoidManager : MonoBehaviour
{
    const int DEBUG_COUNT = 1000;
    float2[] _positions = new float2[DEBUG_COUNT];
    // float2[] _neighbors = new float2[DEBUG_COUNT];
    bool[] _reject = new bool[DEBUG_COUNT];
    float2[] _cohesion = new float2[DEBUG_COUNT];
    float2[] _alignment = new float2[DEBUG_COUNT];
    float2[] _separation = new float2[DEBUG_COUNT];

    GameManager gameManager;

    BoidData[] boids = new BoidData[1000];
    ObstacleData[] obstacles = new ObstacleData[1000];
    PredatorData[] predators = new PredatorData[1000];
    FoodData[] foods = new FoodData[50];
    int boidCount = 0;
    int obstacleCount = 0;
    int predatorCount = 0;
    int foodCount = 0;

    void Awake()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        Simulation.Init();
    }

    void OnEnable()
    {
        Simulation.OnFoodsChanged += OnFoodsChanged;
    }

    void OnDisable()
    {
        Simulation.OnFoodsChanged -= OnFoodsChanged;
    }

    void OnFoodsChanged()
    {
        foodCount = Simulation.foods.Count;
        while (foodCount > foods.Length)
        {
            foods = new FoodData[Mathf.Max(foods.Length * 2, 50)];
        }
        for (int i = 0; i < foodCount; i++)
        {
            foods[i] = new FoodData();
            foods[i].Init(Simulation.foods[i]);
        }
    }

    void Start()
    {
        // Note - we only initialize these boids here in Start
        // since this simulation is a naive implementation. In a large-scale
        // game we would most likely place this inside of an event callback.
        boidCount = Simulation.boids.Count;
        while (boidCount > boids.Length)
        {
            boids = new BoidData[Mathf.Max(boids.Length * 2, 1000)];
        }
        for (int i = 0; i < boidCount; i++)
        {
            boids[i] = new BoidData();
            boids[i].Init(Simulation.boids[i]);
        }

        obstacleCount = Simulation.obstacles.Count;
        while (obstacleCount > obstacles.Length)
        {
            obstacles = new ObstacleData[Mathf.Max(obstacles.Length * 2, 1000)];
        }
        for (int i = 0; i < obstacleCount; i++)
        {
            obstacles[i] = new ObstacleData();
            obstacles[i].Init(Simulation.obstacles[i]);
        }

        predatorCount = Simulation.predators.Count;
        while (predatorCount > predators.Length)
        {
            predators = new PredatorData[Mathf.Max(predators.Length * 2, 1000)];
        }
        for (int i = 0; i < predatorCount; i++)
        {
            predators[i] = new PredatorData();
            predators[i].Init(Simulation.predators[i]);
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (gameManager.UseJobs)
        {
            UpdateWithJobs();
        }
        else
        {
            UpdateWithoutJobs();
        }
    }

    void UpdateWithoutJobs()
    {
        for (int i = 0; i < boidCount; i++)
        {
            boids[i].BeforeFrame();
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            obstacles[i].Hydrate();
        }

        for (int i = 0; i < predatorCount; i++)
        {
            predators[i].Hydrate();
        }

        for (int i = 0; i < foodCount; i++)
        {
            foods[i].Hydrate();
        }

        for (int current = 0; current < boidCount; current++)
        {
            bool debug = boids[current].debug;

            if (!boids[current].isAlive) continue;
            if (!boids[current].CanPerceive()) continue;

            boids[current].BeforePerceive();
            float sightDistance = boids[current].sightDistance;
            float sightDistanceQuotient = 1f / sightDistance;

            #region NEIGHBORS
            for (int other = 0; other < boidCount; other++)
            {

                if (current == other) continue;
                if (!boids[other].isAlive) continue;
                if (boids[current].boidType != boids[other].boidType) continue;

                if (!BoidHelpers.CanSee(
                    boids[current].position,
                    boids[other].position,
                    boids[current].sightDistance,
                    boids[current].velocity)
                ) continue;

                #region COHESION_AGGREGATE
                boids[current].cohesion += boids[other].position;
                #endregion COHESION_AGGREGATE

                #region SEPARATION_AGGREGATE
                float closeness = 0f;
                float distance = 0f;
                distance = BoidHelpers.DistanceTo(boids[current].position, boids[other].position);
                closeness = Mathf.Clamp01((sightDistance - distance) * sightDistanceQuotient);
                closeness = Mathf.Lerp(Easing.InExpo(closeness), Easing.InOutQuint(closeness), closeness);
                boids[current].separation += BoidHelpers.HeadingFrom(
                    boids[current].position,
                    boids[other].position
                ) * closeness;
                #endregion SEPARATION_AGGREGATE

                #region ALIGNMENT_AGGREGATE
                float forwardness = BoidHelpers.ForwardnessTo(
                    boids[current].position,
                    boids[other].position,
                    boids[current].velocity
                );
                boids[current].alignment += boids[other].velocity * Mathf.Clamp01(forwardness + .8f);
                #endregion ALIGNMENT_AGGREGATE

                boids[current].AfterFoundNeighbor();
            }
            #endregion NEIGHBORS

            #region OBSTACLES
            for (int i = 0; i < obstacleCount; i++)
            {
                if (!BoidHelpers.CanSee(
                    boids[current].position,
                    obstacles[i].position,
                    boids[current].sightDistance,
                    boids[current].velocity)
                ) continue;
                if (!BoidHelpers.IsInRange(
                    boids[current].position,
                    obstacles[i].position,
                    obstacles[i].avoidanceMaxRadius
                )) continue;

                #region AVOID_OBSTACLES_AGGREGATE
                float closeness = 0f;
                float distance = BoidHelpers.DistanceTo(boids[current].position, obstacles[i].position);
                // get closeness as a value from MIN to MAX
                closeness = obstacles[i].avoidanceMaxRadius - (distance - obstacles[i].avoidanceMinRadius);
                // get closeness where 0 => MIN, 1 => MAX avoidance radius
                closeness = Mathf.Clamp01(closeness / (obstacles[i].avoidanceMaxRadius - obstacles[i].avoidanceMinRadius));
                if (closeness < boids[current].maxObstacleCloseness) boids[current].maxObstacleCloseness = closeness;
                closeness = Mathf.Pow(closeness, boids[current].avoidanceTension);
                closeness = closeness * boids[current].avoidanceStrength * obstacles[i].avoidanceMod;
                boids[current].avoidObstacles += BoidHelpers.HeadingFrom(
                    boids[current].position,
                    obstacles[i].position
                );
                #endregion AVOID_OBSTACLES_AGGREGATE
            }
            #endregion OBSTACLES

            #region PREDATORS
            for (int i = 0; i < predatorCount; i++)
            {
                if (!BoidHelpers.CanSee(
                    boids[current].position,
                    predators[i].position,
                    boids[current].sightDistance,
                    boids[current].velocity)
                ) continue;

                #region AVOID_PREDATORS_AGGREGATE
                float closeness = 0f;
                float distance = 0f;
                distance = BoidHelpers.DistanceTo(boids[current].position, predators[i].position);
                closeness = sightDistance - distance;
                closeness = Mathf.Clamp01(closeness * sightDistanceQuotient);
                closeness = Easing.OutExpo(Easing.OutExpo(closeness));
                boids[current].avoidPredators += BoidHelpers.HeadingFrom(boids[current].position, predators[i].position) * closeness;
                #endregion AVOID_PREDATORS_AGGREGATE
            }
            #endregion PREDATORS

            #region FOOD
            for (int i = 0; i < foodCount; i++)
            {
                if (!foods[i].isAvailable) continue;
                if (foods[i].foodType != boids[current].foodType) continue;

                #region SEEK_FOOD_FIND_CLOSEST
                float distance = BoidHelpers.DistanceTo(boids[current].position, foods[i].position);
                if (distance < boids[current].closestFoodDistance)
                {
                    boids[current].closestFoodDistance = distance;
                    boids[current].closestFoodPosition = foods[i].position;
                    boids[current].closestFoodRef = foods[i].foodRef;
                }
                #endregion SEEK_FOOD_FIND_CLOSEST
            }
            if (boids[current].closestFoodDistance < (float.MaxValue - Mathf.Epsilon))
            {
                boids[current].seekFood = BoidHelpers.HeadingTo(boids[current].position, boids[current].closestFoodPosition);
            }
            #endregion FOOD

            boids[current].AfterPerceive();

            #region COHESION_CALC
            boids[current].cohesion *= boids[current].neighborCountQuotient;
            boids[current].cohesion = BoidHelpers.HeadingTo(boids[current].position, boids[current].cohesion);
            if (boids[current].neighborsCount == 0) boids[current].cohesion = Vector2.zero;
            #endregion COHESION_CALC

            #region ALIGNMENT_CALC
            boids[current].alignment *= boids[current].neighborCountQuotient;
            boids[current].alignment = boids[current].alignment.normalized;
            if (boids[current].neighborsCount == 0) boids[current].alignment = Vector2.zero;
            #endregion ALIGNMENT_CALC
        }

        for (int i = 0; i < boidCount; i++)
        {
            boids[i].AfterFrame();
        }
    }

    void UpdateWithJobs()
    {
        if (!gameManager.UseJobs) return;

        NativeArray<float2> AllBoidPositions = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> AllBoidVelocities = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> AllObstacles = new NativeArray<float2>(Simulation.obstacles.Count, Allocator.TempJob);
        NativeArray<float2> AllPredators = new NativeArray<float2>(Simulation.predators.Count, Allocator.TempJob);
        NativeArray<float2> AllFoods = new NativeArray<float2>(Simulation.foods.Count, Allocator.TempJob);
        NativeArray<bool> RejectBoids = new NativeArray<bool>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<bool> RejectObstacles = new NativeArray<bool>(Simulation.obstacles.Count, Allocator.TempJob);
        NativeArray<bool> RejectPredators = new NativeArray<bool>(Simulation.predators.Count, Allocator.TempJob);
        NativeArray<bool> RejectFoods = new NativeArray<bool>(Simulation.foods.Count, Allocator.TempJob);
        NativeArray<float> SightDistance = new NativeArray<float>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<int> BoidTypes = new NativeArray<int>(Simulation.boids.Count, Allocator.TempJob);
        // NativeArray<float2> Neighbors = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> Cohesion = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> Alignment = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> Separation = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> FollowTheLeader = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> SeekFood = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> ChompFood = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> AvoidObstacles = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);
        NativeArray<float2> AvoidPredators = new NativeArray<float2>(Simulation.boids.Count, Allocator.TempJob);

        for (int i = 0; i < Simulation.boids.Count; i++)
        {
            AllBoidPositions[i] = Simulation.boids[i].position;
            AllBoidVelocities[i] = Simulation.boids[i].velocity;
            SightDistance[i] = Simulation.boids[i].StatSightDistance;
            BoidTypes[i] = (int)Simulation.boids[i].Type;
            RejectBoids[i] = ShouldReject(Simulation.boids[i]);
        }

        // TODO: add obstacles, predators, foods

        CohesionJob cohesionJob = new CohesionJob
        {
            Positions = AllBoidPositions,
            Velocities = AllBoidVelocities,
            RejectBoids = RejectBoids,
            SightDistance = SightDistance,
            BoidTypes = BoidTypes,
            Cohesion = Cohesion,
        };

        SeparationJob separationJob = new SeparationJob
        {
            Positions = AllBoidPositions,
            Velocities = AllBoidVelocities,
            RejectBoids = RejectBoids,
            SightDistance = SightDistance,
            BoidTypes = BoidTypes,
            Separation = Separation,
        };

        AlignmentJob alignmentJob = new AlignmentJob
        {
            Positions = AllBoidPositions,
            Velocities = AllBoidVelocities,
            RejectBoids = RejectBoids,
            SightDistance = SightDistance,
            BoidTypes = BoidTypes,
            Alignment = Alignment,
        };

        JobHandle cohesionHandle = cohesionJob.Schedule(AllBoidPositions.Length, 1);
        JobHandle separationHandle = separationJob.Schedule(AllBoidPositions.Length, 1);
        JobHandle alignmentHandle = alignmentJob.Schedule(AllBoidPositions.Length, 1);

        cohesionHandle.Complete();
        separationHandle.Complete();
        alignmentHandle.Complete();

        for (int i = 0; i < Simulation.boids.Count; i++)
        {
            if (i < DEBUG_COUNT)
            {
                _positions[i] = AllBoidPositions[i];
                // _neighbors[i] = Neighbors[i];
                _reject[i] = RejectBoids[i];
                _cohesion[i] = Cohesion[i];
                _alignment[i] = Alignment[i];
                _separation[i] = Separation[i];
            }

            if (ShouldReject(Simulation.boids[i]) || RejectBoids[i]) continue;
            Simulation.boids[i].SetCohesion(Cohesion[i]);
            Simulation.boids[i].SetSeparation(Separation[i]);
            Simulation.boids[i].SetAlignment(Alignment[i]);
            Simulation.boids[i].SetFollowTheLeader(FollowTheLeader[i]);
            Simulation.boids[i].SetAvoidObstacles(AvoidObstacles[i]);
            Simulation.boids[i].SetAvoidPredators(AvoidPredators[i]);
            Simulation.boids[i].SetSeekFood(SeekFood[i]);
        }

        AllBoidPositions.Dispose();
        AllBoidVelocities.Dispose();
        AllObstacles.Dispose();
        AllPredators.Dispose();
        AllFoods.Dispose();
        RejectBoids.Dispose();
        RejectObstacles.Dispose();
        RejectPredators.Dispose();
        RejectFoods.Dispose();
        SightDistance.Dispose();
        BoidTypes.Dispose();
        Cohesion.Dispose();
        Alignment.Dispose();
        Separation.Dispose();
        FollowTheLeader.Dispose();
        SeekFood.Dispose();
        ChompFood.Dispose();
        AvoidObstacles.Dispose();
        AvoidPredators.Dispose();
    }

    bool ShouldReject(Boid boid)
    {
        if (boid == null) return true;
        if (!boid.isActiveAndEnabled) return true;
        if (!boid.IsAlive) return true;
        return false;
    }

    bool ShouldReject(Obstacle obstacle)
    {
        if (obstacle == null) return true;
        if (!obstacle.isActiveAndEnabled) return true;
        return false;
    }

    bool ShouldReject(Predator predator)
    {
        if (predator == null) return true;
        if (!predator.isActiveAndEnabled) return true;
        return false;
    }

    bool ShouldReject(Food food)
    {
        if (food == null) return true;
        if (!food.isActiveAndEnabled) return true;
        if (food.isEaten) return true;
        return false;
    }
}