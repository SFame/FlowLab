using System;
using UnityEngine;

public interface IMinimapProxyClient
{
    Vector2 CurrentWorldPosition { get; }
    event Action<Vector2> OnClientMove;
    event Action OnClientDestroy;
    event Action<bool> OnActiveStateChanged;
    Sprite Sprite { get; }
    Color SpriteColor { get; }
}