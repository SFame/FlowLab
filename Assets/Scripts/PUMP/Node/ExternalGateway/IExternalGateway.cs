using System;

public interface IExternalGateway
{
    public bool ObjectIsNull { get; }
    public int GateCount { get; set; }
    public ITransitionPoint this[int index] { get; }
    public event Action OnCountUpdate;
}

public interface IExternalInput : IExternalGateway
{
    
}

public interface IExternalOutput : IExternalGateway
{
    
}
