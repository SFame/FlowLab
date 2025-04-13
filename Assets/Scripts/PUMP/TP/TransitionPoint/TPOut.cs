using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class TPOut : TransitionPoint, ITPOut, ISoundable, IDeserializingListenable
{
    #region Privates
    [SerializeField]
    private bool _state;
    private LineConnector _lineConnector;

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        OnMove = uguiPos => OnNodeMove(lineConnector);
        Node.OnMove += OnMove;

        tpConnection.LineConnector = lineConnector;
        return tpConnection;
    }

    private void OnNodeMove(LineConnector lineConnector)
    {
        lineConnector.StartSidePoint = Location;
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

            OnMove = uguiPos => OnNodeMove(connection.LineConnector);
            Node.OnMove += OnMove;

            if (!OnDeserializing)
            {
                OnSounded?.Invoke(this, new(0, Location));
            }
            return;
        }

        connection.Disconnect(); // 커넥션 블로킹 상태면 바로 Disconnect
    }

    public override void ClearConnection()
    {
        Connection = null;
        Node.OnMove -= OnMove;
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

            ITPIn target = eventData.FindUnderPoint<ITPIn>();
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