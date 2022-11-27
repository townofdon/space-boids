public static class Perf
{
    public static void Init(int startLOD)
    {
        Perf.frameTimings = new FrameTiming[10];
        Perf.timeElapsedSinceLastMetric = float.MaxValue;
        Perf.LOD = startLOD;
    }

    public static FrameTiming[] frameTimings = new FrameTiming[10];
    public static float timeElapsedSinceLastMetric = float.MaxValue;
    public static float deltaTimeThisFrame;
    public static float totalDeltaTime;
    public static int numFramesRecorded;
    public static float maxDeltaTime;
    public static float minDeltaTime;
    public static float avgDeltaTime;
    public static float maxFPS;
    public static float minFPS;
    public static float avgFPS;

    public static float timeBelowThreshold;

    public static int LOD;
}

public struct FrameTiming
{
    public bool wasRecorded;
    public float deltaTime;
}
