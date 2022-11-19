using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

struct JUtil
{
    public static bool CanSee(float2 origin, float2 target, float sightDistance)
    {
        if (sightDistance <= 0) return false;
        return math.distance(origin, target) <= sightDistance;
    }

    public static float2 HeadingTo(float2 origin, float2 target)
    {
        return math.normalize(target - origin);
    }

    public static float2 HeadingFrom(float2 origin, float2 target)
    {
        return math.normalize(origin - target);
    }

    // public static bool IsNeighbor(int neighborIndex, int neighborMask)
    // {
    //     return (((1 << neighborIndex)) & neighborMask) > 0;
    // }

    // public static int AddNeighbor(int neighborIndex, int neighborMask)
    // {
    //     if (IsNeighbor(neighborIndex, neighborMask)) return neighborMask;
    //     return neighborMask + (1 << neighborIndex);
    // }
}

// public struct GetNieghborsJob : IJobParallelFor
// {
//     // NOTE - ALL ARRAYS BELOW MUST BE THE SAME LENGTH
//     [ReadOnly] public NativeArray<float2> AllBoids;
//     [ReadOnly] public NativeArray<bool> BoidsToReject;
//     public NativeArray<bool> Neighbors; // out

//     public float2 CurrentBoid;
//     public float SightDistance;

//     public void Execute(int i)
//     {
//         if (BoidsToReject[i]) return;
//         if (!CanSee(CurrentBoid, AllBoids[i], SightDistance)) return;
//         Neighbors[i] = true;
//     }
// }

// // public static int GetObstaclesNearby(Boid current, Obstacle[] obstaclesNearby)
// // {
// //     int total = 0;
// //     for (int i = 0; i < obstacles.Count; i++)
// //     {
// //         if (total >= obstaclesNearby.Length) break;
// //         if (obstacles[i] == null) continue;
// //         if (!obstacles[i].isActiveAndEnabled) continue;
// //         if (current == obstacles[i]) continue;
// //         if (!current.CanSee(obstacles[i])) continue;

// //         obstaclesNearby[total] = obstacles[i];
// //         total++;
// //     }
// //     return total;
// // }

// public struct GetObstaclesNearbyJob : IJobParallelFor
// {

//     [ReadOnly] public NativeArray<float2> AllObstacles;
//     [ReadOnly] public NativeArray<bool> ObstaclesToReject;
//     public NativeArray<bool> Obstacles; // out

//     public float2 ClosestObstacle;
//     public bool FoundClosestObstacle;
//     float MinDistance;
//     float CurrentDistance;

//     [ReadOnly] public float2 CurrentBoid;
//     [ReadOnly] public float SightDistance;

//     public void Execute(int i)
//     {
//         if (ObstaclesToReject[i]) return;
//         if (!CanSee(CurrentBoid, AllObstacles[i], SightDistance)) return;
//         Obstacles[i] = true;
//         CurrentDistance = math.distance(CurrentBoid, AllObstacles[i]);
//         if (!FoundClosestObstacle)
//         {
//             FoundClosestObstacle = true;
//             ClosestObstacle = AllObstacles[i];
//             MinDistance = CurrentDistance;
//             return;
//         }
//         if (CurrentDistance >= MinDistance) return;
//         ClosestObstacle = AllObstacles[i];
//         MinDistance = CurrentDistance;
//     }
// }

// public struct GetPredatorsNearbyJob : IJobParallelFor
// {

//     [ReadOnly] public NativeArray<float2> AllPredators;
//     [ReadOnly] public NativeArray<bool> PredatorsToReject;
//     public NativeArray<bool> Predators; // out

//     public float2 CurrentBoid;
//     public float SightDistance;

//     public void Execute(int i)
//     {
//         if (PredatorsToReject[i]) return;
//         if (!CanSee(CurrentBoid, AllPredators[i], SightDistance)) return;
//         Predators[i] = true;
//     }
// }


