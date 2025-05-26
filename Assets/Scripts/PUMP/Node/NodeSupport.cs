using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(RectTransform))]
public class NodeSupport : DraggableUGUI, INodeSupportInitializable, ISoundable, IDragSelectable, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    #region On Inspector (Must be)
    [SerializeField] private Image m_Image;
    [SerializeField] private TextMeshProUGUI m_NameTmp;

    [Space(10)]

    [SerializeField] private List<Image> m_ImageGroup;
    [SerializeField] private Color m_DefaultColor = Color.white;
    [SerializeField] private Color m_HighlightedColor = Color.green;
    #endregion

    #region Don't use
    private Canvas _rootCanvas;
    private SoundEventHandler _onSounded;
    private List<Color> _imageGroupDefaultColors;
    private bool _initialized;
    private bool _isDestroyed;
    private bool _isGetTp;
    private float _inEnumHeight;
    private float _outEnumHeight;
    private Vector2 _defaultNodeSize;

    event SoundEventHandler ISoundable.OnSounded
    {
        add => _onSounded += value;
        remove => _onSounded -= value;
    }

    private void OnDestroy()
    {
        if (_isDestroyed)
            return;

        _isDestroyed = true;
        Node.Remove();
    }

    private void OnDisable()
    {
        SelectedRemoveRequestInvoke();
        ((IDragSelectable)this).IsSelected = false;
    }

    private void ComponentNullCheck()
    {
        if (m_Image != null && m_NameTmp != null)
            return;

        throw new MissingComponentException($"{name}: Components must be assigned");
    }

    private void RevertToDefaultColor()
    {
        Image.color = DefaultColor;

        if (m_ImageGroup == null || _imageGroupDefaultColors == null || m_ImageGroup.Count != _imageGroupDefaultColors.Count)
            return;

        for (int i = 0; i < m_ImageGroup.Count; i++)
        {
            m_ImageGroup[i].color = _imageGroupDefaultColors[i];
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (BlockMouseEvent)
            return;

        if (_selectedContextElementsGetter == null)
        {
            OnClick?.Invoke(eventData);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = _selectedContextElementsGetter?.Invoke();
            ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (BlockMouseEvent)
            return;

        if (_selectedContextElementsGetter == null)
        {
            OnMouseDown?.Invoke(eventData);
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (BlockMouseEvent)
            return;

        if (_selectedContextElementsGetter == null)
        {
            OnMouseUp?.Invoke(eventData);
        }
    }

    void INodeSupportInitializable.Initialize(Node node)
    {
        if (_initialized)
            return;

        ComponentNullCheck();

        Node = node;
        ((INodeSupportSettable)Node).SetSupport(this);
        name = Node.GetType().Name;
        OnDragging += (pointerEventArgs, _) => _onSelectedMove?.Invoke(this, pointerEventArgs.delta);
        _imageGroupDefaultColors = m_ImageGroup?.Select(image => image.color).ToList();
        _initialized = true;
    }
    #endregion

    #region Interface
    public Node Node { get; private set; }
    public ITPEnumerator InputEnumerator { get; private set; }
    public ITPEnumerator OutputEnumerator { get; private set; }
    public Image Image => m_Image;
    public TextMeshProUGUI NameText => m_NameTmp;

    public Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= Rect.GetRootCanvas();
            return _rootCanvas;
        }
    }

    public Color DefaultColor
    {
        get => m_DefaultColor;
        set => m_DefaultColor = value;
    }

    public Color HighlightedColor
    {
        get => m_HighlightedColor;
        set => m_HighlightedColor = value;
    }

    public bool BlockMouseEvent { get; set; }

    public event Action<PointerEventData> OnClick;
    public event Action<PointerEventData> OnMouseDown;
    public event Action<PointerEventData> OnMouseUp;

    public void PlaySound(int index)
    {
        if (!Node.OnDeserializing)
        {
            _onSounded?.Invoke(this, new SoundEventArgs(index, WorldPosition));
        }
    }

    public void SetSpriteForResourcesPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite is null)
        {
            Debug.LogError($"{Node.GetType().Name}: Can't find resource <Sprite> : {path}");
            return;
        }

        Image.sprite = sprite;
    }

    public void InitializeTPEnumerator(
        string inPath, string outPath, float inEnumXPos, 
        float outEnumXPos, Vector2 defaultNodeSize, bool sizeFreeze)
    {
        if (_isGetTp)
        {
            throw new Exception("TPEnumerator를 중복하여 생성하고 있습니다");
        }

        GameObject[] objects = Other.GetResources<GameObject>(inPath, outPath);

        if (objects.Any(obj => obj == null))
        {
            throw new NullReferenceException("경로에 TPEnumerator 프리펩이 없습니다");
        }

        GameObject inputObject = Instantiate(objects[0]);
        GameObject outputObject = Instantiate(objects[1]);

        RectTransform inRect = inputObject.GetComponent<RectTransform>();
        RectTransform outRect = outputObject.GetComponent<RectTransform>();

        if (inRect == null || outRect == null)
        {
            throw new MissingComponentException("Enumerator 프리펩은 UGUI 오브젝트가 아닙니다");
        }

        RectTransformUtils.SetParent(Rect, inRect, outRect);

        inRect.SetAnchor(min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));
        outRect.SetAnchor(min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));

        inRect.SetOffset(min: new Vector2(inRect.offsetMin.x, 0f), max: new Vector2(inRect.offsetMax.x, 0f));
        outRect.SetOffset(min: new Vector2(outRect.offsetMin.x, 0f), max: new Vector2(outRect.offsetMax.x, 0f));

        inRect.SetXPos(inEnumXPos);
        outRect.SetXPos(outEnumXPos);

        ITPEnumerator inputEnumerator = inRect.GetComponent<ITPEnumerator>();
        ITPEnumerator outputEnumerator = outRect.GetComponent<ITPEnumerator>();

        if (inputEnumerator == null || outputEnumerator == null)
        {
            throw new MissingComponentException($"ITPEnumerator 컴포넌트를 찾을 수 없습니다. Input: {inputEnumerator} / Output: {outputEnumerator}");
        }

        try
        {
            InputEnumerator = inputEnumerator;
            OutputEnumerator = outputEnumerator;

            _defaultNodeSize = defaultNodeSize;

            InputEnumerator.MinHeight = _defaultNodeSize.y;
            OutputEnumerator.MinHeight = _defaultNodeSize.y;

            if (!sizeFreeze)
            {
                InputEnumerator.OnSizeUpdatedWhenTPChange += size =>
                {
                    _inEnumHeight = size.y;
                    float maxValue = HeightSynchronizationWithEnum();
                    InputEnumerator.SetHeight(maxValue);
                    OutputEnumerator.SetHeight(maxValue);
                };

                OutputEnumerator.OnSizeUpdatedWhenTPChange += size =>
                {
                    _outEnumHeight = size.y;
                    float maxValue = HeightSynchronizationWithEnum();
                    InputEnumerator.SetHeight(maxValue);
                    OutputEnumerator.SetHeight(maxValue);
                };
            }

            InputEnumerator.Node = Node;
            OutputEnumerator.Node = Node;
            _isGetTp = true;
        }
        catch (Exception e)
        {
            InputEnumerator = null;
            OutputEnumerator = null;
            Destroy(inputObject);
            Destroy(outputObject);
            Debug.LogError($"Enumerator 속성 설정 과정에서 예외 발생\n{e.Message}");
            throw;
        }
    }

    public float HeightSynchronizationWithEnum()
    {
        float maxHeight = Mathf.Max(_inEnumHeight, _outEnumHeight, _defaultNodeSize.y);
        SetRectSizeDelta(new Vector2(_defaultNodeSize.x, maxHeight));
        return maxHeight;
    }

    public void SetName(string text) => NameText.text = text;
    public void SetNameFontSize(float size) => NameText.fontSize = size;
    public void SetRectDeltaSize(Vector2 size) => Rect.sizeDelta = size;

    public void SetColor(Color color)
    {
        Image.color = color;

        if (m_ImageGroup == null)
            return;

        foreach (Image imageGroup in m_ImageGroup)
        {
            imageGroup.color = color;
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlighted)
        {
            SetColor(HighlightedColor);
            return;
        }

        RevertToDefaultColor();
    }

    public void DestroyObject()
    {
        if (_isDestroyed)
            return;

        Destroy(gameObject);
    }
    #endregion

    #region Selecting handler
    /// <summary>
    /// Selected 관리자에게 선택 객체들 해제 요청
    /// </summary>
    public void SelectedRemoveRequestInvoke()
    {
        _selectRemoveRequest?.Invoke();
    }


    bool IDragSelectable.IsSelected
    {
        get => _isSelected;
        set
        {
            SetHighlight(value);
            _isSelected = value;
        }
    }

    bool IDragSelectable.CanDestroy => !Node.IgnoreSelectedDelete;
    bool IDragSelectable.CanDisconnect => !Node.IgnoreSelectedDisconnect;

    object IDragSelectable.SelectingTag
    {
        get => _selectedContextElementsGetter;
        set => _selectedContextElementsGetter = value as Func<List<ContextElement>>;
    }

    private bool _isSelected = false;
    private Func<List<ContextElement>> _selectedContextElementsGetter;
    private OnSelectedMoveHandler _onSelectedMove;
    private Action _selectRemoveRequest;

    void IDragSelectable.MoveSelected(Vector2 direction) => MovePosition(direction);
    void IDragSelectable.ObjectDestroy() => Node.Remove();
    void IDragSelectable.ObjectDisconnect() => Node.Disconnect();


    event OnSelectedMoveHandler IDragSelectable.OnSelectedMove
    {
        add => _onSelectedMove += value;
        remove => _onSelectedMove -= value;
    }

    event Action IDragSelectable.SelectRemoveRequest
    {
        add => _selectRemoveRequest += value;
        remove => _selectRemoveRequest -= value;
    }
    #endregion
}

public interface INodeSupportInitializable
{
    void Initialize(Node node);
}