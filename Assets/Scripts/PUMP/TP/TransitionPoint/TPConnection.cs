using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

public class TPConnection : IStateful, IDisposable
{
    #region Privates
    private Transition _state;
    private Transition _stateCache;
    private TransitionType _type;
    private ITransitionPoint _sourceState = null;
    private ITransitionPoint _targetState = null;
    private LineConnector _lineConnector = null;
    private bool _initialized = false;
    private List<Vector2> _lineEdges;
    private readonly SafetyCancellationTokenSource _pendingCts = new();
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
            value.ThrowIfTypeMismatch(Type);

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
            _state = _type.Null();
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
        if (_disconnected)
            return;

        _disconnected = true;

        if (SourceState == null || TargetState == null)
        {
            Debug.Log($"{GetType().Name}: SourceState or TargetState is null");
            return;
        }

        SourceState.ClearConnection();
        TargetState.ClearConnection();

        TargetState.State = Type.Null();

        LineConnector?.Remove();
        _sourceState = null;
        _targetState = null;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _pendingCts.CancelAndDispose();
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

        LineConnector.Initialize(LineEdges, Type.GetColor());
        LineConnector.ContextElements = ContextElements;
    }

    private async UniTaskVoid TargetStateUpdateAsync()
    {
        IsFlushing = true;

        try
        {
            ConnectionAwaiter awaiter = ConnectionAwaitManager.GetAwaiter(this, _pendingCts.Token);
            await awaiter.Task;

            if (TargetState is not null && !_pendingCts.Token.IsCancellationRequested)
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

            TargetState.State = Type.Null();

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

public static class ConnectionAwaitManager
{
    #region Privates
    private const float MAX_WAIT_TIME = 10f;
    private const int MAX_LOOP_THRESHOLD = 20;

    private static float _waitTime = 0.5f;
    private static int _loopThreshold = 2;
    private static bool _hasGetSetting = false;
    private static bool _clearRequested = false;

    private static readonly Dictionary<TPConnection, int> _propagateCountDict = new();

    private static async UniTaskVoid DelayClearDictionary()
    {
        if (_clearRequested)
            return;

        _clearRequested = true;

        await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate);
        _propagateCountDict.Clear();

        _clearRequested = false;
    }

    private static void SetConnectionAwait()
    {
        float simulationSpeed = Setting.SimulationSpeed;
        if (Mathf.Approximately(simulationSpeed, 0f))
        {
            AwaitType = ConnectionAwait.Frame;
            WaitTime = Time.deltaTime;
            return;
        }

        AwaitType = ConnectionAwait.FixedTime;
        WaitTime = simulationSpeed;
    }

    private static ConnectionAwaiter GetImmediatelyAwaiter(TPConnection caller, CancellationToken token)
    {
        if (!_propagateCountDict.TryAdd(caller, 1))
        {
            _propagateCountDict[caller]++;
        }

        DelayClearDictionary().Forget();

        if (_propagateCountDict[caller] >= LoopThreshold)
        {
            _propagateCountDict.Remove(caller);
            OnLoopDetected?.Invoke(caller);
            return new ConnectionAwaiter(UniTask.Yield(PlayerLoopTiming.Update, token), true);
        }

        return new ConnectionAwaiter(UniTask.CompletedTask, false);
    }
    #endregion

    #region Interfaces
    public static ConnectionAwaiter GetAwaiter(TPConnection caller, CancellationToken token)
    {
        if (!_hasGetSetting)
        {
            Setting.OnSettingUpdated += SetConnectionAwait;
            SetConnectionAwait();
            _hasGetSetting = true;
        }

        return AwaitType switch
        {
            ConnectionAwait.Frame => new ConnectionAwaiter(UniTask.Yield(PlayerLoopTiming.Update, token), false),
            ConnectionAwait.FixedTime => new ConnectionAwaiter(UniTask.WaitForSeconds
            (
                duration: WaitTime,
                ignoreTimeScale: false,
                delayTiming: PlayerLoopTiming.Update,
                cancellationToken: token
            ), false),
            ConnectionAwait.Immediately => GetImmediatelyAwaiter(caller, token),
            _ => new ConnectionAwaiter(UniTask.Yield(PlayerLoopTiming.Update, token), false),
        };
    }

    public static event Action<TPConnection> OnImmediatelyLoopDetected;

    public static float WaitTime
    {
        get => _waitTime;
        set => _waitTime = Mathf.Clamp(value, Time.deltaTime, MAX_WAIT_TIME);
    }

    public static int LoopThreshold
    {
        get => _loopThreshold;
        set => _loopThreshold = value.Clamp(2, MAX_LOOP_THRESHOLD);
    }

    public static ConnectionAwait AwaitType { get; set; } = ConnectionAwait.Frame;
    #endregion
}

public struct ConnectionAwaiter
{
    public ConnectionAwaiter(UniTask task, bool isLooped)
    {
        Task = task;
        IsLooped = isLooped;
    }

    public UniTask Task;
    public bool IsLooped;
}

public enum ConnectionAwait
{
    Frame,
    FixedTime,
    Immediately
}