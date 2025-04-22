using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using TMPro;
using UnityEngine;

public class ScriptingSupport : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_LogText;
    [SerializeField] private float m_LoggingDuration;

    private SafetyCancellationTokenSource _cts;

    public void Log(string message)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new SafetyCancellationTokenSource();

        ShowLogAsync(message, _cts.Token).Forget();
    }

    public void LogException(Exception e)
    {

    }

    private async UniTaskVoid ShowLogAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            if (m_LogText != null)
            {
                m_LogText.text = message;
                m_LogText.gameObject.SetActive(true);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_LoggingDuration), cancellationToken: cancellationToken);

            if (m_LogText != null)
            {
                m_LogText.gameObject.SetActive(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

}
