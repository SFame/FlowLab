using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

public abstract class BaseCodexPalette : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private PrismView m_PrismView;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private float m_FadeDuration = 0.05f;
    [SerializeField] private TMPro.TMP_Text TMP_Text;
    #endregion

    private GameObject _elementPrefab;
    private bool _isVisible = false;
    private SafetyCancellationTokenSource _cts = new();

    private const string ELEMENT_PREFAB_PATH = "PUMP/Prefab/NodePalette/CodexElem";

    public abstract Dictionary<string, Dictionary<Type, string>> NodeTypes { get; set; }

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
                CodexElem newElem = newElemRect.GetComponent<CodexElem>();
                newElem.DisplayName = innerKvp.Value;
                newElem.NodeType = innerKvp.Key;

                var resourceTuple = GetResource(innerKvp.Key);

                if (resourceTuple.sprite != null)
                    newElem.Image.sprite = resourceTuple.sprite;

                newElem.Image.color = resourceTuple.color;
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
    private void SetElementCallback(CodexElem elem)
    {
        elem.TMP_Text = TMP_Text;
    }
    private (Sprite sprite, Color color) GetResource(Type nodeType)
    {
        ResourceGetterAttribute resourceGetter =
            (ResourceGetterAttribute)Attribute.GetCustomAttribute(nodeType, typeof(ResourceGetterAttribute), true);

        if (resourceGetter == null)
            return (null, Color.white);

        string imagePath = resourceGetter.Path ?? string.Empty;
        return (string.IsNullOrEmpty(imagePath) ? null : Resources.Load<Sprite>(imagePath), resourceGetter.BackgroundColor);
    }
}
