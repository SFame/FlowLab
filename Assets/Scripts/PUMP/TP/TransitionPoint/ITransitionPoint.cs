using System;
using UnityEngine;

public interface INameable
{
    public string Name { get; set; }
}

public interface IStateful
{
    public bool State { get; set; }
}

public interface ILocatable
{
    public Vector2 Location { get; }
}

public interface IDeserializingListenable
{
    public bool OnDeserializing { get; set; }
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
    public TPConnection Connection { get; set; }
    public GameObject GameObject { get; }
    public Node Node { get; set; }
    public bool BlockConnect { get; set; }
    public void LinkTo(ITransitionPoint targetTp, TPConnection connection = null);
    public void Connect(TPConnection connection);
    public void Disconnect();
    public Action<UGUIPosition> OnMove { get; set; }
}

public interface ITPIn : ITransitionPoint
{
    /// <summary>
    /// Node에서 이벤트 구독
    /// </summary>
    public event StateChangeEventHandler OnStateChange;
}

public interface ITPOut : ITransitionPoint { }