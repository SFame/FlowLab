using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class TPOut : TransitionPoint, ITPOut, ISoundable, IDraggable, ITPHideable
{
    #region Privates
    private Transition _state;
    private TransitionType _type;
    private LineConnector _lineConnector;
    private ITPHideable _hideTargetTpCache;
    private HashSet<object> _hiders = new();
    private readonly object _hider = new();

    private bool OnDeserializing => Node?.OnDeserializing ?? false;

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        OnMove = _ => OnNodeMove(lineConnector);
        Node.Support.OnPositionUpdate += OnMove;

        tpConnection.LineConnector = lineConnector;
        return tpConnection;
    }

    private async UniTaskVoid PushToConnectionAsync(UniTask task)
    {
        try
        {
            await task;

            if (Connection != null)
            {
                Connection.State = State;
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnNodeMove(LineConnector lineConnector)
    {
        lineConnector.StartSidePoint = WorldPosition;
    }

    private void SetHide(bool isHide)
    {
        if (Connection is null)
            return;
        if (Connection.LineConnector is null)
            return;

        if (isHide)
        {
            Connection.LineConnector.SetAlpha(0.5f);
            Connection.LineConnector.FreezeLinesAttributes = true;
        }
        else
        {
            Connection.LineConnector.FreezeLinesAttributes = false;
            Connection.LineConnector.SetAlpha(1f);
        }
    }

    private void CheckHide()
    {
        SetHide(_hiders.Count > 0);
    }
    #endregion

    #region Interface
    public event SoundEventHandler OnSounded;

    public override Transition State
    {
        get => _state;
        set
        {
            value.ThrowIfTypeMismatch(Type);

            _state = value;
            PushToConnection(UniTask.CompletedTask);

            SetImageColor(_state.IsNull ? m_DefaultColor : m_StateActiveColor);
            ShowRadial(_state);
        }
    }

    public override TransitionType Type
    {
        get => _type;
        protected set
        {
            Connection?.Disconnect();

            _type = value;
            _state = _type.Null();

            Color typeColor = _type.GetColor();
            SetTextColor(typeColor);
            SetTypeRingColor(typeColor);
            RadialDefaultColorUpdate(typeColor);
            SetImageColor(_state.IsNull ? m_DefaultColor : m_StateActiveColor);
        }
    }

    public bool IsStatePending => Connection?.IsFlushing ?? false;

    public void PushToConnection(UniTask delayTask)
    {
        PushToConnectionAsync(delayTask).Forget();
    }

    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        if (targetTp.Type != Type)
            return;

        Connection?.Disconnect();

        connection ??= new();

        connection = SetTPConnectionLineConnector(connection);
        connection.SourceState = this;
        Connection = connection;

        targetTp.AcceptLink(connection);
    }

    public override void AcceptLink(TPConnection connection)
    {
        Connection?.Disconnect();

        try
        {
            connection.SourceState = this;
        }
        catch (TransitionException te)
        {
            Debug.LogWarning(te.Message);
            return;
        }

        if (BlockConnect)
        {
            connection.Disconnect(); // 커넥션 블로킹 상태면 바로 Disconnect
            return;
        }

        Connection = connection;

        OnMove = _ => OnNodeMove(Connection.LineConnector);
        Connection.OnSelfDisconnect += Node.ReportChanges;
        Node.Support.OnPositionUpdate += OnMove;

        if (!OnDeserializing)
        {
            OnSounded?.Invoke(this, new SoundEventArgs(0, WorldPosition));
        }
    }

    public override void ClearConnection()
    {
        Connection = null;
        Node.Support.OnPositionUpdate -= OnMove;
        OnMove = null;

        if (!OnDeserializing)
        {
            OnSounded?.Invoke(this, new(1, WorldPosition));
        }
    }

    public void AddHider(object hider)
    {
        if (_hiders.Add(hider))
        {
            CheckHide();
        }
    }

    public void SubHider(object hider)
    {
        if (_hiders.Remove(hider))
        {
            CheckHide();
        }
    }
    #endregion

    #region MouseEvent
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
            _lineConnector.Remove();

        _lineConnector = Node.Background.LineConnectManager.AddLineConnector();
        _lineConnector.Initialize(WorldPosition, WorldPosition);
        _lineConnector.FreezeLinesAttributes = true;

        _hiders.Clear();
        AddHider(_hider);
        _hideTargetTpCache = null;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
        {
            Vector2 targetPoint;
            ITPIn target = eventData.FindUnderPoint<ITPIn>();

            if (target != null && target.Type == Type)
            {
                targetPoint = target.WorldPosition;

                if (target is ITPHideable hideable && _hideTargetTpCache == null)
                {
                    _hideTargetTpCache = hideable;
                    _hideTargetTpCache.AddHider(_hider);
                }
            }
            else
            {
                targetPoint = eventData.position.ScreenToWorldPoint();
                if (_hideTargetTpCache != null)
                {
                    _hideTargetTpCache.SubHider(_hider);
                    CheckHide();
                    _hideTargetTpCache = null;
                }
            }

            _lineConnector.EndSidePoint = targetPoint;
        }
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
        {
            _lineConnector.Remove();
            _lineConnector = null;
        }

        SubHider(_hider);
        _hideTargetTpCache?.SubHider(_hider);
        _hideTargetTpCache = null;

        ITPIn find = eventData.FindUnderPoint<ITPIn>();
        if (find != null && find.Type == Type)
        {
            Node.ReportChanges();
            LinkTo(find);
        }
        else
        {
            if (Connection is not null)
                Node.ReportChanges();
            
            Connection?.Disconnect();
        }
    }
    #endregion
}