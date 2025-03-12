using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextButton : MonoBehaviour
{
    #region OnInspector
    
    [SerializeField] private Button _button;

    [SerializeField] private TextMeshProUGUI _text;
    #endregion
    
    #region Privates

    private bool _initialized = false;
    private List<Action> _onClick = new();

    private void OnClickInvoke()
    {
        for (int i = 0; i < _onClick.Count; ++i)
            _onClick[i]?.Invoke();
        
        _onClick.Clear();
    }
    #endregion
    
    public event Action OnClick
    {
        add => _onClick.Add(value);
        remove => _onClick.Remove(value);
    }

    public string Text
    {
        get => _text.text;
        set => _text.text = value;
    }
    
    public int SiblingIndex => transform.GetSiblingIndex();
    
    public void SetActive(bool active) => gameObject.SetActive(active);

    /// <summary>
    /// 반드시 우선 호출
    /// </summary>
    public void Initialize()
    {
        if (!_initialized)
            _button.onClick.AddListener(OnClickInvoke);

        Text = "";
        _onClick.Clear();
        _initialized = true;
    }

    public void Terminate()
    {
        Text = "";
        _onClick.Clear();
    }
}
