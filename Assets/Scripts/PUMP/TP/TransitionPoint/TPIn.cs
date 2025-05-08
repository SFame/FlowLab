using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(Image))]
public class TPIn : TransitionPoint, ITPIn, ISoundable, IDeserializingListenable, IDraggable, ITPHideable
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

        OnMove = uguiPos => OnNodeMove(lineConnector);   // 커넥션 제거 시 구독 해제를 위해 Action에 할당
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
    public override bool State
    {
        get => _state;
        set
        {
            bool isStateChange = _state != value;
            _state = value;
            SetImageColor(_state ? _activeColor : _defaultColor);
            if (!OnDeserializing)
            {
                TransitionEventArgs args = TransitionEventArgs.Get(Index, value, isStateChange);
                OnStateChange?.Invoke(args);
                TransitionEventArgs.Release(args);
            }
        }
    }

    public bool OnDeserializing { get; set; }

    public event StateChangeEventHandler OnStateChange;
    public event SoundEventHandler OnSounded;

    public override void AcceptLink(TPConnection connection)
    {
        Connection?.Disconnect();

        connection.TargetState = this;

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
    
    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        Connection?.Disconnect();

        if (connection is null)
            connection = new();

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

        ITPOut find = eventData.FindUnderPoint<ITPOut>();
        if (find is not null)
        {
            LinkTo(find);
            Node.ReportChanges();
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