// public struct PerceiveEnvironmentJob : IJobParallelFor
// {
//     [ReadOnly] public NativeArray<float2> Positions; // AllBoids
//     [ReadOnly] public NativeArray<float2> Velocities; // AllBoids
//     [ReadOnly] public NativeArray<float2> AllObstacles;
//     [ReadOnly] public NativeArray<float2> AllPredators;
//     [ReadOnly] public NativeArray<float2> AllFoods;
//     [ReadOnly] public NativeArray<bool> RejectBoids;
//     [ReadOnly] public NativeArray<bool> RejectObstacles;
//     [ReadOnly] public NativeArray<bool> RejectPredators;
//     [ReadOnly] public NativeArray<bool> RejectFoods;
//     [ReadOnly] public NativeArray<float> SightDistance;

//     public NativeArray<bool> Neighbors; // 2D array
//     public NativeArray<bool> PredatorsNearby; // 2D array
//     public NativeArray<bool> ObstaclesNearby; // 2D array
//     public NativeArray<bool> ClosestFood; // 2D array

//     public void Execute(int current)
//     {
//         for (int other = 0; other < Positions.Length; other++)
//         {
//             if (current == other) continue;
//             if (RejectBoids[other]) continue;
//             if (!CanSee(Positions[current], Positions[other], SightDistance[current])) return;
//             // Neighbors[_neighborCount] = AllBoidPositions[current];
//             // Neighbors[_neighborCount] = AllBoidPositions[current];
//             // _neighborCount++;
//         }
//     }

//     bool CanSee(float2 origin, float2 target, float sightDistance)
//     {
//         if (sightDistance <= 0) return false;
//         return math.distance(origin, target) <= sightDistance;
//     }
// }

[BurstCompile]
public struct CohesionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;
    [ReadOnly] public NativeArray<float2> Velocities;
    [ReadOnly] public NativeArray<bool> RejectBoids;
    [ReadOnly] public NativeArray<float> SightDistance;
    [ReadOnly] public NativeArray<int> BoidTypes;

    // public NativeArray<bool> Neighbors;

    public NativeArray<float2> Cohesion;

    public void Execute(int current)
    {
        if (RejectBoids[current]) return;
        Cohesion[current] = float2.zero;
        int total = 0;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!JUtil.CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            Cohesion[current] += Positions[other];
            total++;
        }
        if (total == 0) return;
        // divide by total to get the centroid of this boid's neighbors
        Cohesion[current] /= total;
        Cohesion[current] = JUtil.HeadingTo(Positions[current], Cohesion[current]);
    }
}

[BurstCompile]
public struct SeparationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;
    [ReadOnly] public NativeArray<float2> Velocities;
    [ReadOnly] public NativeArray<bool> RejectBoids;
    [ReadOnly] public NativeArray<float> SightDistance;
    [ReadOnly] public NativeArray<int> BoidTypes;

    public NativeArray<float2> Separation;

    public void Execute(int current)
    {
        if (RejectBoids[current]) return;
        Separation[current] = float2.zero;
        float closeness = 0f;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!JUtil.CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            closeness = (SightDistance[current] - math.distance(Positions[current], Positions[other])) / SightDistance[current];
            closeness = math.clamp(closeness, 0f, 1f);
            closeness = math.lerp(Easing.InExpo(closeness), Easing.InOutQuint(closeness), closeness);
            Separation[current] += JUtil.HeadingFrom(Positions[current], Positions[other]) * closeness;
        }
    }
}

[BurstCompile]
public struct AlignmentJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;
    [ReadOnly] public NativeArray<float2> Velocities;
    [ReadOnly] public NativeArray<bool> RejectBoids;
    [ReadOnly] public NativeArray<float> SightDistance;
    [ReadOnly] public NativeArray<int> BoidTypes;

    public NativeArray<float2> Alignment;

    public void Execute(int current)
    {
        if (RejectBoids[current]) return;
        Alignment[current] = float2.zero;
        int total = 0;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!JUtil.CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            Alignment[current] += Velocities[other];
            total++;
        }
        if (total == 0) return;
        Alignment[current] /= total;
        Alignment[current] = math.normalize(Alignment[current]);
    }
}

