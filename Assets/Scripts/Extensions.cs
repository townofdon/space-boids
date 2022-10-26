using UnityEngine;

public static class Extensions
{
    public static Vector2 heading(this Rigidbody2D rb)
    {
        return rb.velocity.normalized;
    }

    public static Color toAlpha(this Color color, float alpha)
    {
        Color temp = color;
        temp.a = Mathf.Clamp01(alpha);
        return temp;
    }
}
