using System;
using System.Threading;

/// <summary>
/// Specifies the thread safety mode for SafetyCancellationTokenSource
/// </summary>
public enum SctsThreadMode
{
    /// <summary>
    /// No thread safety - optimized for single-threaded scenarios
    /// </summary>
    None,

    /// <summary>
    /// Thread-safe operations with internal locking
    /// </summary>
    ThreadSafe
}

/// <summary>
/// CancellationTokenSource always drives me crazy with its Dispose management.
/// I dedicate this to everyone who shares the same frustration.
///
/// <![CDATA[
/// var cts = new SafetyCancellationTokenSource();
/// await DoWorkAsync(cts.Token);
///
/// // Example 1: Basic usage
/// cts.Cancel();
/// cts.Dispose();
/// 
/// // Example 2: Cancel and dispose together
/// cts.CancelAndDispose();
///
/// // Example 3: Reset pattern
/// var newCts = cts.CancelAndDisposeAndGetNew();
///
/// // Example 4: Reset and use new token in one line
/// await DoWorkAsync(_cts.CancelAndDisposeAndGetNewToken(out _cts));
/// ]]>
/// </summary>
public class SafetyCancellationTokenSource : IDisposable
{
    #region Privates
    private readonly CancellationTokenSource _cts;
    private bool _disposed = false;
    private bool _canceled = false;
    private readonly object? _lock = null;

    private SafetyCancellationTokenSource(CancellationTokenSource cts, bool threadSafe)
    {
        _cts = cts;
        _lock = threadSafe ? new object() : null;
    }

    private void InternalCancel()
    {
        if (_canceled || _cts.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _cts.Cancel();
            _canceled = true;
        }
        catch { }
    }

    private void InternalCancel(bool throwOnFirstException)
    {
        if (_canceled || _cts.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _cts.Cancel(throwOnFirstException);
            _canceled = true;
        }
        catch { }
    }

