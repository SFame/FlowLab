using System;
using UnityEngine;

public interface IMinimapProxyClient
{
    /// <summary>
    /// Rect.Position 인자 전달 후 Invoke()
    /// </summary>
    event Action<Vector2> OnClientMove;

    /// <summary>
    /// new Vector2(Rect.rect.width, Rect.rect.height) 인자 전달 후 Invoke()
    /// </summary>
    event Action<Vector2> OnClientSizeUpdate;

    /// <summary>
    /// 색상 변경 시 현재 Color 전달 후 Invoke()
    /// </summary>
    event Action<Color> OnClientColorUpdate;

    /// <summary>
    /// 오브젝트 파괴 시 Invoke()
    /// </summary>
    event Action OnClientDestroy;

    /// <summary>
    /// 오브젝트 활성화/비활성화 시 Invoke()
    /// </summary>
    event Action<bool> OnActiveStateChanged;

    /// <summary>
    /// 미러링 객체에 표시될 이름
    /// </summary>
    string MirrorName { get; }

    /// <summary>
    /// Rect.Position 반환
    /// </summary>
    Vector2 CurrentWorldPosition { get; }

    /// <summary>
    /// 레이어 Order-Z
    /// </summary>
    float OrderZ { get; }

    /// <summary>
    /// Z축 로테이션
    /// </summary>
    float RotationZ { get; }

    /// <summary>
    /// new Vector2(Rect.rect.width, Rect.rect.height) 반환
    /// </summary>
    Vector2 DefaultSize { get; }

    /// <summary>
    /// 미니맵에 표시될 Sprite
    /// </summary>
    Sprite Sprite { get; }

    /// <summary>
    /// 해당 Sprite의 색상
    /// </summary>
    Color SpriteDefaultColor { get; }
}