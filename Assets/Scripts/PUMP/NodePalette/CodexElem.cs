using System;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodexElem : MonoBehaviour, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private Image m_Image;
    #endregion

    #region Privates
    private string _displayName;
    private bool _caughtException = false;
    private List<RaycastResult> _raycastResults = new();
    private PUMPBackground _background;
    private Node _newNode;
    private const string MANUAL_TEXT_PATH = "Prefab/UI/NodeManual";

    #endregion

    public event Action OnInstantiate;
    public TMPro.TMP_Text TMP_Text;
    public Image Image => m_Image;

    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            m_Text.text = _displayName;
        }
    }
    
    public Type NodeType { get; set; }

    public Node NewNode => _newNode;

    public void OnPointerClick(PointerEventData eventData)
    {
        string path = MANUAL_TEXT_PATH + "/" +_displayName;
        TextAsset textAsset = Resources.Load<TextAsset>(path);

        if (textAsset != null)
        {
            // TextMeshPro에 텍스트 적용
            //if (tmpText != null)
            //{
            TMP_Text.text = textAsset.text;
            //}
            //else
            //{
            //    Debug.LogError("TextMeshPro 컴포넌트가 할당되지 않았습니다!");
            //}
        }
        else
        {
            Debug.LogError($"텍스트 파일을 찾을 수 없습니다: {path}.txt");
        }


    }
}