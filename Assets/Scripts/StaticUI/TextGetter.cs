using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextGetter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TMP_InputField m_InputField;
    [SerializeField] private Button m_ConfirmButton;
    [SerializeField] private ClosablePanel m_ClosablePanel;

    #region Privates
    private const KeyCode CONFIRM_KEY = KeyCode.Return;
    private Action<string> _callback;
    private Action _onExitCallback;
    private bool _initialized = false;
    private CancellationTokenSource _cts;
    private RectTransform _rect;
    #endregion

    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    public event Action OnExit;

    public void Set(string titleString, string inputString, Action<string> callback, Action onExitCallback)
    {
        gameObject.SetActive(true);
        Initialize();
        Terminate();

        m_TitleText.text = titleString;
        m_InputField.text = inputString;
        _callback = callback;
        _onExitCallback = onExitCallback;
        
        m_InputField.Select();
        m_InputField.ActivateInputField();
        
        _cts = new CancellationTokenSource();
        WaitKeyAsync(_cts.Token).Forget();
    }

    #region Privates
    private void SendWithExit()
    {
        _callback?.Invoke(m_InputField.text);
        Exit();
    }

    private void Exit()
    {
        _onExitCallback?.Invoke();
        Terminate();
        gameObject.SetActive(false);
        OnExit?.Invoke();
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        m_ClosablePanel.OnClose += Exit;
        m_ConfirmButton.onClick.AddListener(SendWithExit);
    }
    
    private void Terminate()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        m_TitleText.text = "";
        m_InputField.text = "";
        _callback = null;
        _onExitCallback = null;
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
    #endregion
}