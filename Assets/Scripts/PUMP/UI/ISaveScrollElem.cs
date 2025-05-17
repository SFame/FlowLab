using PolyAndCode.UI;
using System;
using UnityEngine.EventSystems;

public interface ISaveScrollElem : ICell
{
    public void Initialize(PUMPSaveDataStructure data);
    public void Refresh();
    public event Action<PUMPSaveDataStructure, PointerEventData> OnRightClick;
    public event Action<PUMPSaveDataStructure> OnDoubleClick;
}