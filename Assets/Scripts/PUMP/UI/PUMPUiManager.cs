using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class PUMPUiManager : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Canvas m_RootCanvas;
    [SerializeField] private GameObject m_LayerPrefab;
    #endregion

    #region Interface
    public static PUMPUiManager Instance { get; private set; }

    public Canvas RootCanvas => m_RootCanvas;

    public void Render(RectTransform ui, int layerIndex, Action<RectTransform> returner, Action<RectTransform> onRender = null)
    {
        AddLayer(layerIndex);
        CallReturner(layerIndex);
        ui.SetParent(_layerList[layerIndex]);
        ui.gameObject.SetActive(true);
        onRender?.Invoke(ui);
        _returners[layerIndex] = () => returner?.Invoke(ui);
    }

    public void Discard(int layerIndex)
    {
        CallReturner(layerIndex);
    }
    #endregion

    #region Privates
    private readonly List<RectTransform> _layerList = new();
    private readonly Dictionary<int, Action> _returners = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CallReturner(int layerIndex)
    {
        if (!_returners.TryGetValue(layerIndex, out Action returner))
        {
            return;
        }

        returner?.Invoke();
        _returners[layerIndex] = null;
    }

    private void AddLayer(int layerIndex)
    {   
        if (layerIndex < 0 || _layerList.Count > layerIndex)
        {
            return;
        }

        int count = layerIndex - _layerList.Count + 1;

        for (; count > 0; count--)
        {
            _layerList.Add(GetNewLayer($"Layer_{_layerList.Count}"));
        }
    }

    private RectTransform GetNewLayer(string name)
    {
        GameObject newLayerObject = Instantiate(m_LayerPrefab);
        newLayerObject.name = name;
        RectTransform newLayer = newLayerObject.GetComponent<RectTransform>();
        SetRectProperty(newLayer);
        return newLayer;
    }

    private void SetRectProperty(RectTransform rect)
    {
        rect.SetParent(transform);
        rect.SetRectFull();
    }
    #endregion
}
