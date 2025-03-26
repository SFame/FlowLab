using System;
using UnityEngine;
using System.Collections.Generic;

public static class GlobalEventManager
{
    public static Action GameStartEvent;
    public static Action GameExitEvent;

    public static void OnGameStartEvent() => GameStartEvent?.Invoke();
    public static void OnGameExitEvent() => GameExitEvent?.Invoke();
}
