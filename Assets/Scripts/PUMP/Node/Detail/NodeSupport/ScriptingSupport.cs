using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
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
    [Space(10)] 
    [SerializeField] private RectTransform m_LogPanel;
    [SerializeField] private RecyclableScrollRect m_RecyclableScrollRect;
    #endregion

    #region Privates
    private SafetyCancellationTokenSource _showLogCts;
    private readonly int _maxLogCapacity = 50;
    private readonly List<string> _logQueue = new();
    private Canvas _rootCanvas;
    private RectTransform _rootCanvasRect;
    private bool _scrollRectInit;

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= GetComponent<RectTransform>().GetRootCanvas();
            return _rootCanvas;
        }
    }

    private RectTransform RootCanvasRect
    {
        get
        {
            _rootCanvasRect ??= RootCanvas.GetComponent<RectTransform>();
            return _rootCanvasRect;
        }
    }

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
                await UniTask.Yield(cancellationToken);
                m_LogText.text = message;
                m_LogText.gameObject.SetActive(true);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_LoggingDuration), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            if (m_LogText != null)
            {
                m_LogText.text = string.Empty;
                m_LogText.gameObject.SetActive(false);
            }
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

    private void EnqueueLog(string log)
    {
        if (_logQueue.Count > _maxLogCapacity)
            _logQueue.RemoveAt(_logQueue.Count - 1);

        _logQueue.Insert(0, log);
    }

    int IRecyclableScrollRectDataSource.GetItemCount() => _logQueue.Count;

    void IRecyclableScrollRectDataSource.SetCell(ICell cell, int index)
    {
        if (cell is LoggingElem loggingElem)
        {
            loggingElem.RootCanvas = RootCanvas;
            loggingElem.Index = index;
            loggingElem.Text = _logQueue[index];
        }
    }

    private void OnDestroy()
    {
        RemoveAll();
    }
    #endregion

    public void Print(string value)
    {
        m_PrintText.text = value;
    }

    public void Log(string message)
    {
        _showLogCts?.Cancel();
        _showLogCts?.Dispose();
        _showLogCts = new SafetyCancellationTokenSource();

        ShowLogAsync(message, _showLogCts.Token).Forget();
    }

    public void LogException(Exception e)
    {
        if (TryFilterPythonException(e, out string pythonEx))
        {
            EnqueueLog(pythonEx);
            return;
        }

        Debug.LogException(e);
    }

    public void OpenLoggingPanel()
    {
        m_LogPanel.gameObject.SetActive(true);
        m_LogPanel.SetParent(RootCanvasRect);
        m_LogPanel.SetRectFull();
        RecyclableScrollRect.ReloadData();
    }

    public void CloseLoggingPanel()
    {
        m_LogPanel.gameObject.SetActive(false);
        m_LogPanel.SetParent(transform);
    }

    public void RemoveAll()
    {
        Print(string.Empty);
        _logQueue.Clear();
    }
}