
using UnityEngine;

public static class BoidHelpers
{
    public static float ForwardnessTo(Vector2 origin, Vector2 target, Vector2 currentVelocity)
    {
        return Vector2.Dot(target - origin, currentVelocity);
    }

    public static Vector2 LineTo(Vector2 origin, Vector2 target)
    {
        return target - origin;
    }

    public static Vector2 HeadingTo(Vector2 origin, Vector2 target)
    {
        return LineTo(origin, target).normalized;
    }

    public static Vector2 LineFrom(Vector2 origin, Vector2 target)
    {
        return origin - target;
    }

    public static Vector2 HeadingFrom(Vector2 origin, Vector2 target)
    {
        return LineFrom(origin, target).normalized;
    }

    public static float DistanceTo(Vector2 origin, Vector2 target)
    {
        return Vector2.Distance(origin, target);
    }

    public static bool IsInRange(Vector2 origin, Vector2 target, float range)
    {
        if (Mathf.Abs(origin.x - target.x) > range) return false;
        if (Mathf.Abs(origin.y - target.y) > range) return false;
        return DistanceTo(origin, target) < range;
    }

    public static bool CanSee(Vector2 origin, Vector2 target, float sightDistance, Vector2 currentVelocity)
    {
        if (Mathf.Abs(origin.x - target.x) > sightDistance) return false;
        if (Mathf.Abs(origin.y - target.y) > sightDistance) return false;
        if (ForwardnessTo(origin, target, currentVelocity) < -0.75) return false;
        return Vector2.Distance(origin, target) <= sightDistance;
    }
}
