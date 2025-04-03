using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class TPOut : TransitionPoint, ITPOut
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

    public override void LinkTo(ITransitionPoint targetTp, TPConnection connection = null)
    {
        Connection?.Disconnect();

        if (connection is null)
            connection = new();

        connection = SetTPConnectionLineConnector(connection);
        connection.SourceState = this;
        Connection = connection;

        targetTp.Connect(connection);
    }
    #endregion

    #region Interface
    public override bool State
    {
        get => _state;
        set
        {
            _state = value;
            if (Connection is not null)
            {
                Connection.State = value;
            }
            SetImageColor(_state ? _activeColor : _defaultColor);
        }
    }

    public override void Connect(TPConnection connection)
    {
        Connection?.Disconnect();

        connection.SourceState = this;
        Connection = connection;

        OnMove = uguiPos => OnNodeMove(connection.LineConnector);
        Node.OnMove += OnMove;
    }

    public override void Disconnect()
    {
        Connection = null;
        Node.OnMove -= OnMove;
        OnMove = null;
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
            OnSuccessFinding(find);
        }
        else
        {
            if (Connection is not null)
                Node.ReportChanges();
            
            Connection?.Disconnect();
        }
    }

    private void OnSuccessFinding(ITPIn find)
    {
        LinkTo(find);
    }
    #endregion
}