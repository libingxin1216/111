using System;
using System.Collections.Generic;

public static class EventBus
{
    private static Dictionary<string, Action<object>> listeners = new();

    public static void On(string eventName, Action<object> callback)
    {
        if (!listeners.ContainsKey(eventName))
            listeners[eventName] = null;
        listeners[eventName] += callback;
    }

    public static void Off(string eventName, Action<object> callback)
    {
        if (listeners.ContainsKey(eventName))
            listeners[eventName] -= callback;
    }

    public static void Emit(string eventName, object data = null)
    {
        if (listeners.ContainsKey(eventName))
            listeners[eventName]?.Invoke(data);
    }
}