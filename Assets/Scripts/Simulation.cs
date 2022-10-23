using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Simulation
{
    static List<Boid> boids = new List<Boid>();

    // static Boid[] currentBoids = new Boid[1000];
    // static int currentBoidIndex = 0;

    public static void RegisterBoid(Boid current)
    {
        if (boids.Contains(current)) return;
        boids.Add(current);
    }

    public static void DeregisterBoid(Boid current)
    {
        if (!boids.Contains(current)) return;
        boids.Remove(current);
    }

    public static int GetNeighbors(Boid current, Boid[] neighbors, BoidStats stats)
    {
        int total = 0;
        // PrepareCurrentBoidsArray(boids.Count);
        for (int i = 0; i < boids.Count; i++)
        {
            if (total >= neighbors.Length) break;
            if (!boids[i].IsAlive) continue;
            if (current == boids[i]) continue;
            if (!CanSeeOther(current, boids[i], stats.sightDistance)) continue;

            neighbors[total] = boids[i];
            total++;
        }
        return total;
    }

    public static bool CanSeeOther(Boid current, Boid other, float sightDistance)
    {
        return Vector2.Distance(current.transform.position, other.transform.position) <= sightDistance;
    }

    // static void PrepareCurrentBoidsArray(float count)
    // {
    //     currentBoidIndex = 0;
    //     if (count < currentBoids.Length) return;
    //     currentBoids = new Boid[currentBoids.Length * 2];
    // }
}
