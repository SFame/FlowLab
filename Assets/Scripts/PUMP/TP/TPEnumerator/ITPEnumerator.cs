using System.Collections.Generic;
using System;
using UnityEngine;

public interface ITPEnumerator : IActivable
{
    Node Node { get; set; }
    ITransitionPoint[] GetTPs();
    ITPEnumerator SetTPCount(int count);
    void SetTPConnections(ITransitionPoint[] targetTps, List<Vector2>[] vertices, DeserializationCompleteReceiver completeReceiver);
    ITPEnumerator SetTPSize(Vector2 value);
    ITPEnumerator SetPadding(float value);
    ITPEnumerator SetMargin(float margin);
    ITPEnumerator SetHeight(float value);

    event Action<Vector2> OnSizeUpdatedWhenTPChange;
    float MinHeight { get; set; }

    TPEnumeratorToken GetToken();
}

public interface IActivable
{
    void SetActive(bool active);
}