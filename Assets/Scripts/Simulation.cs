using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Simulation
{
    static List<Boid> boids = new List<Boid>();

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

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        boids.Clear();
        foreach (var boid in Object.FindObjectsOfType<Boid>())
        {
            boids.Add(boid);
        }
    }

    public static void Init()
    {
        boids.Clear();
    }

    public static Boid GetClosestNeighbor(Boid current, Boid[] neighbors, int neighborCount)
    {
        Boid closest = null;
        float closestDistance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;
        for (int i = 0; i < neighborCount; i++)
        {
            if (neighbors[i] == null || !neighbors[i].IsAlive || neighbors[i] == current)
            {
                continue;
            }
            if (closest == null)
            {
                closest = neighbors[i];
                continue;
            }
            currentDistance = Vector2.Distance(current.transform.position, neighbors[i].transform.position);
            if (currentDistance < closestDistance)
            {
                closest = neighbors[i];
                closestDistance = currentDistance;
            }
        }
        return closest;
    }

    public static Boid GetLeader(Boid current, Boid[] neighbors, int neighborCount)
    {
        Boid leader = null;
        float maxForwardness = -1f;
        for (int i = 0; i < neighborCount; i++)
        {
            if (neighbors[i] == null) continue;
            if (!neighbors[i].IsAlive) continue;
            if (neighbors[i] == leader) continue;
            if (neighbors[i] == current) continue;
            if (current.ForwardnessTo(neighbors[i]) <= maxForwardness) continue;
            leader = neighbors[i];
            maxForwardness = current.ForwardnessTo(neighbors[i]);
        }
        return leader;
    }

    // public static Boid GetLeader(Boid current, Boid[] neighbors, int neighborCount)
    // {
    //     Boid leader = null;
    //     int i = 0;
    //     // int j = 0;
    //     while (i < neighborCount)
    //     {
    //         if (neighbors[i] == null || !neighbors[i].IsAlive || neighbors[i] == leader || neighbors[i] == current)
    //         {
    //             i++;
    //             continue;
    //         }
    //         if (leader == null && current.IsBehind(neighbors[i]) && neighbors[i].CurrentlyFollowing != current)
    //         {
    //             leader = neighbors[i].CurrentlyFollowing;
    //             i++;
    //             continue;
    //         }
    //         if (leader != null && leader.IsBehind(neighbors[i]) && neighbors[i].CurrentlyFollowing != leader)
    //         {
    //             leader = neighbors[i].CurrentlyFollowing;
    //             i++;

    //             // if (j < 5) i = 0;
    //             // j++;
    //             continue;
    //         }
    //         i++;
    //     }
    //     return leader;
    // }

    public static int GetNeighbors(Boid current, Boid[] others, BoidStats stats)
    {
        int total = 0;
        // PrepareCurrentBoidsArray(boids.Count);
        for (int i = 0; i < boids.Count; i++)
        {
            if (total >= others.Length) break;
            if (boids[i] == null) continue;
            if (!boids[i].IsAlive) continue;
            if (current == boids[i]) continue;
            if (!CanSeeOther(current, boids[i], stats.sightDistance)) continue;

            others[total] = boids[i];
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
