using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(RectTransform))]
public class NodeSupport : MonoBehaviour, ISoundable
{
    #region On Inspector (Must be)
    [SerializeField] private Image m_Image;
    [SerializeField] private TextMeshProUGUI m_NameTmp;
    [SerializeField] private Color m_DefaultColor = Color.white;
    [SerializeField] private Color m_HighlightedColor = Color.green;
    #endregion

    #region Don't use
    private Node _node;
    private Canvas _rootCanvas;
    private RectTransform _rect;
    private SoundEventHandler _onSounded;
    private bool _initialized;

    event SoundEventHandler ISoundable.OnSounded
    {
        add => _onSounded += value;
        remove => _onSounded -= value;
    }

    private void ComponentNullCheck()
    {
        if (m_Image != null && m_NameTmp != null)
            return;

        throw new NullReferenceException($"{name}: Components must be assigned");
    }

    public void Initialize(Node node)
    {
        if (_initialized)
            return;

        ComponentNullCheck();

        _node = node;
        name = _node.GetType().Name;
        _initialized = true;
    }
    #endregion

    #region Interface
    public Image Image => m_Image;
    public TextMeshProUGUI NameText => m_NameTmp;

    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

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

    public void PlaySound(int index)
    {
        if (!_node.OnDeserializing)
        {
            _onSounded?.Invoke(this, new SoundEventArgs(index, _node.Location));
        }
    }

    public void SetSpriteForResourcesPath(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite is null)
        {
            Debug.LogError($"{_node.GetType().Name}: Can't find resource <Sprite>");
            return;
        }

        Image.sprite = sprite;
    }

    public void SetText(string text) => NameText.text = text;
    public void SetFontSize(float size) => NameText.fontSize = size;
    public void SetRectDeltaSize(Vector2 size) => Rect.sizeDelta = size;

    public void SetHighlight(bool highlighted)
    {
        Image.color = highlighted ? HighlightedColor : DefaultColor;
    }
    #endregion
}
