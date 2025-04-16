using System;
using System.Collections.Generic;

public class UndoDelegate<T>
{
    #region Privates
    private Func<T> _recordGetter;
    private Action<T> _onUndo;
    private Action<T> _onRedo;
    private Action<T> _onClear;

    private int _currentRecordedIndex = -1;
    private readonly int _maxCapacity;

    private List<T> _recorded = new();
    private T _lateRecord;

    private void Push(T record)
    {
        if (_currentRecordedIndex >= 0)
        {
            int removeCount = _recorded.Count - (_currentRecordedIndex + 1);
            if (removeCount > 0)
                _recorded.RemoveRange(_currentRecordedIndex + 1, removeCount);
        }

        while (_recorded.Count >= _maxCapacity)
        {
            _recorded.RemoveAt(0);
            _currentRecordedIndex--;
        }

        _recorded.Add(record);
        _currentRecordedIndex = Math.Clamp(++_currentRecordedIndex, -1, _recorded.Count - 1);
    }

    private bool Pop(bool moveLeft, out T result)
    {
        int nextIndex = moveLeft ? _currentRecordedIndex - 1 : _currentRecordedIndex + 1;

        if (nextIndex >= 0 && nextIndex < _recorded.Count)
        {
            _currentRecordedIndex = nextIndex;
            result = _recorded[_currentRecordedIndex];
            return true;
        }

        result = default;
        return false;
    }
    #endregion

    #region Public
    /// <summary>
    /// Undo/Redo
    /// </summary>
    /// <param name="recordGetter">Function that retrieves the current state when Record() is called</param>
    /// <param name="onUndo">Action that is invoked when Undo() is called, with the previous state as parameter</param>
    /// <param name="maxCapacity">Maximum number of states to keep in history</param>
    /// <param name="onRedo">Action that is invoked when Redo() is called, with the next state as parameter. Defaults to onUndo if not provided</param>
    /// <param name="onClear">Optional action that is invoked for each recorded state when Clear() is called</param>
    /// <exception cref="ArgumentNullException">Thrown when recordGetter or onUndo is null</exception>
    public UndoDelegate(Func<T> recordGetter, Action<T> onUndo, int maxCapacity = 10, Action<T> onRedo = null, Action<T> onClear = null)
    {
        if (recordGetter == null || onUndo == null)
        {
            throw new ArgumentNullException($"{GetType().Name}: Required parameter returned null (recordGetter: [{recordGetter}] / onUndo[{onUndo}]");
        }

        _recordGetter = recordGetter;
        _onUndo = onUndo;
        _maxCapacity = maxCapacity;
        _onRedo = onRedo ?? onUndo;
        _onClear = onClear;
    }

    public bool RecordAfterClear { get; set; }

    public void Record()
    {
        _lateRecord = _recordGetter.Invoke();
        Push(_lateRecord);
    }

    public bool Undo()
    {
        if (Pop(true, out T popResult))
        {
            _lateRecord = popResult;
            _onUndo.Invoke(popResult);
            return true;
        }

        return false;
    }

    public bool Redo()
    {
        if (Pop(false, out T popResult))
        {
            _lateRecord = popResult;
            _onRedo.Invoke(popResult);
            return true;
        }

        return false;
    }

    public void Clear()
    {
        if (_onClear != null)
        {
            foreach (T record in _recorded)
            {
                _onClear.Invoke(record);
            }
        }

        _recorded.Clear();
        _currentRecordedIndex = -1;

        if (RecordAfterClear)
            Record();
    }
    #endregion
}