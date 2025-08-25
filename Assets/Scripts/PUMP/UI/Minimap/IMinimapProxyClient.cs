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
    /// 오브젝트 파괴 시 Invoke()
    /// </summary>
    event Action OnClientDestroy;

    /// <summary>
    /// 오브젝트 활성화/비활성화 시 Invoke()
    /// </summary>
    event Action<bool> OnActiveStateChanged;

    /// <summary>
    /// Rect.Position 반환
    /// </summary>
    Vector2 CurrentWorldPosition { get; }

    /// <summary>
    /// new Vector2(Rect.rect.width, Rect.rect.height) 반환
    /// </summary>
    Vector2 Size { get; }

    /// <summary>
    /// 미니맵에 표시될 Sprite
    /// </summary>
    Sprite Sprite { get; }

    /// <summary>
    /// 해당 Sprite의 색상
    /// </summary>
    Color SpriteColor { get; }
}