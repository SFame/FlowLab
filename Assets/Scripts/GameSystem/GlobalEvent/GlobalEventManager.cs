using System;

public static class GlobalEventManager
{
    public static Action GameStartEvent;
    public static Action GameExitEvent;

    public static Action StageStartEvent;
    public static Action<StageData> StageClearEvent;
    public static Action StageClearEventForNode;

    public static void OnGameStartEvent() => GameStartEvent?.Invoke();
    public static void OnGameExitEvent() => GameExitEvent?.Invoke();

    public static void OnStageStartEvent() =>StageStartEvent?.Invoke();
    public static void OnStageExitEvent(StageData stageData) => StageClearEvent?.Invoke(stageData);
    public static void OnStageClearEvent() => StageClearEventForNode?.Invoke();
}
