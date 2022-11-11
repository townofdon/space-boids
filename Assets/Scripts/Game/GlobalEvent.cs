using System;
using UnityEngine;

public static class GlobalEvent
{
    public enum type
    {
        SIMULATION_START,
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
