using System;
using UnityEngine;

public static class GlobalEvent
{
    public enum type
    {
        SIMULATION_START,
        ACQUIRE_BLUE_POWER_TANK,
        ACQUIRE_YELLOW_POWER_TANK,
        ACQUIRE_RED_POWER_TANK,
        SELECT_BLUE_POWER_TANK,
        SELECT_YELLOW_POWER_TANK,
        SELECT_RED_POWER_TANK,
        OPEN_MENU,
        CLOSE_MENU,
        DEGRADE_LOD,
        PAUSE,
        UNPAUSE,
        MUFFLE_AUDIO,
        UNMUFFLE_AUDIO,
    }

    static Action<type> OnGlobalEvent;

    public static void Invoke(type eventType)
    {
        OnGlobalEvent?.Invoke(eventType);
    }

    public static void Subscribe(Action<type> action)
    {
        OnGlobalEvent += action;
    }

    public static void Unsubscribe(Action<type> action)
    {
        OnGlobalEvent -= action;
    }
}
