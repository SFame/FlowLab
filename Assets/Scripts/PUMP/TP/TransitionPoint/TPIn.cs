using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TPIn : TransitionPoint, ITPIn
{
    #region Privates
    [SerializeField]
    private bool _state;
    private LineConnector _lineConnector;

    private Action<UGUIPosition> NodeMoveAction { get; set; }

    private TPConnection SetTPConnectionLineConnector(TPConnection tpConnection)
    {
        LineConnector lineConnector = Node.Background.LineConnectManager.AddLineConnector();

        NodeMoveAction = uguiPos => OnNodeMove(lineConnector);   // 커넥션 제거 시 구독 해제를 위해 Action에 할당
        Node.OnMove += NodeMoveAction;

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
            _state = value;
            OnStateChange?.Invoke(new TransitionEventArgs());
            SetImageColor(_state ? _activeColor : _defaultColor);
        }
    }

    public event StateChangeEventHandler OnStateChange;

    public override void Connect(TPConnection connection)
    {
        Connection?.Disconnect();

        connection.TargetState = this;
        Connection = connection;

        NodeMoveAction = uguiPos => OnNodeMove(connection.LineConnector);
        Node.OnMove += NodeMoveAction;
    }
    
    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        Connection?.Disconnect();

        if (connection is null)
            connection = new();

        connection = SetTPConnectionLineConnector(connection);
        connection.TargetState = this;
        Connection = connection;

        targetTp.Connect(connection);
    }

    public override void Disconnect()
    {
        Connection = null;
        Node.OnMove -= NodeMoveAction;
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

            TPOut target = FindUnderPoint<TPOut>(eventData);
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

        TPOut find = FindUnderPoint<TPOut>(eventData);
        if (find is not null)
        {
            OnSuccessFinding(find);
            Node.RecordingCall();
        }
        else
        {
            if (Connection is not null)
                Node.RecordingCall();
            
            Connection?.Disconnect();
        }
    }

    private void OnSuccessFinding(TPOut find)
    {
        LinkTo(find);
    }
    #endregion
}
