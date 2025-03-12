using System.Collections.Generic;
using System;
using UnityEngine;

public interface ITPEnumerator : IActivable
{
    public Node Node { get; set; }
    public ITPEnumerator SetTPs(int count);
    public void SetTPsConnection(ITransitionPoint[] targetTps, List<Vector2>[] vertices);
    public ITPEnumerator SetGridSize(Vector2 value);
    public ITPEnumerator SetGridMargin(float value);
    public ITPEnumerator SetHeight(float value);
    public event Action<Vector2> OnSizeUpdatedWhenTPChange;
    public float MinHeight { get; set; }
    public TPEnumeratorToken GetToken();
}

public interface IActivable
{
    public void SetActive(bool active);
}