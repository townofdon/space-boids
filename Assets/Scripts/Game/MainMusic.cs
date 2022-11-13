using UnityEngine;
using FMODUnity;

public class MainMusic : MonoBehaviour
{
    const string TRIGGER_MENU_OPEN = "MenuOpen";

    StudioEventEmitter emitter;

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
        emitter = GetComponent<FMODUnity.StudioEventEmitter>();
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        switch (eventType)
        {
            case GlobalEvent.type.OPEN_MENU:
                emitter.SetParameter(TRIGGER_MENU_OPEN, 1f);
                break;
            case GlobalEvent.type.CLOSE_MENU:
                emitter.SetParameter(TRIGGER_MENU_OPEN, 0f);
                break;
        }
    }
}
