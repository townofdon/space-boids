using System.Collections.Generic;
using UnityEngine;

public static class Simulation
{
    static List<Boid> _boids = new List<Boid>();
    static List<Obstacle> _obstacles = new List<Obstacle>();
    static List<Predator> _predators = new List<Predator>();
    static List<Food> _foods = new List<Food>();

    public static List<Boid> boids { get => _boids; }
    public static List<Obstacle> obstacles { get => _obstacles; }
    public static List<Predator> predators { get => _predators; }
    public static List<Food> foods { get => _foods; }

    public static System.Action OnFoodsChanged;

    public static float speed = 1f;

    public static void SetSimulationSpeed(float val)
    {
        speed = val;
    }

    public static void RegisterBoid(Boid current)
    {
        if (_boids.Contains(current)) return;
        _boids.Add(current);
    }

    public static void DeregisterBoid(Boid current)
    {
        if (!_boids.Contains(current)) return;
        _boids.Remove(current);
    }

    public static void RegisterObstacle(Obstacle current)
    {
        if (_obstacles.Contains(current)) return;
        _obstacles.Add(current);
    }

    public static void DeregisterObstacle(Obstacle current)
    {
        if (!_obstacles.Contains(current)) return;
        _obstacles.Remove(current);
    }

    public static void RegisterPredator(Predator current)
    {
        if (_predators.Contains(current)) return;
        _predators.Add(current);
    }

    public static void DeregisterPredator(Predator current)
    {
        if (!_predators.Contains(current)) return;
        _predators.Remove(current);
    }

    public static void RegisterFood(Food current)
    {
        if (_foods.Contains(current)) return;
        _foods.Add(current);
        if (OnFoodsChanged != null) OnFoodsChanged.Invoke();
    }

    public static void DeregisterFood(Food current)
    {
        if (!_foods.Contains(current)) return;
        _foods.Remove(current);
        if (OnFoodsChanged != null) OnFoodsChanged.Invoke();
    }

    public static void Init()
    {
        _boids.Clear();
        _obstacles.Clear();
    }
}
