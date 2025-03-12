public interface INodeModifiableArgs
{
    public object ModifiableObject { get; set; }
}

public interface INodeModifiableArgs<T> : INodeModifiableArgs
{
    public T ModifiableTObject { get; set; }
}