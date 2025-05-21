using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public abstract class NodePalette : MonoBehaviour, IPointerDownHandler
{
    #region On Inspector
    [SerializeField] private PrismView m_PrismView;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private float m_FadeDuration = 0.05f;
    #endregion

    #region Interface
    public abstract Dictionary<string, Dictionary<Type, string>> NodeTypes { get; set; }
    
    public event Action<Node> OnNodeAdded;
    
    public void SetContent()
    {
        SetContentAsync().Forget();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            _cts = _cts.CancelAndDisposeAndGetNew();
            Other.LerpAction
            (
                m_FadeDuration,
                lerp =>
                {
                    float calc = value ? lerp : 1f - lerp;
                    m_CanvasGroup.alpha = calc;
                },
                null,
                _cts!.Token
            ).Forget();
        }
    }
    #endregion

    #region Privates
    private GameObject _elementPrefab;
    private bool _isVisible = false;
    private SafetyCancellationTokenSource _cts = new();

    private const string ELEMENT_PREFAB_PATH = "PUMP/Prefab/NodePalette/PaletteElem";

    private GameObject ElementPrefab
    {
        get
        {
            _elementPrefab ??= Resources.Load<GameObject>(ELEMENT_PREFAB_PATH);
            return _elementPrefab;
        }
    }

    private async UniTaskVoid SetContentAsync()
    {
        m_PrismView.Clear();

        await UniTask.Yield();

        Dictionary<string, List<RectTransform>> prism = new();

        foreach (KeyValuePair<string, Dictionary<Type, string>> kvp in NodeTypes)
        {
            List<RectTransform> categoryRectList = new();

            foreach (KeyValuePair<Type, string> innerKvp in kvp.Value)
            {
                RectTransform newElemRect = GetNewElement();
                PaletteElem newElem = newElemRect.GetComponent<PaletteElem>();
                newElem.DisplayName = innerKvp.Value;
                newElem.NodeType = innerKvp.Key;

                if (GetSprite(innerKvp.Key) is { } sprite)
                    newElem.Image.sprite = sprite;

                SetElementCallback(newElem);
                categoryRectList.Add(newElemRect);
            }

            prism[kvp.Key] = categoryRectList;
        }

        m_PrismView.Initialize(prism);
    }

    private RectTransform GetNewElement()
    {
        GameObject newElement = Instantiate(ElementPrefab);
        RectTransform elemRect = newElement.GetComponent<RectTransform>();
        return elemRect;
    }

    private void SetElementCallback(PaletteElem elem)
    {
        elem.OnDragStart += () => IsVisible = false;
        elem.OnInstantiate += () => OnNodeAdded?.Invoke(elem.NewNode);
        elem.OnDragEnd += () => IsVisible = true;
    }

    private Sprite GetSprite(Type nodeType)
    {
        string imagePath = ((ResourceGetterAttribute)Attribute.GetCustomAttribute(nodeType, typeof(ResourceGetterAttribute), true))?.Path ?? string.Empty;
        return string.IsNullOrEmpty(imagePath) ? null : Resources.Load<Sprite>(imagePath);
    }
    #endregion

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);

        if (result.Count <= 0)
            return;

        if (result[0].gameObject == gameObject)
            Close();
    }
}