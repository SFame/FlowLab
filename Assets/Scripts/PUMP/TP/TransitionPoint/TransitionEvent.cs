using System;

public class TransitionEventArgs : EventArgs
{
    public TransitionEventArgs(int index, bool state, bool isStateChange)
    {
        Index = index;
        State = state;
        IsStateChange = isStateChange;
    }

    /// <summary>
    /// 변경된 TP의 Index
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 현재 TP의 State
    /// </summary>
    public bool State { get; }

    /// <summary>
    /// 이전 값과 비교해서 변경되었는지
    /// </summary>
    public bool IsStateChange { get; }

    public override string ToString() => $"Index: [{Index}]\nState: [{State}]\nIsStateChange: [{IsStateChange}]";
}

public delegate void StateChangeEventHandler(TransitionEventArgs args);