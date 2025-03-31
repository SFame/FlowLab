using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextGetter : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;

    #region Privates
    private const KeyCode CONFIRM_KEY = KeyCode.Return;
    private Action<string> _callback;
    private bool _initialized = false;
    private CancellationTokenSource _cts;
    private RectTransform _rect;
    #endregion

    public bool IsObjectNull => gameObject == null;

    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    public event Action OnExit;

    public void Set(string titleString, string inputString, Action<string> callback)
    {
        gameObject.SetActive(true);
        Initialize();
        Terminate();

        titleText.text = titleString;
        inputField.text = inputString;
        _callback = callback;
        
        inputField.Select();
        inputField.ActivateInputField();
        
        _cts = new CancellationTokenSource();
        WaitKeyAsync(_cts.Token).Forget();
    }

    #region Privates
    private void SendWithExit()
    {
        _callback?.Invoke(inputField.text);
        Exit();
    }

    private void Exit()
    {
        Terminate();
        gameObject.SetActive(false);
        OnExit?.Invoke();
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        confirmButton.onClick.AddListener(SendWithExit);
        
        _initialized = true;
    }
    
    private void Terminate()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        titleText.text = "";
        inputField.text = "";
        _callback = null;
    }

    private async UniTaskVoid WaitKeyAsync(CancellationToken token)
    {
        try
        {
            await UniTask.WaitUntil(() => Input.GetKeyDown(CONFIRM_KEY), PlayerLoopTiming.Update, token);
            SendWithExit();
        }
        catch (OperationCanceledException) { }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);
        
        if (result.Count <= 0)
            return;

        if (result[0].gameObject == gameObject)
            Exit();
    }
    #endregion
}
