using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public abstract class NodePalette : MonoBehaviour
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

    public void SetActive(bool isActive)
    {
        m_CanvasGroup.alpha = 1f;
        _isActive = isActive;
        gameObject.SetActive(isActive);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetActive(value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;

            if (!_isActive)
            {
                return;
            }

            m_CanvasGroup.DOKill();
            m_CanvasGroup.DOFade(_isVisible ? 1 : 0, m_FadeDuration);
        }
    }
    #endregion

    #region Privates
    private GameObject _elementPrefab;
    private bool _isActive = false;
    private bool _isVisible = false;

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
        m_PrismView.Release();

        await UniTask.Yield();

        Dictionary<string, List<RectTransform>> prism = new();

        foreach (KeyValuePair<string, Dictionary<Type, string>> kvp in NodeTypes)
        {
            List<RectTransform> categoryRectList = new();

            foreach (KeyValuePair<Type, string> innerKvp in kvp.Value)
            {
                RectTransform newElemRect = GetNewElement();
                PaletteElem newElem = newElemRect.GetComponent<PaletteElem>();
                newElem.Text = innerKvp.Value;
                newElem.NodeType = innerKvp.Key;

                var resourceTuple = GetResource(innerKvp.Key);

                if (resourceTuple.sprite != null)
                {
                    newElem.Sprite = resourceTuple.sprite;
                }

                newElem.SpriteColor = resourceTuple.backgroundColor;
                newElem.TextColor = resourceTuple.textColor;

                SetElementCallback(newElem);
                categoryRectList.Add(newElemRect);
            }

            prism[kvp.Key] = categoryRectList;
        }

        m_PrismView.Allocate(prism);
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
        elem.OnInstantiate += newNode => OnNodeAdded?.Invoke(newNode);
        elem.OnDragEnd += () => IsVisible = true;
    }

    private (Sprite sprite, Color backgroundColor, Color textColor) GetResource(Type nodeType)
    {
        ResourceGetterAttribute resourceGetter = (ResourceGetterAttribute)Attribute.GetCustomAttribute(nodeType, typeof(ResourceGetterAttribute), true);

        if (resourceGetter == null)
        {
            return (null, Color.white, Color.black);
        }
        
        string imagePath = resourceGetter.Path ?? string.Empty;
        return (string.IsNullOrEmpty(imagePath) ? null : Resources.Load<Sprite>(imagePath), resourceGetter.BackgroundColor, resourceGetter.TextColor);
    }
    #endregion
}