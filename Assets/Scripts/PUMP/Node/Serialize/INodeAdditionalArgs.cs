public interface INodeAdditionalArgs
{
    object AdditionalArgs { get; set; }
}

public interface INodeAdditionalArgs<T> : INodeAdditionalArgs
{
    new T AdditionalArgs { get; set; }

    object INodeAdditionalArgs.AdditionalArgs
    {
        get => AdditionalArgs;
        set => AdditionalArgs = (T)value;
    }
}