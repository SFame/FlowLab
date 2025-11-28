using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using Utils;


public class ScriptingSupport : MonoBehaviour, IRecyclableScrollRectDataSource
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_PrintText;
    [SerializeField] private TextMeshProUGUI m_LogText;
    [SerializeField] private float m_LoggingDuration;
    [SerializeField] private RectTransform m_PythonLogoRect;
    [SerializeField] private TextMeshProUGUI m_FileNameText;
    [Space(10)] 
    [SerializeField] private RectTransform m_LogPanel;
    [SerializeField] private RecyclableScrollRect m_RecyclableScrollRect;
    [Space(10)]
    [SerializeField] private GameObject m_LoadingObject;
    [SerializeField] private Animator m_LoadingAnimator;
    #endregion

    #region Privates
    private SafetyCancellationTokenSource _showLogCts;
    private const string NULL_FILE_NAME = "----------";
    private readonly int _maxLogCapacity = 50;
    private readonly List<string> _logQueue = new();
    private bool _scrollRectInit;
    private static readonly int AnimatorActive = Animator.StringToHash("Activate");


    private RecyclableScrollRect RecyclableScrollRect
    {
        get
        {
            if (!_scrollRectInit)
            {
                _scrollRectInit = true;
                m_RecyclableScrollRect.Initialize(this);
            }

            return m_RecyclableScrollRect;
        }
    }

    private async UniTaskVoid ShowLogAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            if (m_LogText != null)
            {
                await UniTask.WaitForEndOfFrame(cancellationToken);
                m_LogText.text = message;
                m_LogText.gameObject.SetActive(true);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_LoggingDuration), cancellationToken: cancellationToken);
            RemoveLog();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void RemoveLog()
    {
        if (m_LogText != null)
        {
            m_LogText.text = string.Empty;
            m_LogText.gameObject.SetActive(false);
        }
    }

    private bool TryFilterPythonException(Exception exception, out string pythonEx)
    {
        pythonEx = string.Empty;

        if (exception == null)
            return false;

        bool isPythonException = (exception.GetType().Namespace?.Contains("IronPython.Runtime") ?? false) ||
                                 (exception.InnerException?.GetType().Namespace?.Contains("IronPython.Runtime") ?? false);

        if (!isPythonException)
        {
            string fullMessage = exception.ToString();
            isPythonException = fullMessage.Contains("IronPython.Runtime");
        }

        if (isPythonException)
        {
            string fullMessage = exception.ToString();
            int newLineIndex = fullMessage.IndexOf('\n');

            if (newLineIndex > 0)
            {
                pythonEx = fullMessage.Substring(0, newLineIndex).Trim();
            }
            else
            {
                pythonEx = exception.Message;
            }

            const string prefix = "IronPython.Runtime.";
            if (pythonEx.StartsWith(prefix))
            {
                pythonEx = pythonEx.Substring(prefix.Length);
            }

            return true;
        }

        return false;
    }

    private bool TryFilterMicrosoftScriptingException(Exception exception, out string scriptingEx)
    {
        scriptingEx = string.Empty;

        if (exception == null)
            return false;

        bool isMicrosoftScriptingException = (exception.GetType().Namespace?.Contains("Microsoft.Scripting") ?? false) ||
                                             (exception.InnerException?.GetType().Namespace?.Contains("Microsoft.Scripting") ?? false);

        if (!isMicrosoftScriptingException)
        {
            string fullMessage = exception.ToString();
            isMicrosoftScriptingException = fullMessage.Contains("Microsoft.Scripting");
        }

        if (isMicrosoftScriptingException)
        {
            string fullMessage = exception.ToString();
            int newLineIndex = fullMessage.IndexOf('\n');

            if (newLineIndex > 0)
            {
                scriptingEx = fullMessage.Substring(0, newLineIndex).Trim();
            }
            else
            {
                scriptingEx = exception.Message;
            }

            const string prefix = "Microsoft.Scripting.";
            if (scriptingEx.StartsWith(prefix))
            {
                scriptingEx = scriptingEx.Substring(prefix.Length);
            }

            return true;
        }

        return false;
    }

    private void EnqueueLog(string log)
    {
        if (_logQueue.Count > _maxLogCapacity)
        {
            _logQueue.RemoveAt(_logQueue.Count - 1);
        }

        _logQueue.Insert(0, log);
    }

    int IRecyclableScrollRectDataSource.GetItemCount() => _logQueue.Count;

    void IRecyclableScrollRectDataSource.SetCell(ICell cell, int index)
    {
        if (cell is LoggingElem loggingElem)
        {
            loggingElem.Index = index;
            loggingElem.Text = _logQueue[index];
        }
    }

    private void OnDestroy()
    {
        RemoveAllLog();
    }
    #endregion

    #region Interface
    public void Initialize()
    {
        RemoveFileName();
    }

    /// <summary>
    /// 일반 디스플레이
    /// </summary>
    public void Print(string value)
    {
        string escapedValue = value.Replace("<", "&lt;").Replace(">", "&gt;");
        m_PrintText.text = escapedValue;
        EnqueueLog($"<color=black>{escapedValue}</color>");
    }

    /// <summary>
    /// 빨강 텍스트 경고 디스플레이 (로깅 큐에는 들어가지 않음)
    /// </summary>
    public void Log(string message)
    {
        _showLogCts?.CancelAndDispose();
        _showLogCts = new SafetyCancellationTokenSource(false);
        RemoveLog();

        ShowLogAsync(message, _showLogCts.Token).Forget();
    }

    public void LogException(Exception e)
    {
        if (TryFilterPythonException(e, out string pythonEx))
        {
            string escapedString = pythonEx.Replace("<", "&lt;").Replace(">", "&gt;");
            EnqueueLog($"<color=red>{escapedString}</color>");
            return;
        }

        if (TryFilterMicrosoftScriptingException(e, out string scriptingEx))
        {
            string escapedString = scriptingEx.Replace("<", "&lt;").Replace(">", "&gt;");
            EnqueueLog($"<color=red>{escapedString}</color>");
            return;
        }

        Debug.LogWarning("<---- Python Error ---->");
        Debug.LogException(e);
        Debug.LogWarning("<---------------------->");
    }

    public void SetLoadingAnimation(bool show)
    {
        if (show)
        {
            m_LoadingObject.SetActive(true);
            m_LoadingAnimator.SetBool(AnimatorActive, true);
            return;
        }
        m_LoadingAnimator.SetBool(AnimatorActive, false);
        m_LoadingObject.SetActive(false);
    }

    public void ShowFileName(string fileName)
    {
        m_PythonLogoRect.gameObject.SetActive(true);
        m_FileNameText.text = fileName;
    }

    public void RemoveFileName()
    {
        m_PythonLogoRect.gameObject.SetActive(false);
        m_FileNameText.text = NULL_FILE_NAME;
    }

    public void OpenLoggingPanel()
    {
        m_LogPanel.gameObject.SetActive(true);
        m_LogPanel.SetParent(PUMPUiManager.RootCanvasRect);
        m_LogPanel.SetRectFull();
        RecyclableScrollRect.ReloadData();
    }

    public void CloseLoggingPanel()
    {
        m_LogPanel.gameObject.SetActive(false);
        m_LogPanel.SetParent(transform);
    }

    public void RemoveAllLog()
    {
        Print(string.Empty);
        _logQueue.Clear();
    }
    #endregion
}