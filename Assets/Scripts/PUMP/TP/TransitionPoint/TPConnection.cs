using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TPConnection : IStateful, IDisposable
{
    #region Privates
    private static float _waitTime = 0.5f;
    private const float MAX_WAIT_TIME = 10f;

    private static UniTask GetStateUpdateTask(CancellationToken token) => AwaitType switch
    {
        ConnectionAwait.Frame => UniTask.Yield(PlayerLoopTiming.Update, token),
        ConnectionAwait.FixedTime => UniTask.WaitForSeconds
        (
            duration: WaitTime,
            ignoreTimeScale: false,
            delayTiming: PlayerLoopTiming.LastPostLateUpdate,
            cancellationToken: token
        ),
        _ => UniTask.WaitForEndOfFrame(token),
    };
    #endregion

    #region Static Interface
    public static float WaitTime
    {
        get => _waitTime;
        set
        {
            float waitTime = Mathf.Clamp(value, Time.deltaTime, MAX_WAIT_TIME);
            _waitTime = waitTime;
        }
    }

    public static ConnectionAwait AwaitType { get; set; } = ConnectionAwait.Frame;
    #endregion


    #region Privates
    private Transition _state;
    private Transition _stateCache;
    private TransitionType _type;
    private ITransitionPoint _sourceState = null;
    private ITransitionPoint _targetState = null;
    private LineConnector _lineConnector = null;
    private bool _initialized = false;
    private List<Vector2> _lineEdges;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _typeSet = false;
    private bool _disposed = false;
    private bool _disconnected = false;
    
    private void InitializeCheck()
    {
        if (!_initialized)
        {
            if (SourceState != null && TargetState != null)
            {
                State = SourceState.State;

                DrawLine();
                _initialized = true;
            }
        }
    }

    private List<ContextElement> ContextElements =>
        new()
        {
            new("Disconnect", () =>
            {
                Disconnect();
                OnSelfDisconnect?.Invoke();
            }),
        };

    #endregion

    #region Interface
    public Transition State
    {
        get => _state;
        set
        {
            if (value.Type != Type)
            {
                throw new TransitionTypeMismatchException(value.Type, Type);
            }

            if (DisableFlush)
            {
                _state = value;
                return;
            }

            _stateCache = value;
            if (TargetState is not null && !IsFlushing)
            {
                TargetStateUpdateAsync().Forget();
            }
        }
    }

    public TransitionType Type
    {
        get => _type;
        private set
        {
            if (_typeSet)
            {
                return;
            }
            _typeSet = true;
            _type = value;
            _state = Transition.Null(_type);
        }
    }

    public bool DisableFlush { get; set; }

    public bool IsFlushing { get; private set; }

    public ITransitionPoint SourceState
    {
        get => _sourceState;
        set
        {
            if (_sourceState is null)
            {
                Type = value.Type;
                ThrowIfMismatch(value.Type);
                _sourceState = value;
                InitializeCheck();
            }
        }
    }

    public ITransitionPoint TargetState
    {
        get => _targetState;
        set
        {
            if (_targetState is null)
            {
                Type = value.Type;
                ThrowIfMismatch(value.Type);
                _targetState = value;
                InitializeCheck();
            }
        }
    }

    /// <summary>
    /// 외부에서 할당
    /// </summary>
    public LineConnector LineConnector
    {
        get => _lineConnector;
        set => _lineConnector ??= value;
    }
    
    public List<Vector2> LineEdges
    {
        get
        {
            if (_lineEdges is null) // 라인 간선 추가하지 않으면 양 노드 WorldPosition 직선으로 이을 수 있도록
            {
                return new() { SourceState.WorldPosition, TargetState.WorldPosition };
            }
            
            if (_lineEdges.Count < 2) // Invalid
            {
                _lineEdges = null;
                return new() { SourceState.WorldPosition, TargetState.WorldPosition };
            }

            return _lineEdges;
        }
        set
        {
            if (value is null)
            {
                Debug.LogError($"{GetType().Name}: edges are null");
                return;
            }

            if (value.Count < 2)
            {
                Debug.LogWarning($"{GetType().Name}: Line must contain at least 2 edges");
                return;
            }

            _lineEdges = value;
        }
    }

    // LineConnector에서 ContextMenu를 통해 Disconnect 되었을 때
    public event Action OnSelfDisconnect;

    public void Disconnect()
    {
        if (SourceState == null || TargetState == null)
        {
            Debug.Log($"{GetType().Name}: SourceState or TargetState is null");
            return;
        }

        if (_disconnected)
            return;

        SourceState.ClearConnection();
        TargetState.ClearConnection();

        TargetState.State = Transition.Null(Type);

        LineConnector?.Remove();
        _sourceState = null;
        _targetState = null;
        _disconnected = true;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _cts.Cancel();
        _cts.Dispose();
        OnSelfDisconnect = null;
        _sourceState = null;
        _targetState = null;
        _disposed = true;
    }
    #endregion

    #region Privates
    private void DrawLine()
    {
        if (LineConnector is null)
        {
            Debug.LogError($"{GetType().Name}: LineConnector is null");
            return;
        }

        LineConnector.Initialize(LineEdges);
        LineConnector.ContextElements = ContextElements;
    }

    private async UniTaskVoid TargetStateUpdateAsync()
    {
        IsFlushing = true;

        try
        {
            await GetStateUpdateTask(_cts.Token);

            if (TargetState is not null && !_cts.Token.IsCancellationRequested)
            {
                _state = _stateCache;
                IsFlushing = false;
                TargetState.State = _stateCache;
            }
        }
        catch (OperationCanceledException)
        {
            IsFlushing = false;
        }
        catch
        {
            IsFlushing = false;
            throw;
        }
    }

    private void ThrowIfMismatch(TransitionType checkType)
    {
        if (checkType != Type)
        {
            SourceState.ClearConnection();
            TargetState.ClearConnection();

            TargetState.State = Transition.Null(Type);

            LineConnector?.Remove();
            _sourceState = null;
            _targetState = null;
            _disconnected = true;
            Dispose();

            throw new TransitionTypeMismatchException(checkType, Type);
        }
    }
    #endregion
}

public enum ConnectionAwait
{
    Frame,
    FixedTime
}