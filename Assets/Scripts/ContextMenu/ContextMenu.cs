using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContextMenu : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private GridLayoutGroup _contexts;

    private const string BUTTON_PREFAB_PATH = "ContextMenu/ContextElem";
    
    #region Variables
    private RectTransform _rect;
    private RectTransform _contextRect;
    private GameObject _buttonPrefab;
    private List<ContextButton> _activeButtons = new();
    private Pool<ContextButton> _buttonPool;
    private List<Action> _onFinished = new();
    #endregion
    
    
    private GameObject ButtonPrefab
    {
        get
        {
            if (_buttonPrefab == null)
                _buttonPrefab = Resources.Load<GameObject>(BUTTON_PREFAB_PATH);
            return _buttonPrefab;
        }
    }
    
    private RectTransform ContextRect
    {
        get
        {
            if (_contextRect == null)
                _contextRect = _contexts.GetComponent<RectTransform>();
            return _contextRect;
        }
    }
    
    private float ContextWidth => ContextRect.rect.width;
    
    private RectTransform Rect
    {
        get
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            return _rect;
        }
    }

    private Pool<ContextButton> ButtonPool
    {
        get
        {
            if (_buttonPool == null)
            {
                _buttonPool = new Pool<ContextButton>
                (
                    createFunc: () => GetNewButton().GetComponent<ContextButton>(),
                    actionOnGet: cb =>
                    {
                        cb.SetActive(true);
                        cb.Initialize();
                    },
                    actionOnRelease: cb =>
                    {
                        cb.SetActive(false);
                        cb.Terminate();
                    },
                    actionOnDestroy: Destroy,
                    isNullPredicate: cb => cb == null || cb.gameObject == null,
                    initSize: 10,
                    maxSize: 100
                );
            }
            return _buttonPool;
        }
    }
    
    private void AddButtons(int count)
    {
        List<ContextButton> buttons = new();
        for (int i = 0; i < count; i++)
            buttons.Add(ButtonPool.Get());
        
        _activeButtons = buttons.OrderBy(button => button.SiblingIndex).ToList();
    }

    private void ButtonsClear()
    {
        foreach (ContextButton button in _activeButtons)
            _buttonPool.Release(button);
        
        _activeButtons.Clear();
    }

    private GameObject GetNewButton()
    {
        GameObject buttonGo = Instantiate(ButtonPrefab, ContextRect);
        buttonGo.SetActive(false);
        return buttonGo;
    }

    private void SetContextSize(int elementCount)
    {
        ContextRect.sizeDelta = new Vector2(ContextRect.sizeDelta.x, elementCount * _contexts.cellSize.y);
        _contexts.cellSize = new Vector2(ContextWidth, _contexts.cellSize.y);
    }

    private void FinishInvoke()
    {
        for (int i = 0; i < _onFinished.Count; i++)
            _onFinished[i]?.Invoke();
        
        _onFinished.Clear();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        FinishInvoke();
    }
    
    #region Interface
    public event Action OnFinished
    {
        add => _onFinished.Add(value);
        remove => _onFinished.Remove(value);
    }
    
    public ContextMenu SetRootCanvas(Canvas canvas)
    {
        if (canvas is null)
            return this;
        
        transform.SetParent(canvas.rootCanvas.transform);
        transform.SetAsLastSibling();
        
        Rect.anchorMin = Vector2.zero;
        Rect.anchorMax = Vector2.one;
        Rect.sizeDelta = Vector2.zero;
        Rect.localPosition = Vector3.zero;
        return this;
    }
    
    public ContextMenu SetElement(ContextElement[] elements)
    {
        int elementCount = elements.Length;
        SetContextSize(elementCount);
        AddButtons(elementCount);

        for (int i = 0; i < elements.Length; i++)
        {
            _activeButtons[i].OnClick += elements[i].ClickAction;
            _activeButtons[i].OnClick += FinishInvoke;
            _activeButtons[i].Text = elements[i].Text;
        }

        return this;
    }

    public ContextMenu SetPosition(Vector2 position)
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 contextSize = ContextRect.sizeDelta;
        
        float xPos = position.x;
        if (xPos + contextSize.x > screenSize.x)
            xPos = position.x - contextSize.x;
        
        if (xPos < 0)
            xPos = 0;
        
        float yPos = position.y;
        if (yPos < contextSize.y)
            yPos = contextSize.y;
        if (yPos > screenSize.y)
            yPos = screenSize.y;

        ContextRect.position = new Vector2(xPos, yPos);
        return this;
    }

    public void Terminate()
    {
        ButtonsClear();
    }
    #endregion
}

public class ContextElement
{
    public string Text { get; set; }
    public Action ClickAction { get; set; }

    public ContextElement(string text, Action clickAction)
    {
        Text = text;
        ClickAction = clickAction;
    }

    public ContextElement() { }
}