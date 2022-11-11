using UnityEngine;

public static class Utils
{
    private static ScreenStats _screenStats;

    public static ScreenStats GetScreenStats()
    {
        if (_screenStats == null) _screenStats = new ScreenStats();
        return _screenStats;
    }

    public static void InvalidateScreenStats()
    {
        _screenStats = null;
    }

    public static bool IsOdd(int val)
    {
        return val % 2 == 1 || val % 2 == -1;
    }
}

public static class CameraUtils
{
    static Camera _camera;

    public static Camera GetMainCamera()
    {
        if (_camera != null) return _camera;
        return Camera.main;
    }
}

public class ScreenStats
{
    public Vector2 lowerLeftBounds;
    public Vector2 upperRightBounds;
    public float screenWidth;
    public float screenHeight;

    public ScreenStats()
    {
        lowerLeftBounds = CameraUtils.GetMainCamera().ViewportToWorldPoint(Vector2.zero);
        upperRightBounds = CameraUtils.GetMainCamera().ViewportToWorldPoint(Vector2.one);
        screenWidth = upperRightBounds.x - lowerLeftBounds.x;
        screenHeight = upperRightBounds.y - lowerLeftBounds.y;
    }
}
