using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class LoggingElem : MonoBehaviour, ICell, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_IndexText;
    [SerializeField] private TextMeshProUGUI m_Text;
    #endregion

    #region Privates
    private string _text;
    private int _index;
    private Canvas _rootCanvas;

    private ContextElement[] ContextElements =>
        new[]
        {
            new ContextElement("Copy", CopyText)
        };

    private void CopyText()
    {
        string log = Other.RemoveRichTextTags(Text);
        GUIUtility.systemCopyBuffer = log;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ContextMenuManager.ShowContextMenu(PUMPUiManager.RootCanvas, eventData.position, ContextElements);
        }
    }
    #endregion

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            m_Text.text = value;
        }
    }

    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            m_IndexText.text = $"[{_index.ToString()}]";
        }
    }
}