    private void InternalDispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _cts.Dispose();
        }
        catch { }
        _disposed = true;
    }
    #endregion

    #region Interface
    /// <summary>
    /// Standard constructor
    /// </summary>
    public SafetyCancellationTokenSource(bool threadSafe = true) : this(new CancellationTokenSource(), threadSafe) { }

    public SafetyCancellationTokenSource(SctsThreadMode threadMode) : this(new CancellationTokenSource(), threadMode == SctsThreadMode.ThreadSafe) { }

    /// <summary>
    /// Gets the CancellationToken
    /// </summary>
    public CancellationToken Token
    {
        get
        {
            if (_lock == null)
            {
                return _disposed ? CancellationToken.None : _cts.Token;
            }

            lock (_lock)
            {
                return _disposed ? CancellationToken.None : _cts.Token;
            }
        }
    }

    /// <summary>
    /// Checks if cancellation has been requested
    /// </summary>
    public bool IsCancellationRequested
    {
        get
        {
            if (_lock == null)
            {
                return _cts.IsCancellationRequested;
            }

            lock (_lock)
            {
                return _cts.IsCancellationRequested;
            }
        }
    }

    /// <summary>
    /// Standard Cancel method
    /// </summary>
    public void Cancel()
    {
        if (_lock == null)
        {
            InternalCancel();
            return;
        }

        lock (_lock)
        {
            InternalCancel();
        }
    }

    /// <summary>
    /// Cancel with callback exception handling behavior
    /// </summary>
    /// <param name="throwOnFirstException">If true, throws immediately on first exception and stops; if false, executes all callbacks then throws all exceptions</param>
    public void Cancel(bool throwOnFirstException)
    {
        if (_lock == null)
        {
            InternalCancel(throwOnFirstException);
            return;
        }

        lock (_lock)
        {
            InternalCancel(throwOnFirstException);
        }
    }

    /// <summary>
    /// Standard Dispose method
    /// </summary>
    public void Dispose()
    {
        if (_lock == null)
        {
            InternalDispose();
            return;
        }

        lock (_lock)
        {
            InternalDispose();
        }
    }

    /// <summary>
    /// Cancels and disposes simultaneously
    /// </summary>
    public void CancelAndDispose()
    {
        Cancel();
        Dispose();
    }

    /// <summary>
    /// Cancels and disposes simultaneously with the same callback exception handling as Cancel(bool throwOnFirstException)
    /// </summary>
    /// <param name="throwOnFirstException">If true, throws immediately on first exception and stops; if false, executes all callbacks then throws all exceptions</param>
    public void CancelAndDispose(bool throwOnFirstException)
    {
        Cancel(throwOnFirstException);
        Dispose();
    }

    /// <summary>
    /// Cancels and disposes simultaneously, then returns a new SafetyCancellationTokenSource instance
    /// </summary>
    /// <returns>A new SafetyCancellationTokenSource instance</returns>
    public SafetyCancellationTokenSource CancelAndDisposeAndGetNew()
    {
        CancelAndDispose();
        return new SafetyCancellationTokenSource(_lock != null);
    }

    /// <summary>
    /// Cancels and disposes simultaneously, then returns a new SafetyCancellationTokenSource instance
    /// with the same callback exception handling as Cancel(bool throwOnFirstException)
    /// </summary>
    /// <param name="throwOnFirstException">If true, throws immediately on first exception and stops; if false, executes all callbacks then throws all exceptions</param>
    /// <returns>A new SafetyCancellationTokenSource instance</returns>
    public SafetyCancellationTokenSource CancelAndDisposeAndGetNew(bool throwOnFirstException)
    {
        CancelAndDispose(throwOnFirstException);
        return new SafetyCancellationTokenSource(_lock != null);
    }

    /// <summary>
    /// Cancels and disposes simultaneously, then returns the Token of a new SafetyCancellationTokenSource instance.
    /// The new SafetyCancellationTokenSource instance can be retrieved via the out parameter
    /// </summary>
    /// <param name="ctsField">Out field to receive the new SafetyCancellationTokenSource instance</param>
    /// <returns>Token of the new SafetyCancellationTokenSource instance</returns>
    public CancellationToken CancelAndDisposeAndGetNewToken(out SafetyCancellationTokenSource ctsField)
    {
        ctsField = CancelAndDisposeAndGetNew();
        return ctsField.Token;
    }

    /// <summary>
    /// Cancels and disposes simultaneously, then returns the Token of a new SafetyCancellationTokenSource instance.
    /// The new SafetyCancellationTokenSource instance can be retrieved via the out parameter.
    /// Uses the same callback exception handling as Cancel(bool throwOnFirstException)
    /// </summary>
    /// <param name="ctsField">Out field to receive the new SafetyCancellationTokenSource instance</param>
    /// <param name="throwOnFirstException">If true, throws immediately on first exception and stops; if false, executes all callbacks then throws all exceptions</param>
    /// <returns>Token of the new SafetyCancellationTokenSource instance</returns>
    public CancellationToken CancelAndDisposeAndGetNewToken(out SafetyCancellationTokenSource ctsField, bool throwOnFirstException)
    {
        ctsField = CancelAndDisposeAndGetNew(throwOnFirstException);
        return ctsField.Token;
    }

    /// <summary>
    /// Creates a LinkedTokenSource by linking multiple CancellationTokens
    /// </summary>
    /// <param name="tokens">Tokens to link</param>
    /// <returns>A new SafetyCancellationTokenSource instance with linked tokens</returns>
    public static SafetyCancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
    {
        return new SafetyCancellationTokenSource(CancellationTokenSource.CreateLinkedTokenSource(tokens), true);
    }

    /// <summary>
    /// Creates a LinkedTokenSource by linking multiple CancellationTokens
    /// </summary>
    /// <param name="threadSafe">Enable thread-safe operations</param>
    /// <param name="tokens">Tokens to link</param>
    /// <returns>A new SafetyCancellationTokenSource instance with linked tokens</returns>
    public static SafetyCancellationTokenSource CreateLinkedTokenSource(bool threadSafe, params CancellationToken[] tokens)
    {
        return new SafetyCancellationTokenSource(CancellationTokenSource.CreateLinkedTokenSource(tokens), threadSafe);
    }

    /// <summary>
    /// Creates a LinkedTokenSource by linking multiple CancellationTokens
    /// </summary>
    /// <param name="threadSafe">Enable thread-safe operations</param>
    /// <param name="tokens">Tokens to link</param>
    /// <returns>A new SafetyCancellationTokenSource instance with linked tokens</returns>
    public static SafetyCancellationTokenSource CreateLinkedTokenSource(SctsThreadMode threadMode, params CancellationToken[] tokens)
    {
        return new SafetyCancellationTokenSource(CancellationTokenSource.CreateLinkedTokenSource(tokens), threadMode == SctsThreadMode.ThreadSafe);
    }
    #endregion
}

