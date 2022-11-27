using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] bool useJobs;

    [Space]
    [Space]

    [SerializeField] Slider loadingBar;

    [Space]
    [Space]

    [SerializeField] bool runBenchmark;
    [SerializeField] bool debugBenchmark;
    [SerializeField] bool debugDetail;
    [SerializeField] float metricsLatency = 0.1f;
    [SerializeField] float benchmarkStartDelay = .2f;
    [SerializeField] float benchmarkTime = 4f;
    [SerializeField] float degradeLODFPSThreshold = 45f;
    [SerializeField] float degradeLODLatency = 0.2f;
    [SerializeField] int startLOD = 10;

    GameState _state;

    int currentLOD;

    public bool UseJobs { get => useJobs; }
    public GameState state { get => _state; }

    public void SetCohesion(float val)
    {
        state.cohesion = val;
    }

    public void SetSeparation(float val)
    {
        state.separation = val;
    }

    public void SetAlignment(float val)
    {
        state.alignment = val;
    }

    public void SetAvoidPredators(float val)
    {
        state.avoidPredators = val;
    }

    public void SetSeekFood(float val)
    {
        state.seekFood = val;
    }

    void Awake()
    {
        _state = new GameState();
        CameraUtils.InvalidateCameraCache();
        Debug.Log($"Current LOD: {Perf.LOD}");

        if (runBenchmark)
        {
            Perf.Init(startLOD);
            Simulation.SetSimulationSpeed(1f);
        }
        else
        {
            Simulation.SetSimulationSpeed(0f);
        }
    }

    void Start()
    {
        if (runBenchmark)
        {
            StartCoroutine(RunBenchmark());
        }
    }

    void Update()
    {
        currentLOD = Perf.LOD;
    }

    void PopulateFrameTimings()
    {
        if (!runBenchmark) return;
        Perf.deltaTimeThisFrame = Time.smoothDeltaTime;
        for (int i = Perf.frameTimings.Length - 2; i >= 0; i--)
        {
            Perf.frameTimings[i + 1] = Perf.frameTimings[i];
        }
        Perf.frameTimings[0].deltaTime = Perf.deltaTimeThisFrame;
    }

    void CalcFrameTiming()
    {
        if (!runBenchmark) return;
        Perf.minDeltaTime = float.MaxValue;
        Perf.maxDeltaTime = 0f;
        Perf.totalDeltaTime = 0f;
        Perf.numFramesRecorded = 0;
        for (int i = 0; i < Perf.frameTimings.Length; i++)
        {
            if (Perf.frameTimings[i].deltaTime == 0f) continue;
            Perf.numFramesRecorded++;
            Perf.totalDeltaTime += Perf.frameTimings[i].deltaTime;
            if (Perf.frameTimings[i].deltaTime > Perf.maxDeltaTime) Perf.maxDeltaTime = Perf.frameTimings[i].deltaTime;
            if (Perf.frameTimings[i].deltaTime < Perf.minDeltaTime) Perf.minDeltaTime = Perf.frameTimings[i].deltaTime;
        }
        Perf.avgDeltaTime = Perf.numFramesRecorded > 0 ? Perf.totalDeltaTime / Perf.numFramesRecorded : 0;
        Perf.avgFPS = toFPS(Perf.avgDeltaTime);
        Perf.maxFPS = toFPS(Perf.minDeltaTime);
        Perf.minFPS = toFPS(Perf.maxDeltaTime);
    }

    void CheckLOD()
    {
        if (!runBenchmark) return;
        if (Perf.avgDeltaTime == 0f) return;
        if (Perf.avgFPS >= degradeLODFPSThreshold)
        {
            Perf.timeBelowThreshold = 0f;
            return;
        }
        if (Perf.timeBelowThreshold < degradeLODLatency) return;

        Perf.timeBelowThreshold = 0f;
        Perf.LOD--;
        Debug.Log($">>> Degrading LOD to {Perf.LOD}");
        GlobalEvent.Invoke(GlobalEvent.type.DEGRADE_LOD);
    }

    void DebugFrameTiming()
    {
        if (!runBenchmark) return;
        if (!debugBenchmark) return;
        string str = "";
        str += $"AVG={Perf.avgFPS} FPS ({Perf.avgDeltaTime}s) \n";
        str += $"MAX={Perf.maxFPS} FPS ({Perf.minDeltaTime}s) \n";
        str += $"MIN={Perf.minFPS} FPS ({Perf.maxDeltaTime}s) \n";
        if (debugDetail)
        {
            for (int i = 0; i < Perf.frameTimings.Length; i++)
            {
                str += $"F{i}={toFPS(Perf.frameTimings[i].deltaTime)} FPS ({Perf.frameTimings[i].deltaTime}s) \n";
            }
        }
        Debug.Log(str);
    }

    float toFPS(float time)
    {
        if (time == 0) return 0;
        return Mathf.Round((1f / time) * 10f) * 0.1f;
    }

    void LoadMainScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    IEnumerator RunBenchmark()
    {
        float t = 0f;
        while (t < benchmarkTime)
        {
            PopulateFrameTimings();
            if (Perf.timeElapsedSinceLastMetric >= metricsLatency)
            {
                CalcFrameTiming();
                DebugFrameTiming();
                if (t >= benchmarkStartDelay) CheckLOD();
                Perf.timeElapsedSinceLastMetric = 0f;
            }
            else
            {
                Perf.timeElapsedSinceLastMetric += Time.deltaTime;
            }

            if (Perf.LOD == 0)
            {
                LoadMainScene();
                yield break;
            }

            t += Time.unscaledDeltaTime;
            Perf.timeBelowThreshold += Time.unscaledDeltaTime;

            loadingBar.value = benchmarkTime > 0 ? (t / benchmarkTime) : 0;

            yield return new WaitForEndOfFrame();
        }

        LoadMainScene();
    }
}
