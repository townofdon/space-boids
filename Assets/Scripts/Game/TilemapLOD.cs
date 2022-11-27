using UnityEngine;
using TilemapShadowCaster.Runtime;

public class TilemapLOD : MonoBehaviour
{
    [SerializeField] int shadowLOD = 7;

    TilemapShadowCaster2D shadowCaster;

    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void Awake()
    {
        shadowCaster = GetComponent<TilemapShadowCaster2D>();
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
        if (shadowLOD > Perf.LOD) TurnOffShadows();
    }

    void TurnOffShadows()
    {
        shadowCaster.enabled = false;
    }
}
