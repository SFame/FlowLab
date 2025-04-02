using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Compatibility;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public abstract class NodePalette : MonoBehaviour
{
    #region Interface
    public abstract Dictionary<Type, string> NodeTypes { get; set; }
    
    public event Action<Node> OnNodeAdded;
    
    public void SetContent()
    {
        SetContentAsync().Forget();
    }
    #endregion
    
    #region Privates
    private ScrollRect _scrollRect;
    private GridLayoutGroup _gridLayoutGroup;
    private GameObject _elementPrefab;
    private readonly List<PaletteElem> _elements = new();
    private RectTransform _rootRect;

    private const string ELEMENT_PREFAB_PATH = "PUMP/Prefab/NodePalette/PaletteElem";

    private GameObject ElementPrefab
    {
        get
        {
            if (_elementPrefab is null)
                _elementPrefab = Resources.Load<GameObject>(ELEMENT_PREFAB_PATH);
            
            return _elementPrefab;
        }
    }
    private ScrollRect ScrollRect
    {
        get
        {
            if (_scrollRect is null)
                _scrollRect = GetComponentInChildren<ScrollRect>();
            
            return _scrollRect;
        }
    }

    private RectTransform RootRect
    {
        get
        {
            if (_rootRect is null)
                _rootRect = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();;
            
            return _rootRect;
        }
    }
    
    private RectTransform Content => ScrollRect.content;
    
    private float ContentWidth => Content.rect.width;

    private float ContentHeight
    {
        get => Content.sizeDelta.y;
        set => Content.sizeDelta = new Vector2(Content.sizeDelta.x, value);
    }

    private float ElementHeight => 100f;

    private async UniTaskVoid SetContentAsync()
    {
        await UniTask.WaitUntil(() => ContentWidth > float.Epsilon);
        
        SetContentRectSize();
        _elements.WhereForeach(elem => !elem.IsUnityNull(), elem => Destroy(elem.gameObject));
        _elements.Clear();

        foreach (KeyValuePair<Type, string> kvp in NodeTypes)
        {
            RectTransform newElemRect = GetNewElemWithScaled();
            PaletteElem newElem = newElemRect.GetComponent<PaletteElem>();
            newElem.DisplayName = kvp.Value;
            newElem.NodeType = kvp.Key;

            if (GetSprite(kvp.Key) is Sprite sprite)
                newElem.Image.sprite = sprite;

            SetElementCallback(newElem);
            _elements.Add(newElem);
        }
        
        SortContent();
    }

    private void SortContent()
    {
        Vector2 defaultPos = new Vector2(ContentWidth / 2, -(ElementHeight / 2));

        for (int i = 0; i < _elements.Count; i++)
            _elements[i].Rect.anchoredPosition = new Vector2(defaultPos.x, defaultPos.y + -i * ElementHeight);
    }

    private void SetContentRectSize()
    {
        ContentHeight = NodeTypes.Count * ElementHeight;
    }

    private RectTransform GetNewElemWithScaled()
    {
        GameObject newElement = Instantiate(ElementPrefab, Content.transform);
        RectTransform elemRect = newElement.GetComponent<RectTransform>();
        elemRect.anchorMin = new Vector2(0f, 1f);
        elemRect.anchorMax = new Vector2(0f, 1f);
        elemRect.sizeDelta = new Vector2(ContentWidth, ElementHeight);
        return elemRect;
    }

    private void SetElementCallback(PaletteElem elem)
    {
        elem.OnDragStart += () =>
        {
            elem.ContentFixPosition = elem.Rect.anchoredPosition;
            elem.Rect.SetParent(RootRect, true);
        };
        elem.OnDragEnd += () =>
        {
            elem.Rect.SetParent(Content, true);
            elem.Rect.anchoredPosition = elem.ContentFixPosition;
        };
        elem.OnInstantiate += () => OnNodeAdded?.Invoke(elem.NewNode);
    }

    private Sprite GetSprite(Type nodeType)
    {
        string imagePath = ((ResourceGetterAttribute)Attribute.GetCustomAttribute(nodeType, typeof(ResourceGetterAttribute), true))?.Path ?? string.Empty;
        return string.IsNullOrEmpty(imagePath) ? null : Resources.Load<Sprite>(imagePath);
    }
    #endregion
}
