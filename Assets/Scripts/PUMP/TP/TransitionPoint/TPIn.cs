using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(Image))]
public class TPIn : TransitionPoint, ITPIn, ISoundable, IDeserializingListenable, IDraggable, ITPHideable
{
    #region Privates
    private Transition _state;
    private TransitionType _type;
    private LineConnector _lineConnector;
    private ITPHideable _hideTargetTpCache;
    private HashSet<object> _hiders = new();
    private readonly object _hider = new();

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        OnMove = _ => OnNodeMove(lineConnector);   // 커넥션 제거 시 구독 해제를 위해 Action에 할당
        Node.Support.OnPositionUpdate += OnMove;

        tpConnection.LineConnector = lineConnector;
        return tpConnection;
    }

    private void OnNodeMove(LineConnector lineConnector)
    {
        lineConnector.EndSidePoint = WorldPosition;
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
    public override Transition State
    {
        get => _state;
        set
        {
            value.ThrowIfTypeMismatch(Type);

            Transition beforeState = _state;
            bool isStateChange = !beforeState.Equals(value);
            _state = value;

            SetImageColor(_state.IsNull ? m_DefaultColor: m_StateActiveColor);

            if (!OnDeserializing)
            {
                ShowRadial(_state);
                TransitionEventArgs args = TransitionEventArgs.Get(Index, value, beforeState, isStateChange);
                OnStateChange?.Invoke(args);
                TransitionEventArgs.Release(args);
            }
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

            SetTextColor(_type.GetColor());
            SetImageColor(_state.IsNull ? m_DefaultColor : m_StateActiveColor);
        }
    }

    public bool OnDeserializing { get; set; }

    public event StateChangeEventHandler OnStateChange;
    public event SoundEventHandler OnSounded;

    public override void AcceptLink(TPConnection connection)
    {
        Connection?.Disconnect();

        try
        {
            connection.TargetState = this;
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
            OnSounded?.Invoke(this, new(0, WorldPosition));
        }

    }

    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        if (targetTp.Type != Type)
            return;

        Connection?.Disconnect();

        connection ??= new();

        connection = SetTPConnectionLineConnector(connection);
        connection.TargetState = this;
        Connection = connection;

        targetTp.AcceptLink(connection);
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
            ITPOut target = eventData.FindUnderPoint<ITPOut>();

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
                targetPoint = eventData.position;
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

        ITPOut find = eventData.FindUnderPoint<ITPOut>();
        if (find != null && find.Type == Type)
        {
            LinkTo(find);
            Node.ReportChanges();
        }
        else
        {
            if (Connection != null)
                Node.ReportChanges();
            
            Connection?.Disconnect();
        }
    }
    #endregion
}