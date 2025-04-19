using System;
using UnityEngine;

public interface INameable
{
    string Name { get; set; }
}

public interface IStateful
{
    bool State { get; set; }
}

public interface ILocatable
{
    Vector2 Location { get; }
}

public interface IDeserializingListenable
{
    bool OnDeserializing { get; set; }
}

public interface IGameObject
{
    GameObject GameObject { get; }
}

public interface IMoveable
{
    Action<PositionInfo> OnMove { get; set; }
}

/// <summary>
/// 노드를 잇는 말단 객체
/// Connection.Disconnect()는 양쪽 모두의 커넥션을 해제를 의미
/// ITransitionPoint.Disconnect()는 Connection객체의 참조를 지우도록 설계
/// Connection.Disconnect()에서 양쪽의 ITransitionPoint.Disconnect()를 호출하도록
/// LinkTo() 메서드로 상대 TP와 연결
/// </summary>
public interface ITransitionPoint : INameable, IStateful, ILocatable
{
    int Index { get; set; }
    TPConnection Connection { get; set; }
    Node Node { get; set; }
    bool BlockConnect { get; set; }
    void LinkTo(ITransitionPoint targetTp, TPConnection connection = null);
    void AcceptLink(TPConnection connection);
    void ClearConnection();
}

public interface ITPIn : ITransitionPoint
{
    /// <summary>
    /// Node에서 이벤트 구독
    /// </summary>
    event StateChangeEventHandler OnStateChange;
}

public interface ITPOut : ITransitionPoint
{
    bool IsStatePending { get; }
    void PushToConnection();
}