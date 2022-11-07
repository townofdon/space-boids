using UnityEngine;

public class MeshBackgroundEntity : MonoBehaviour
{
    [SerializeField][Range(1, 20)] int maxHistoryCount = 10;
    [SerializeField] float historyTimeStep = 0.1f;

    MeshBackground meshBackground;

    public int historyCount = -1;
    public Vector3[] history = new Vector3[20];
    int prevMaxHistoryCount;

    float timeLastStepped = float.MaxValue;

    void OnEnable()
    {
        meshBackground.RegisterEntity(this);
    }

    void OnDisable()
    {
        meshBackground.DeregisterEntity(this);
    }

    void Awake()
    {
        meshBackground = FindObjectOfType<MeshBackground>();
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        if (prevMaxHistoryCount != maxHistoryCount) Init();
        if (timeLastStepped > historyTimeStep) Step();
        timeLastStepped += Time.deltaTime;
    }

    void Init()
    {
        historyCount = 0;
        history = new Vector3[maxHistoryCount];
        prevMaxHistoryCount = maxHistoryCount;
    }

    void Step()
    {
        timeLastStepped = 0f;
        history[0] = transform.position;
        historyCount = Mathf.Min(historyCount + 1, history.Length - 1);

        for (int i = historyCount; i > 0; i--)
        {
            history[i] = history[i - 1];
        }

    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < historyCount; i++)
        {
            Gizmos.DrawCube(history[i], Vector3.one * 0.1f);
        }
    }
}