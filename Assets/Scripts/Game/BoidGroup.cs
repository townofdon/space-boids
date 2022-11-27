using UnityEngine;

public class BoidGroup : MonoBehaviour
{
    [SerializeField] int groupLOD = 5;

    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void Start()
    {
        CheckLOD();
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        switch (eventType)
        {
            case GlobalEvent.type.DEGRADE_LOD:
                CheckLOD();
                break;
        }
    }

    void CheckLOD()
    {
        if (groupLOD > Perf.LOD) DisableGroup();
    }

    void DisableGroup()
    {
        gameObject.SetActive(false);
    }
}
