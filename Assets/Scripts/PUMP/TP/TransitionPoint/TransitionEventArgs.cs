using System;
using UnityEngine;

public class TransitionEventArgs : EventArgs
{
    #region Interface
    /// <summary>
    /// 변경된 TP의 Index
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// 현재 TP의 State
    /// </summary>
    public Transition State { get; private set; }

    /// <summary>
    /// 변경 이전 TP의 State
    /// </summary>
    public Transition BeforeState { get; private set; }

    /// <summary>
    /// 이전 값과 비교해서 변경되었는지
    /// </summary>
    public bool IsStateChange { get; private set; }

    /// <summary>
    /// 현재 State의 Type (편의성)
    /// </summary>
    public TransitionType Type => State.Type;

    /// <summary>
    /// 현재 State가 Null Value 인지 (편의성)
    /// </summary>
    public bool IsNull => State.IsNull;
    #endregion

    #region Privates
    private TransitionEventArgs() { }

    private static readonly Pool<TransitionEventArgs> _pool = new
    (
        createFunc: () => new(),
        initSize: 100,
        maxSize: 50000,
        actionOnGet:ResetArgs,
        actionOnRelease: ResetArgs
    );

    private static void ResetArgs(TransitionEventArgs args)
    {
        args.Index = -1;
        args.State = false;
        args.IsStateChange = false;
    }
    #endregion

    #region Static Instantiator
    public static TransitionEventArgs Get(int index, Transition state, Transition beforeState, bool isStateChange)
    {
        TransitionEventArgs args = _pool.Get();
        args.Index = index;
        args.State = state;
        args.BeforeState = beforeState;
        args.IsStateChange = isStateChange;
        return args;
    }

    public static void Release(TransitionEventArgs args)
    {
        _pool.Release(args);
    }
    #endregion

    public override string ToString() => $"Index: [{Index}]\nState: [{State}]\nIsStateChange: [{IsStateChange}]";
}

public delegate void StateChangeEventHandler(TransitionEventArgs args);