using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class TPOut : TransitionPoint, ITPOut, ISoundable, IDeserializingListenable, IDraggable, ITPHideable
{
    #region Privates
    [SerializeField]
    private bool _state;
    private LineConnector _lineConnector;
    private ITPHideable _hideTargetTpCache;
    private HashSet<object> _hiders = new();
    private readonly object _hider = new();

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        OnMove = uguiPos => OnNodeMove(lineConnector);
        Node.Support.OnPositionUpdate += OnMove;

        tpConnection.LineConnector = lineConnector;
        return tpConnection;
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

    public override bool State
    {
        get => _state;
        set
        {
            _state = value;
            PushToConnection();
            SetImageColor(_state ? _activeColor : _defaultColor);
        }
    }

    public bool IsStatePending => Connection?.IsFlushing ?? false;

    public bool OnDeserializing { get; set; }

    public void PushToConnection()
    {
        if (Connection != null)
        {
            Connection.State = State;
        }
    }

    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        Connection?.Disconnect();

        if (connection is null)
            connection = new();

        connection = SetTPConnectionLineConnector(connection);
        connection.SourceState = this;
        Connection = connection;

        targetTp.AcceptLink(connection);
    }

    public override void AcceptLink(TPConnection connection)
    {
        Connection?.Disconnect();

        connection.SourceState = this;
        if (!BlockConnect)
        {
            Connection = connection;

            OnMove = uguiPos => OnNodeMove(Connection.LineConnector);
            Connection.OnSelfDisconnect += Node.ReportChanges;
            Node.Support.OnPositionUpdate += OnMove;

            if (!OnDeserializing)
            {
                OnSounded?.Invoke(this, new(0, WorldPosition));
            }
            return;
        }

        connection.Disconnect(); // 커넥션 블로킹 상태면 바로 Disconnect
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

            if (target != null)
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

        ITPIn find = eventData.FindUnderPoint<ITPIn>();
        if (find is not null)
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