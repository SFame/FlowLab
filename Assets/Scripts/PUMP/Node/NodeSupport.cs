using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(RectTransform))]
public class NodeSupport : DraggableUGUI, INodeSupportInitializable, ISoundable, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IMinimapProxyClient
{
    #region On Inspector (Must be)
    [SerializeField] private Image m_Image;
    [SerializeField] private TextMeshProUGUI m_NameTmp;

    [Space(10)]

    [SerializeField] private List<Graphic> m_ImageGroup;
    [SerializeField] private Color m_DefaultColor = Color.white;
    [SerializeField] private Color m_HighlightedColor = Color.green;

    [Space(10)]

    [SerializeField] private bool m_IncludeMinimap = true;
    [SerializeField] private Sprite m_MinimapSprite;
    [SerializeField] private Color m_MinimapColor;
    #endregion

    #region Don't use
    private SoundEventHandler _onSounded;
    private List<Color> _imageGroupDefaultColors;
    private readonly HashSet<object> _mouseEventBlockers = new();
    private bool _initialized;
    private bool _isDestroyed;
    private bool _isGetTp;
    private float _inEnumHeight;
    private float _outEnumHeight;
    private Vector2 _defaultNodeSize;
    private Vector2 _defaultNameTextPosition;
    private RectTransform _nameTextRect;
    private const string NODE_NAME_IDENTIFIER = "<Node>";

    private bool IsMouseEventBlocked => _mouseEventBlockers.Count > 0;

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
        OnClientDestroy?.Invoke();
        Node.Remove();
    }

    private void OnEnable()
    {
        OnActiveStateChanged?.Invoke(true);
    }

    private void OnDisable()
    {
        OnActiveStateChanged?.Invoke(false);
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
        if (IsDragging || IsMouseEventBlocked)
            return;

        OnClick?.Invoke(eventData);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (IsMouseEventBlocked)
            return;

        OnMouseDown?.Invoke(eventData);
        
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (IsMouseEventBlocked)
            return;

        OnMouseUp?.Invoke(eventData);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (IsMouseEventBlocked)
            return;

        OnMouseEnter?.Invoke(eventData);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (IsMouseEventBlocked)
            return;

        OnMouseExit?.Invoke(eventData);
    }

    void INodeSupportInitializable.Initialize(Node node)
    {
        if (_initialized)
            return;

        ComponentNullCheck();

        Node = node;
        ((INodeSupportSettable)Node).SetSupport(this);
        name = NODE_NAME_IDENTIFIER + " " + Node.GetType().Name;
        Image.color = m_DefaultColor;
        _nameTextRect = m_NameTmp.GetComponent<RectTransform>();
        _defaultNameTextPosition = _nameTextRect.anchoredPosition;
        _imageGroupDefaultColors = m_ImageGroup?.Select(graphic => graphic.color).ToList();
        OnPositionUpdate += posInfo => OnClientMove?.Invoke(posInfo.WorldPos);
        OnSizeUpdate += size => OnClientSizeUpdate?.Invoke(size);

        if (m_IncludeMinimap)
        {
            MinimapProxy.Register(this);
        }

        _initialized = true;
    }
    #endregion

    #region Interface
    public Node Node { get; private set; }
    public ITPEnumerator InputEnumerator { get; private set; }
    public ITPEnumerator OutputEnumerator { get; private set; }
    public Image Image => m_Image;
    public TextMeshProUGUI NameText => m_NameTmp;

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

    public event Action<PointerEventData> OnClick;
    public event Action<PointerEventData> OnMouseDown;
    public event Action<PointerEventData> OnMouseUp;
    public event Action<PointerEventData> OnMouseEnter;
    public event Action<PointerEventData> OnMouseExit;
    public event Action<bool> OnSetHighlight;
    public event Action OnMouseEventBlocked;
    public event Action OnMouseEventUnblocked;

    public void AddMouseEventBlocker(object blocker)
    {
        if (blocker == null)
            return;

        if (_mouseEventBlockers.Count == 0)
            OnMouseEventBlocked?.Invoke();
        
        _mouseEventBlockers.Add(blocker);
    }

    public void RemoveMouseEventBlocker(object blocker)
    {
        if (blocker == null)
            return;

        if (_mouseEventBlockers.Remove(blocker) && _mouseEventBlockers.Count == 0)
            OnMouseEventUnblocked?.Invoke();
    }

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

    public void SetName(string text)
    {
        if (text == null)
            return;

        NameText.text = text;
    }

    public void SetNamePositionOffset(Vector2 offset) => _nameTextRect.anchoredPosition = _defaultNameTextPosition + offset;

    public void SetNameFontSize(float size) => NameText.fontSize = size;

    public void SetRectDeltaSize(Vector2 size) => Rect.sizeDelta = size;

    public void SetColor(Color color)
    {
        Image.color = color;

        if (m_ImageGroup == null)
            return;

        foreach (Graphic graphic in m_ImageGroup)
        {
            graphic.color = color;
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlighted)
        {
            SetColor(HighlightedColor);
        }
        else
        {
            RevertToDefaultColor();
        }

        OnSetHighlight?.Invoke(highlighted);
    }

    public void DestroyObject()
    {
        if (_isDestroyed)
            return;

        Destroy(gameObject);
    }
    #endregion

    #region Minimap
    public Vector2 CurrentWorldPosition => Rect.position;
    public event Action<Vector2> OnClientMove;
    public event Action<Vector2> OnClientSizeUpdate;
    public event Action OnClientDestroy;
    public event Action<bool> OnActiveStateChanged;
    public Sprite Sprite => m_MinimapSprite;
    public Color SpriteColor => m_MinimapColor;
    public Vector2 Size => new Vector2(Rect.rect.x, Rect.rect.y);

    #endregion
}

public interface INodeSupportInitializable
{
    void Initialize(Node node);
}