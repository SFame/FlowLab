using System;
using System.Collections.Generic;

public interface IExternalGateway : IEnumerable<ITransitionPoint>
{
    public bool ObjectIsNull { get; }
    public int GateCount { get; set; }
    public ITransitionPoint this[int index] { get; }
    public event Action<int> OnCountUpdate;
}

public interface IExternalInput : IExternalGateway
{
    
}

public interface IExternalOutput : IExternalGateway
{
    public event Action OnStateUpdate;
}
