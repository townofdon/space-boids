using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Simulation
{
    static List<Boid> boids = new List<Boid>();
    static List<Obstacle> obstacles = new List<Obstacle>();
    static List<Predator> predators = new List<Predator>();
    static List<Food> foods = new List<Food>();

    public static List<Boid> _debug_boids => boids;
    public static List<Obstacle> _debug_obstacles => obstacles;

    public static float speed = 1f;

    public static void SetSimulationSpeed(float val)
    {
        speed = val;
    }

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

    public static void RegisterObstacle(Obstacle current)
    {
        if (obstacles.Contains(current)) return;
        obstacles.Add(current);
    }

    public static void DeregisterObstacle(Obstacle current)
    {
        if (!obstacles.Contains(current)) return;
        obstacles.Remove(current);
    }

    public static void RegisterPredator(Predator current)
    {
        if (predators.Contains(current)) return;
        predators.Add(current);
    }

    public static void DeregisterPredator(Predator current)
    {
        if (!predators.Contains(current)) return;
        predators.Remove(current);
    }

    public static void RegisterFood(Food current)
    {
        if (foods.Contains(current)) return;
        foods.Add(current);
    }

    public static void DeregisterFood(Food current)
    {
        if (!foods.Contains(current)) return;
        foods.Remove(current);
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        boids.Clear();
        obstacles.Clear();
        predators.Clear();
        // foreach (var boid in Object.FindObjectsOfType<Boid>())
        // {
        //     boids.Add(boid);
        // }
        // foreach (var obstacle in Object.FindObjectsOfType<Obstacle>())
        // {
        //     obstacles.Add(obstacle);
        // }
    }

    // public static void Init()
    // {
    //     Debug.Log("Init");
    //     boids.Clear();
    //     obstacles.Clear();
    // }

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
        // naive implementation - just return the first boid in the array
        if (neighborCount <= 0) return null;
        for (int i = 0; i < neighborCount; i++)
        {
            if (neighbors[i] == null) continue;
            if (!neighbors[i].IsAlive) continue;
            if (neighbors[i] == current) continue;
            return neighbors[i];
        }
        return null;
        // int leaderIndex = -1;
        // float maxForwardness = -1f;
        // for (int i = 0; i < neighborCount; i++)
        // {
        //     if (neighbors[i] == null) continue;
        //     if (!neighbors[i].IsAlive) continue;
        //     if (leaderIndex >= 0 && neighbors[i] == neighbors[leaderIndex]) continue;
        //     if (neighbors[i] == current) continue;
        //     if (current.ForwardnessTo(neighbors[i]) <= maxForwardness) continue;
        //     leaderIndex = i;
        //     maxForwardness = current.ForwardnessTo(neighbors[i]);
        // }
        // return leaderIndex >= 0 ? neighbors[leaderIndex] : null;
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

    public static int GetNeighbors(Boid current, Boid[] others)
    {
        int total = 0;
        for (int i = 0; i < boids.Count; i++)
        {
            if (total >= others.Length) break;
            if (boids[i] == null) continue;
            if (boids[i].Type != current.Type) continue;
            if (!boids[i].IsAlive) continue;
            if (current == boids[i]) continue;
            if (!current.CanSee(boids[i])) continue;

            others[total] = boids[i];
            total++;
        }
        return total;
    }

    public static int GetObstaclesNearby(Boid current, Obstacle[] obstaclesNearby)
    {
        int total = 0;
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (total >= obstaclesNearby.Length) break;
            if (obstacles[i] == null) continue;
            if (!obstacles[i].isActiveAndEnabled) continue;
            if (current == obstacles[i]) continue;
            if (!current.CanSee(obstacles[i])) continue;

            obstaclesNearby[total] = obstacles[i];
            total++;
        }
        return total;
    }

    public static int GetPredatorsNearby(Boid current, Predator[] predatorsNearby)
    {
        int total = 0;
        for (int i = 0; i < predators.Count; i++)
        {
            if (total >= predatorsNearby.Length) break;
            if (predators[i] == null) continue;
            if (!predators[i].isActiveAndEnabled) continue;
            if (current == predators[i]) continue;
            if (!current.CanSee(predators[i])) continue;

            predatorsNearby[total] = predators[i];
            total++;
        }
        return total;
    }

    public static Obstacle GetClosestObstacle(Boid current, ref Obstacle closestObstacle, Obstacle[] obstaclesNearby, int obstaclesNearbyCount)
    {
        closestObstacle = null;
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < obstaclesNearbyCount; i++)
        {
            if (obstaclesNearby[i] == null) continue;
            if (!obstaclesNearby[i].isActiveAndEnabled) continue;
            if (obstaclesNearby[i] == closestObstacle) continue;
            if (obstaclesNearby[i] == current) continue;
            if (current.ForwardnessTo(obstaclesNearby[i]) <= 0) continue;
            if (current.DistanceTo(obstaclesNearby[i]) >= minDistance) continue;
            closestObstacle = obstaclesNearby[i];
            minDistance = current.DistanceTo(obstaclesNearby[i]);
        }
        return closestObstacle;
    }

    public static int GetFoods(Boid current, Food[] currentBoidFoods)
    {
        int total = 0;
        for (int i = 0; i < foods.Count; i++)
        {
            if (total >= currentBoidFoods.Length) break;
            if (foods[i] == null) continue;
            if (foods[i].isEaten) continue;
            if (foods[i].foodType != current.GetFoodType()) continue;
            if (!foods[i].isActiveAndEnabled) continue;

            currentBoidFoods[total] = foods[i];
            total++;
        }
        return total;
    }

    public static Food GetClosestFood(Boid current, Food[] foods, int foodCount)
    {
        Food closest = null;
        float closestDistance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;
        for (int i = 0; i < foodCount; i++)
        {
            if (foods[i] == null) continue;
            if (foods[i].isEaten) continue;
            if (foods[i].foodType != current.GetFoodType()) continue;
            if (closest == null)
            {
                closest = foods[i];
                continue;
            }
            currentDistance = Vector2.Distance(current.transform.position, foods[i].transform.position);
            if (currentDistance < closestDistance)
            {
                closest = foods[i];
                closestDistance = currentDistance;
            }
        }
        return closest;
    }
}
