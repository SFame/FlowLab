using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;
using System.Linq;

public static class Loading
{
    #region Privates
    private const string PREFAB_PATH = "StaticUI/LoadingCanvas";
    private const float COMPLETE_WAIT_TIME = 0.1f;
    private static GameObject _prefab;
    private static GameObject _uiGameObject;
    private static ILoadingUi _loadingUi;
    private static CancellationTokenSource _cts;
    private static List<IProgressManagable> _currentProgresses = new();

    private static Pool<IProgressManagable> _progressPool = new
    (
        createFunc: () => new Progress(),
        initSize: 20,
        maxSize: 10000,
        actionOnDestroy: p => p.Terminate()
    );

    private static Pool<IProgressManagable<Task>> _progressTaskPool = new
    (
        createFunc: () => new ProgressTask(),
        initSize: 20,
        maxSize: 5000,
        actionOnDestroy: pt => pt.Terminate()
    );

    private static GameObject Prefab
    {
        get
        {
            _prefab ??= Resources.Load<GameObject>(PREFAB_PATH);
            return _prefab;
        }
    }

    private static GameObject UiGameObject
    {
        get
        {
            _uiGameObject ??= Object.Instantiate(Prefab);
            return _uiGameObject;
        }
    }

    private static ILoadingUi LoadingUi
    {
        get
        {
            if (_loadingUi.IsUnityNull())
            {
                _loadingUi = UiGameObject.GetComponentInChildren<ILoadingUi>();
                if (_loadingUi.IsUnityNull()) // 컴포넌트 삭제된 경우
                {
                    Object.Destroy(_uiGameObject);
                    _uiGameObject = Object.Instantiate(Prefab);
                    _loadingUi = UiGameObject.GetComponentInChildren<ILoadingUi>();
                    if (_loadingUi.IsUnityNull()) // 프리펩 문제
                    {
                        Debug.LogError("Loading UI Prefab is invalid");
                        return null;
                    }
                }
                _loadingUi.Hide();
            }
            return _loadingUi;
        }
    }

    private static void ProgressUpdated()
    {
        int currentProgressAvg = InternalGetProgressesAverage();
        UiUpdate(currentProgressAvg);
        try
        {
            _cts?.Cancel();
        }
        catch { }
        _cts?.Dispose();
        _cts = new();
        CheckProgressCompleteAsync(_cts.Token).Forget();
    }

    private static int InternalGetProgressesAverage()
    {
        int progressCount = _currentProgresses.Count;

        if (progressCount <= 0)
            return 100;

        int progressSum = 0;
        for (int i = 0; i < progressCount; i++)
        {
            progressSum += _currentProgresses[i].GetProgress();
        }

        return progressSum / progressCount;
    }