/// <summary>
/// Extension methods for those who don't want to deal with Dispose and null checks.
/// All methods safely handle null instances - just add "Safe" prefix to any method name.
/// 
/// <![CDATA[
/// SafetyCancellationTokenSource _cts = null;
/// 
/// // Works even if _cts is null - no exceptions thrown
/// _cts.SafeCancel();
/// _cts.SafeDispose();
/// _cts.SafeCancelAndDispose();
/// 
/// // Creates new instance if _cts is null
/// _cts = _cts.SafeCancelAndDisposeAndGetNew();
/// 
/// // Cancel old, get new token, and use it - all in one line
/// await DoWorkAsync(_cts.SafeCancelAndDisposeAndGetNewToken(out _cts));
/// ]]>
public static class SafetyCancellationTokenSourceExtensions
{
    public static void SafeCancel(this SafetyCancellationTokenSource scts)
    {
        scts?.Cancel();
    }

    public static void SafeCancel(this SafetyCancellationTokenSource scts, bool throwOnFirstException)
    {
        scts?.Cancel(throwOnFirstException);
    }

    public static void SafeDispose(this SafetyCancellationTokenSource scts)
    {
        scts?.Dispose();
    }

    public static void SafeCancelAndDispose(this SafetyCancellationTokenSource scts)
    {
        scts?.CancelAndDispose();
    }

    public static void SafeCancelAndDispose(this SafetyCancellationTokenSource scts, bool throwOnFirstException)
    {
        scts?.CancelAndDispose(throwOnFirstException);
    }

    public static SafetyCancellationTokenSource SafeCancelAndDisposeAndGetNew(this SafetyCancellationTokenSource scts)
    {
        if (scts == null)
        {
            return new SafetyCancellationTokenSource(true);
        }

        return scts.CancelAndDisposeAndGetNew();
    }

    public static SafetyCancellationTokenSource SafeCancelAndDisposeAndGetNew(this SafetyCancellationTokenSource scts, SctsThreadMode threadMode)
    {
        if (scts == null)
        {
            return new SafetyCancellationTokenSource(threadMode);
        }

        return scts.CancelAndDisposeAndGetNew();
    }

    public static SafetyCancellationTokenSource SafeCancelAndDisposeAndGetNew(this SafetyCancellationTokenSource scts, bool throwOnFirstException)
    {
        if (scts == null)
        {
            return new SafetyCancellationTokenSource(true);
        }

        return scts.CancelAndDisposeAndGetNew(throwOnFirstException);
    }

    public static SafetyCancellationTokenSource SafeCancelAndDisposeAndGetNew(
        this SafetyCancellationTokenSource scts,
        bool throwOnFirstException,
        SctsThreadMode threadMode)
    {
        if (scts == null)
        {
            return new SafetyCancellationTokenSource(threadMode);
        }

        return scts.CancelAndDisposeAndGetNew(throwOnFirstException);
    }

    public static CancellationToken SafeCancelAndDisposeAndGetNewToken(this SafetyCancellationTokenSource scts, out SafetyCancellationTokenSource newScts)
    {
        if (scts == null)
        {
            newScts = new SafetyCancellationTokenSource(true);
            return newScts.Token;
        }

        return scts.CancelAndDisposeAndGetNewToken(out newScts);
    }

    public static CancellationToken SafeCancelAndDisposeAndGetNewToken(
        this SafetyCancellationTokenSource scts,
        out SafetyCancellationTokenSource newScts,
        SctsThreadMode threadMode)
    {
        if (scts == null)
        {
            newScts = new SafetyCancellationTokenSource(threadMode);
            return newScts.Token;
        }

        return scts.CancelAndDisposeAndGetNewToken(out newScts);
    }

    public static CancellationToken SafeCancelAndDisposeAndGetNewToken(
        this SafetyCancellationTokenSource scts,
        out SafetyCancellationTokenSource newScts,
        bool throwOnFirstException)
    {
        if (scts == null)
        {
            newScts = new SafetyCancellationTokenSource(true);
            return newScts.Token;
        }

        return scts.CancelAndDisposeAndGetNewToken(out newScts, throwOnFirstException);
    }

    public static CancellationToken SafeCancelAndDisposeAndGetNewToken(
        this SafetyCancellationTokenSource scts,
        out SafetyCancellationTokenSource newScts,
        bool throwOnFirstException,
        SctsThreadMode threadMode)
    {
        if (scts == null)
        {
            newScts = new SafetyCancellationTokenSource(threadMode);
            return newScts.Token;
        }

        return scts.CancelAndDisposeAndGetNewToken(out newScts, throwOnFirstException);
    }
}