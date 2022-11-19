using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

public class BoidManager : MonoBehaviour
{
    private const int DEBUG_COUNT = 1000;
    float2[] _positions = new float2[DEBUG_COUNT];
    // float2[] _neighbors = new float2[DEBUG_COUNT];
    bool[] _reject = new bool[DEBUG_COUNT];
    float2[] _cohesion = new float2[DEBUG_COUNT];
    float2[] _alignment = new float2[DEBUG_COUNT];
    float2[] _separation = new float2[DEBUG_COUNT];

    GameManager gameManager;

    void Awake()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        if (!gameManager.UseJobs) return;

        Debug.Log("BoidManager");

        NativeArray<float2> AllBoidPositions = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> AllBoidVelocities = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> AllObstacles = new NativeArray<float2>(Simulation.Obstacles.Count, Allocator.TempJob);
        NativeArray<float2> AllPredators = new NativeArray<float2>(Simulation.Predators.Count, Allocator.TempJob);
        NativeArray<float2> AllFoods = new NativeArray<float2>(Simulation.Foods.Count, Allocator.TempJob);
        NativeArray<bool> RejectBoids = new NativeArray<bool>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<bool> RejectObstacles = new NativeArray<bool>(Simulation.Obstacles.Count, Allocator.TempJob);
        NativeArray<bool> RejectPredators = new NativeArray<bool>(Simulation.Predators.Count, Allocator.TempJob);
        NativeArray<bool> RejectFoods = new NativeArray<bool>(Simulation.Foods.Count, Allocator.TempJob);
        NativeArray<float> SightDistance = new NativeArray<float>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<int> BoidTypes = new NativeArray<int>(Simulation.Boids.Count, Allocator.TempJob);
        // NativeArray<float2> Neighbors = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> Cohesion = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> Alignment = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> Separation = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> FollowTheLeader = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> SeekFood = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> ChompFood = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> AvoidObstacles = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);
        NativeArray<float2> AvoidPredators = new NativeArray<float2>(Simulation.Boids.Count, Allocator.TempJob);

        for (int i = 0; i < Simulation.Boids.Count; i++)
        {
            AllBoidPositions[i] = Simulation.Boids[i].position;
            AllBoidVelocities[i] = Simulation.Boids[i].velocity;
            SightDistance[i] = Simulation.Boids[i].SightDistance;
            BoidTypes[i] = (int)Simulation.Boids[i].Type;
            RejectBoids[i] = ShouldReject(Simulation.Boids[i]);
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

        // BoidsJob boidsJob = new BoidsJob
        // {
        //     Positions = AllBoidPositions,
        //     Velocities = AllBoidVelocities,
        //     AllObstacles = AllObstacles,
        //     AllPredators = AllPredators,
        //     AllFoods = AllFoods,
        //     RejectBoids = RejectBoids,
        //     RejectObstacles = RejectObstacles,
        //     RejectPredators = RejectPredators,
        //     RejectFoods = RejectFoods,
        //     SightDistance = SightDistance,
        //     BoidTypes = BoidTypes,
        //     Cohesion = Cohesion,
        //     Alignment = Alignment,
        //     Separation = Separation,
        //     FollowTheLeader = FollowTheLeader,
        //     SeekFood = SeekFood,
        //     ChompFood = ChompFood,
        //     AvoidObstacles = AvoidObstacles,
        //     AvoidPredators = AvoidPredators,
        // };

        // JobHandle jobHandle = boidsJob.Schedule(AllBoidPositions.Length, 1);

        // jobHandle.Complete();



        for (int i = 0; i < Simulation.Boids.Count; i++)
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

            if (ShouldReject(Simulation.Boids[i]) || RejectBoids[i]) continue;
            Simulation.Boids[i].SetCohesion(Cohesion[i]);
            Simulation.Boids[i].SetSeparation(Separation[i]);
            Simulation.Boids[i].SetAlignment(Alignment[i]);
            Simulation.Boids[i].SetFollowTheLeader(FollowTheLeader[i]);
            Simulation.Boids[i].SetAvoidObstacles(AvoidObstacles[i]);
            Simulation.Boids[i].SetAvoidPredators(AvoidPredators[i]);
            Simulation.Boids[i].SetSeekFood(SeekFood[i]);
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
        // Neighbors.Dispose();
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