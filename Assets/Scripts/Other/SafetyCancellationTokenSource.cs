using System;
using System.Threading;

public class SafetyCancellationTokenSource : IDisposable
{
    #region Privates
    private readonly CancellationTokenSource _cts;
    private bool _disposed = false;
    private bool _canceled = false;

    private SafetyCancellationTokenSource(CancellationTokenSource cts)
    {
        _cts = cts;
    }

    public SafetyCancellationTokenSource()
    {
        _cts = new CancellationTokenSource();
    }
    #endregion

    #region Interface
    public CancellationToken Token => _disposed ? CancellationToken.None : _cts.Token;
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void Cancel()
    {
        if (_canceled)
            return;

        _cts.Cancel();
        _canceled = true;
    }

    public void Cancel(bool throwOnFirstException)
    {
        if (_canceled)
            return;

        _cts.Cancel(throwOnFirstException);
        _canceled = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try { _cts.Dispose(); } catch { }
        _disposed = true;
    }

    public void CancelAndDispose()
    {
        Cancel();
        Dispose();
    }

    public SafetyCancellationTokenSource CancelAndDisposeAndGetNew()
    {
        CancelAndDispose();
        return new SafetyCancellationTokenSource();
    }

    public CancellationToken CancelAndDisposeAndGetNewToken(out SafetyCancellationTokenSource ctsField)
    {
        ctsField = CancelAndDisposeAndGetNew();
        return ctsField.Token;
    }

    public CancellationTokenRegistration Register(Action callback)
    {
        return _disposed ? default : _cts.Token.Register(callback);
    }

    public static SafetyCancellationTokenSource CreateLinkedTokenSource(CancellationToken token)
    {
        return new SafetyCancellationTokenSource(CancellationTokenSource.CreateLinkedTokenSource(token));
    }
    #endregion
}