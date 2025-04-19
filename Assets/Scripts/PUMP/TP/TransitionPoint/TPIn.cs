using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(Image))]
public class TPIn : TransitionPoint, ITPIn, ISoundable, IDeserializingListenable
{
    #region Privates
    [SerializeField]
    private bool _state;
    private LineConnector _lineConnector;

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        OnMove = uguiPos => OnNodeMove(lineConnector);   // 커넥션 제거 시 구독 해제를 위해 Action에 할당
        Node.OnPositionUpdate += OnMove;

        tpConnection.LineConnector = lineConnector;
        return tpConnection;
    }

    private void OnNodeMove(LineConnector lineConnector)
    {
        lineConnector.EndSidePoint = Location;
    }

    private void SetConnectionLineHideMode(bool isHide)
    {
        if (Connection is null)
            return;
        if (Connection.LineConnector is null)
            return;

        if (isHide)
        {
            Connection.LineConnector.SetAlpha(0.1f);
            Connection.LineConnector.FreezeLinesAttributes = true;
        }
        else
        {
            Connection.LineConnector.FreezeLinesAttributes = false;
            Connection.LineConnector.SetAlpha(1f);
        }
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
                OnStateChange?.Invoke(new TransitionEventArgs(Index, value, isStateChange));
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

            OnMove = uguiPos => OnNodeMove(connection.LineConnector);
            Node.OnPositionUpdate += OnMove;

            if (!OnDeserializing)
            {
                OnSounded?.Invoke(this, new(0, Location));
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
        Node.OnPositionUpdate -= OnMove;
        OnMove = null;

        if (!OnDeserializing)
        {
            OnSounded?.Invoke(this, new(1, Location));
        }
    }
    #endregion

    #region MouseEvent
    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
            _lineConnector.Remove();

        _lineConnector = Node.Background.LineConnectManager.AddLineConnector();
        _lineConnector.Initialize(Location, Location);
        _lineConnector.FreezeLinesAttributes = true;

        SetConnectionLineHideMode(true);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
        {
            Vector2 targetPoint = eventData.position;

            ITPOut target = eventData.FindUnderPoint<ITPOut>();
            if (target is not null)
                targetPoint = target.Location;

            _lineConnector.EndSidePoint = targetPoint;
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (_lineConnector != null)
        {
            _lineConnector.Remove();
            _lineConnector = null;
        }

        SetConnectionLineHideMode(false);

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