[BurstCompatible]
public struct BoidsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;
    [ReadOnly] public NativeArray<float2> Velocities;
    [ReadOnly] public NativeArray<int> BoidTypes;
    [ReadOnly] public NativeArray<float2> AllObstacles;
    [ReadOnly] public NativeArray<float2> AllPredators;
    [ReadOnly] public NativeArray<float2> AllFoods;
    [ReadOnly] public NativeArray<bool> RejectBoids;
    [ReadOnly] public NativeArray<bool> RejectObstacles;
    [ReadOnly] public NativeArray<bool> RejectPredators;
    [ReadOnly] public NativeArray<bool> RejectFoods;

    [ReadOnly] public NativeArray<float> SightDistance;

    // public NativeArray<bool> Neighbors;

    public NativeArray<float2> Cohesion;
    public NativeArray<float2> Alignment;
    public NativeArray<float2> Separation;
    public NativeArray<float2> FollowTheLeader;
    public NativeArray<float2> SeekFood;
    public NativeArray<float2> ChompFood;
    public NativeArray<float2> AvoidObstacles;
    public NativeArray<float2> AvoidPredators;

    public void Execute(int current)
    {
        if (RejectBoids[current]) return;

        // PerceiveEnvironment(current);
        CheckCohesion(current);
        CheckSeparation(current);
        CheckAlignment(current);
        CheckFollowTheLeader(current);
        CheckSeekFood(current);
        CheckChompFood(current);
        CheckAvoidObstacles(current);
        CheckAvoidPredators(current);
    }

    void PerceiveEnvironment(int current)
    {
        // _neighborCount = 0;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!CanSee(Positions[current], Positions[other], SightDistance[current])) return;
            // Neighbors[_neighborCount] = AllBoidPositions[current];
            // Neighbors[_neighborCount] = AllBoidPositions[current];
            // _neighborCount++;
        }
    }

    void CheckCohesion(int current)
    {
        Cohesion[current] = float2.zero;
        int total = 0;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            Cohesion[current] += Positions[other];
            total++;
        }
        if (total == 0) return;
        // divide by total to get the centroid of this boid's neighbors
        Cohesion[current] /= total;
        Cohesion[current] = HeadingTo(Positions[current], Cohesion[current]);
    }

    void CheckSeparation(int current)
    {
        Separation[current] = float2.zero;
        float closeness = 0f;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            closeness = (SightDistance[current] - math.distance(Positions[current], Positions[other])) / SightDistance[current];
            closeness = math.clamp(closeness, 0f, 1f);
            closeness = math.lerp(Easing.InExpo(closeness), Easing.InOutQuint(closeness), closeness);
            Separation[current] += HeadingFrom(Positions[current], Positions[other]) * closeness;
        }
    }

    void CheckAlignment(int current)
    {
        Alignment[current] = float2.zero;
        int total = 0;
        for (int other = 0; other < Positions.Length; other++)
        {
            if (current == other) continue;
            if (RejectBoids[other]) continue;
            if (BoidTypes[current] != BoidTypes[other]) continue;
            if (!CanSee(Positions[current], Positions[other], SightDistance[current])) continue;
            // if (!IsNeighbor(other, Neighbors[current])) continue;
            Alignment[current] += Velocities[other];
            total++;
        }
        if (total == 0) return;
        Alignment[current] /= total;
        Alignment[current] = math.normalize(Alignment[current]);
    }

    void CheckFollowTheLeader(int current)
    {
        FollowTheLeader[current] = float2.zero;
    }

    void CheckSeekFood(int current)
    {
        SeekFood[current] = float2.zero;
    }

    void CheckChompFood(int current)
    {
        ChompFood[current] = float2.zero;
    }

    void CheckAvoidObstacles(int current)
    {
        AvoidObstacles[current] = float2.zero;
    }

    void CheckAvoidPredators(int current)
    {
        AvoidPredators[current] = float2.zero;
    }


    // 
    // UTILS
    // 

    bool CanSee(float2 origin, float2 target, float sightDistance)
    {
        if (sightDistance <= 0) return false;
        if (math.abs(origin.x - target.x) > sightDistance) return false;
        if (math.abs(origin.y - target.y) > sightDistance) return false;
        return math.distance(origin, target) <= sightDistance;
    }

    float2 HeadingTo(float2 origin, float2 target)
    {
        return math.normalize(target - origin);
    }

    float2 HeadingFrom(float2 origin, float2 target)
    {
        return math.normalize(origin - target);
    }
}
