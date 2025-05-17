using System;
using UnityEngine;

public interface INameable
{
    string Name { get; set; }
}

public interface ILocatable
{
    Vector2 WorldPosition { get; }
    Vector2 LocalPosition { get; }
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

public interface ITPHideable
{
    void AddHider(object hider);
    void SubHider(object hider);
}

public interface IStateful
{
    // 우선 설정, State 백킹필드에 동일 타입 Null값 삽입
    TransitionType Type { get; }
    Transition State { get; set; }

    bool IsActivateState()
    {
        if (Type == TransitionType.Bool)
        {
            if (State.Type == TransitionType.Bool)
            {
                return State;
            }

            return false;
        }

        return !State.IsNull;
    }
}

public interface ITypeListenStateful : IStateful
{
    event Action<TransitionType> OnTypeChanged;
}

public interface IPolymorphicStateful : IStateful
{
    // 현재 Type과 입력된 type이 같다면 설정하지 않고 return 하도록 설계
    void SetType(TransitionType type);
}

/// <summary>
/// 노드의 연결 포인트
/// Connection.Disconnect()는 양쪽 모두의 커넥션을 해제를 의미
/// ITransitionPoint.Disconnect()는 Connection객체의 참조를 지우도록 설계
/// Connection.Disconnect()에서 양쪽의 ITransitionPoint.Disconnect()를 호출하도록
/// LinkTo() 메서드로 상대 TP와 연결
/// </summary>
public interface ITransitionPoint : IPolymorphicStateful, ITypeListenStateful, INameable, ILocatable
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