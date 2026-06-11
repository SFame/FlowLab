using System.Collections.Generic;
using UnityEngine;

public class Trim : Node, INodeAdditionalArgs<TrimType>
{
    private TrimType _trimType = TrimType.All;
    private List<ContextElement> _context;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            _context = base.ContextElements;
            _context.Add(new ContextElement($"All  <b>{CheckMarkGetter(TrimType.All)}</b>", () => SetTrim(TrimType.All)));
            _context.Add(new ContextElement($"Start  <b>{CheckMarkGetter(TrimType.Start)}</b>", () => SetTrim(TrimType.Start)));
            _context.Add(new ContextElement($"End  <b>{CheckMarkGetter(TrimType.End)}</b>", () => SetTrim(TrimType.End)));

            return _context;
        }
    }

    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.String };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.String };

    protected override float InEnumeratorXPos => -39f;

    protected override float OutEnumeratorXPos => 39f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 50f);

    protected override string NodeDisplayName => "Trim";

    protected override float NameTextSize => 18f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(DoTrim(args.State));
    }

    private string DoTrim(string target)
    {
        return _trimType switch
        {
            TrimType.All => target.Trim(),
            TrimType.Start => target.TrimStart(),
            TrimType.End => target.TrimEnd(),
        };
    }

    private void SetTrim(TrimType value)
    {
        _trimType = value;

        if (InputToken.HasAnyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(DoTrim(InputToken.FirstState));
        ReportChanges();
    }

    private string CheckMarkGetter(TrimType value)
    {
        return _trimType == value ? "<" : string.Empty;
    }

    public TrimType AdditionalArgs
    {
        get => _trimType;
        set => _trimType = value;
    }
}

public enum TrimType
{
    All,
    Start,
    End,
}