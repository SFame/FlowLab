using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class PUMPUiManager : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Canvas m_RootCanvas;
    [SerializeField] private GameObject m_LayerPrefab;
    [SerializeField] private Transform m_LayerParent;
    #endregion

    #region Interface
    public static Canvas RootCanvas => Instance.m_RootCanvas;

    public static void Render(RectTransform ui, int layerIndex, Action<RectTransform> onRender, Action<RectTransform> onReturn)
    {
        Instance.AddLayer(layerIndex);
        Instance.CallReturner(layerIndex);
        ui.SetParent(Instance._layerList[layerIndex]);
        onRender?.Invoke(ui);
        Instance._returners[layerIndex] = () => onReturn?.Invoke(ui);
    }

    public static void Discard(int layerIndex)
    {
        Instance.CallReturner(layerIndex);
    }
    #endregion

    #region Privates
    private readonly List<RectTransform> _layerList = new();
    private readonly Dictionary<int, Action> _returners = new();
    private const string UI_PREFAB_PATH = "PUMP/Prefab/UI/PumpUiCanvas";
    private static GameObject _uiPrefab;
    private static PUMPUiManager _instance;

    private static PUMPUiManager Instance
    {
        get
        {
            InstanceCheck();
            return _instance;
        }
    }

    private static GameObject UiPrefab
    {
        get
        {
            if (_uiPrefab == null)
            {
                _uiPrefab = Resources.Load<GameObject>(UI_PREFAB_PATH);
            }

            return _uiPrefab;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            return;
        }

        if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private static void InstanceCheck()
    {
        if (_instance == null)
        {
            GameObject newObject = Instantiate(UiPrefab);
            _instance = newObject.GetComponent<PUMPUiManager>();
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
        rect.SetParent(m_LayerParent);
        rect.SetRectFull();
    }
    #endregion
}