    private static async UniTask CheckProgressCompleteAsync(CancellationToken cts)
    {
        try
        {
            await UniTask.WaitForSeconds(COMPLETE_WAIT_TIME, true, PlayerLoopTiming.Update, cts);
            if (!cts.IsCancellationRequested && InternalGetProgressesAverage() >= 100)
            {
                Reset();
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void InitProgressManagable(IProgressManagable managable, object tag)
    {
        _currentProgresses.Add(managable);
        managable.ProgressUpdated += ProgressUpdated;
        if (!managable.Initialize(tag))
        {
            throw new InvalidCastException();
        }
    }

    private static void TerminateProgressManagable(IProgressManagable managable)
    {
        managable.Terminate();
        _currentProgresses.Remove(managable);
    }

    private static void Reset()
    {
        foreach (IProgressManagable pm in _currentProgresses.ToList()) // 순회중 Enumerable 변경에 의해 복사본 전달
        {
            TerminateProgressManagable(pm);

            if (pm is Progress progress)
            {
                _progressPool.Release(progress);
            }
            else if (pm is ProgressTask progressTask)
            {
                _progressTaskPool.Release(progressTask);
            }
        }

        _currentProgresses.Clear();
        LoadingUi.Hide();
    }

    private static void UiUpdate(int value)
    {
        float fillValue = value / 100f;
        fillValue = Mathf.Clamp01(fillValue);
        LoadingUi.SliderValue = fillValue;
    }
    #endregion

    #region Interface
    /// <summary>
    /// Progress 객체 Get
    /// </summary>
    /// <returns>Progress 객체</returns>
    public static Progress GetProgress()
    {
        LoadingUi.Show();
        IProgressManagable managable = _progressPool.Get();
        InitProgressManagable(managable, null);
        return managable as Progress;
    }

    /// <summary>
    /// Task 추적 로딩
    /// </summary>
    /// <param name="task">Task</param>
    /// <returns>Copied Task</returns>
    public static Task AddTask(Task task)
    {
        LoadingUi.Show();
        IProgressManagable<Task> progressTask = _progressTaskPool.Get();
        InitProgressManagable(progressTask, task);
        return progressTask.ProcessingObject;
    }

    /// <summary>
    /// Task 추적 로딩 및 결과 await
    /// </summary>
    /// <typeparam name="T">Task result type</typeparam>
    /// <param name="task">Task</param>
    /// <returns>Copied Task</returns>
    public static Task<T> AddTask<T>(Task<T> task)
    {
        LoadingUi.Show();
        IProgressManagable<Task<T>> progressTask = new ProgressTask<T>();
        InitProgressManagable(progressTask, task);
        return progressTask.ProcessingObject;
    }

    /// <summary>
    /// UniTask 추적 로딩
    /// </summary>
    /// <param name="task">UniTask</param>
    /// <returns>Copied UniTask</returns>
    public static UniTask AddTask(UniTask task)
    {
        return AddTask(task.AsTask()).AsUniTask();
    }

    /// <summary>
    /// UniTask 추적 로딩 및 결과 await
    /// </summary>
    /// <typeparam name="T">UniTask result type</typeparam>
    /// <param name="task">UniTask</param>
    /// <returns>Copied UniTask</returns>
    public static UniTask<T> AddTask<T>(UniTask<T> task)
    {
        return AddTask(task.AsTask()).AsUniTask();
    }

    /// <summary>
    /// 작업 진행도 전체 평균
    /// </summary>
    /// <returns></returns>
    public static int GetProgressesAverage()
    {
        return InternalGetProgressesAverage();
    }

    /// <summary>
    /// 작업 개수
    /// </summary>
    /// <returns></returns>
    public static int GetProgressesCount()
    {
        return _currentProgresses.Count;
    }

    /// <summary>
    /// 로딩 강제종료
    /// </summary>
    public static void ForceReset()
    {
        Reset();
    }
    #endregion

    #region Progress Class
    public sealed class Progress : IProgressManagable
    {
        #region Manage Only
        private int _progress = 0;
        private Action _progressUpdated;

        event Action IProgressManagable.ProgressUpdated
        {
            add => _progressUpdated += value;
            remove => _progressUpdated -= value;
        }

        int IProgressManagable.GetProgress()
        {
            return _progress;
        }

        bool IProgressManagable.Initialize(object _)
        {
            _progress = 0;
            _progressUpdated?.Invoke();
            return true;
        }

        void IProgressManagable.Terminate()
        {
            _progress = 0;
            _progressUpdated = null;
        }
        #endregion

        #region Interface
        /// <summary>
        /// 작업 상태에 따라 0 ~ 100사이의 값 진행 중 업데이트
        /// </summary>
        /// <param name="progress"></param>
        public void SetProgress(int progress)
        {
            _progress = progress.Clamp(0, 100);
            _progressUpdated?.Invoke();
        }

        /// <summary>
        /// 작업 완료 보고
        /// (SetProgress(100)과 동일)
        /// </summary>
        public void SetComplete()
        {
            SetProgress(100);
        }
        #endregion

        #region Operator Overloading
        public static Progress operator +(Progress a, int b)
        {
            a.SetProgress(a._progress + b);
            return a;
        }

        public static Progress operator -(Progress a, int b)
        {
            a.SetProgress(a._progress - b);
            return a;
        }

        public static Progress operator ++(Progress a)
        {
            a.SetProgress(a._progress + 1);
            return a;
        }

        public static Progress operator --(Progress a)
        {
            a.SetProgress(a._progress - 1);
            return a;
        }
        #endregion
    }

    private abstract class ProgressTaskBase : IProgressManagable
    {
        protected CancellationTokenSource _cts;
        protected int _progress = 0;
        public event Action ProgressUpdated;

        public abstract bool Initialize(object tag);

        public int GetProgress()
        {
            return _progress;
        }

        public void Terminate()
        {
            _progress = 0;
            ProgressUpdated = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        protected void InvokeProgressUpdated()
        {
            ProgressUpdated?.Invoke();
        }
    }

    private sealed class ProgressTask : ProgressTaskBase, IProgressManagable<Task>
    {
        public Task ProcessingObject { get; private set; }

        public override bool Initialize(object tag)
        {
            if (tag is Task task)
            {
                try
                {
                    _cts?.Cancel();
                }
                catch { }
                _cts?.Dispose();
                _cts = new();
                InvokeProgressUpdated();

                Task observeTask = task.ContinueWith(_ => // 스레드 풀에서 Run 주의
                {
                    _progress = 100;
                    UniTask.Post(() => InvokeProgressUpdated());
                },
                _cts.Token);

                TaskCompletionSource<bool> tcs = new();
                MonitorTask(task, observeTask, tcs).Forget();
                ProcessingObject = tcs.Task;
                return true;
            }
            return false;
        }

        private async UniTaskVoid MonitorTask(Task originalTask, Task observeTask, TaskCompletionSource<bool> tcs)
        {
            try
            {
                await Task.WhenAll(originalTask, observeTask);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                if (originalTask.IsCompletedSuccessfully)
                {
                    tcs.SetResult(true);
                }
                else if (originalTask.IsFaulted)
                {
                    tcs.SetException(originalTask.Exception ?? ex);
                }
                else if (originalTask.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else if (!originalTask.IsCompleted)
                {
                    try
                    {
                        await originalTask;
                        tcs.SetResult(true);
                    }
                    catch (Exception inEx)
                    {
                        if (originalTask.IsCompletedSuccessfully)
                        {
                            tcs.SetResult(true);
                        }
                        else if (originalTask.IsCanceled)
                        {
                            tcs.SetCanceled();
                        }
                        else if (originalTask.IsFaulted)
                        {
                            tcs.SetException(inEx);
                        }
                        else
                        {
                            throw inEx;
                        }
                    }
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    private sealed class ProgressTask<T> : ProgressTaskBase, IProgressManagable<Task<T>>
    {
        public Task<T> ProcessingObject { get; private set; }

        public override bool Initialize(object tag)
        {
            if (tag is Task<T> task)
            {
                try
                {
                    _cts?.Cancel();
                }
                catch { }
                _cts?.Dispose();
                _cts = new();
                InvokeProgressUpdated();

                Task observeTask = task.ContinueWith(_ => // 스레드 풀에서 Run 주의
                {
                    _progress = 100;
                    UniTask.Post(() => InvokeProgressUpdated());
                },
                _cts.Token);

                TaskCompletionSource<T> tcs = new();
                MonitorTask(task, observeTask, tcs).Forget();
                ProcessingObject = tcs.Task;
                return true;
            }
            return false;
        }

        private async UniTaskVoid MonitorTask(Task<T> originalTask, Task observeTask, TaskCompletionSource<T> tcs)
        {
            try
            {
                await Task.WhenAll(originalTask, observeTask);
                tcs.SetResult(await originalTask);
            }
            catch (Exception ex)
            {
                if (originalTask.IsCompletedSuccessfully)
                {
                    tcs.SetResult(await originalTask);
                }
                else if (originalTask.IsFaulted)
                {
                    tcs.SetException(originalTask.Exception ?? ex);
                }
                else if (originalTask.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else if (!originalTask.IsCompleted)
                {
                    try
                    {
                        await originalTask;
                        tcs.SetResult(await originalTask);
                    }
                    catch (Exception inEx)
                    {
                        if (originalTask.IsCompletedSuccessfully)
                        {
                            tcs.SetResult(await originalTask);
                        }
                        else if (originalTask.IsCanceled)
                        {
                            tcs.SetCanceled();
                        }
                        else if (originalTask.IsFaulted)
                        {
                            tcs.SetException(inEx);
                        }
                        else
                        {
                            throw inEx;
                        }
                    }
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    private interface IProgressManagable
    {
        event Action ProgressUpdated;
        int GetProgress();
        bool Initialize(object tag);
        void Terminate();
    }

    private interface IProgressManagable<T> : IProgressManagable
    {
        T ProcessingObject { get; }
    }
    #endregion
}