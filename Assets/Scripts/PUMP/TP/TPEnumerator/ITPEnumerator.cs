using System.Collections.Generic;
using System;
using UnityEngine;

public interface ITPEnumerator : IActivable
{
    public Node Node { get; set; }
    public ITPEnumerator SetTPs(int count);
    public void SetTPsConnection(ITransitionPoint[] targetTps, List<Vector2>[] vertices, DeserializationCompleteReceiver completeReceiver);
    public ITPEnumerator SetTPSize(Vector2 value);
    public ITPEnumerator SetTPsMargin(float value);
    public ITPEnumerator SetHeight(float value);
    public event Action<Vector2> OnSizeUpdatedWhenTPChange;
    public float MinHeight { get; set; }
    public TPEnumeratorToken GetToken();
}

public interface IActivable
{
    public void SetActive(bool active);
}