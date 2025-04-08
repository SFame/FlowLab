using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

public class TPConnection : IStateful, IDisposable
{
    #region Privates
    private bool _state = false;
    private bool _stateChache = false;
    private ITransitionPoint _sourceState = null;
    private ITransitionPoint _targetState = null;
    private LineConnector _lineConnector = null;
    private bool _initialized = false;
    private List<Vector2> _lineEdges;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private UniTask _targetStateUpdateTask = UniTask.CompletedTask;
    private bool _disposed = false;
    
    private void InitializCheck()
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

    private List<ContextElement> ContextElements
    {
        get
        {
            return new List<ContextElement>()
            {
                new ContextElement("Disconnect", Disconnect),
            };
        }
    }
    #endregion

    #region Interface
    public bool State
    {
        get => _state;
        set
        {
            _stateChache = value;
            if (TargetState is not null && _targetStateUpdateTask.Status != UniTaskStatus.Pending)
            {
                _targetStateUpdateTask = TargetStateUpdateAsync();
            }
        }
    }

    public ITransitionPoint SourceState
    {
        get => _sourceState;
        set
        {
            if (_sourceState is null)
            {
                _sourceState = value;
                InitializCheck();
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
                _targetState = value;
                InitializCheck();
            }
        }
    }

    /// <summary>
    /// 외부에서 할당
    /// </summary>
    public LineConnector LineConnector
    {
        get => _lineConnector;
        set
        {
            if (_lineConnector is null)
                _lineConnector = value;
        }
    }
    
    public List<Vector2> LineEdges
    {
        get
        {
            if (_lineEdges is null) // 라인 간선 추가하지 않으면 양 노드 Location 직선으로 이을 수 있도록
            {
                return new() { SourceState.Location, TargetState.Location };
            }
            
            if (_lineEdges.Count < 2) // Invalid
            {
                _lineEdges = null;
                return new() { SourceState.Location, TargetState.Location };
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

    public void Disconnect()
    {
        if (SourceState == null || TargetState == null)
        {
            Debug.Log($"{GetType().Name}: SourceState or TargetState is null");
            return;
        }

        SourceState.ClearConnection();
        TargetState.ClearConnection();

        TargetState.State = false;

        LineConnector?.Remove();
        _sourceState = null;
        _targetState = null;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _cts.Cancel();
        _cts.Dispose();
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

    private async UniTask TargetStateUpdateAsync()
    {      
        try
        {
            await UniTask.WaitForEndOfFrame(_cts.Token);

            if (TargetState is not null && !_cts.Token.IsCancellationRequested)
            {
                _state = _stateChache;
                TargetState.State = _stateChache;
            }
        }
        catch (OperationCanceledException) { }
    }
    #endregion
}