public interface INodeAdditionalArgs
{
    public object AdditionalArgs { get; set; }
}

public interface INodeAdditionalArgs<T> : INodeAdditionalArgs
{
    public T AdditionalTArgs { get; set; }
}