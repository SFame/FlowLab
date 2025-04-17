using System;
using System.Threading;

public class SafetyCancellationTokenSource : IDisposable
{
    private CancellationTokenSource _cts = new();
    private bool _disposed = false;
    private bool _canceled = false;
    public CancellationToken Token => _disposed ? CancellationToken.None : _cts.Token;
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void Dispose()
    {
        if (_disposed)
            return;

        _cts.Dispose();
        _disposed = true;
    }

    public void Cancel()
    {
        if (_canceled)
            return;

        _cts.Cancel();
        _canceled = true;
    }

    public void CancelAndDispose()
    {
        Cancel();
        Dispose();
    }
}