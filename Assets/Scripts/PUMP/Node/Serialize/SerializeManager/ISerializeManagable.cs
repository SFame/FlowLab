using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISerializeManagable
{
    PairEvent OnDataUpdated { get; }
    Task AddData(string path, object serializeObject);
    Task<List<object>> GetDatas(string fileName);
}

public interface ISerializeManagable<T> : ISerializeManagable
{
    Task AddData(string path, T serializeObject);
    new Task<List<T>> GetDatas(string fileName);
}

public class PairEvent : IPairEventInvokable
{
    /// <summary>
    /// Key: 파일명
    /// Value: 해당 파일명의 세이브에 변경이 있을 시 호출될 Action
    /// </summary>
    private Dictionary<string, Action> Events { get; } = new();

    public void AddEvent(string key, Action action, string caller)
    {
        if (!Events.TryAdd(key, action))
        {
            Events[key] += action;
        }
    }

    public void RemoveEvent(string key, Action action, string caller)
    {
        if (Events.ContainsKey(key))
        {
            Events[key] -= action;
        }
    }

    #region Non Public
    void IPairEventInvokable.Invoke(string key)
    {
        if (Events.TryGetValue(key, out Action action))
        {
            action?.Invoke();
        }
    }
    #endregion
}

public interface IPairEventInvokable
{
    void Invoke(string key);
}