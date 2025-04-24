using System;
using System.Collections.Generic;
using System.Linq;

public abstract class DynamicIONode : Node
{
    #region Privates
    private int _inputCount = -1;  // -1: 미설정 (기본값 대체)
    private int _outputCount = -1;

    protected sealed override List<string> InputNames => GetNames(DefineInputName, InputCount).ToList();
    protected sealed override List<string> OutputNames => GetNames(DefineOutputName, OutputCount).ToList();

    private IEnumerable<string> GetNames(Func<int, string> builder, int count)
    {
        for (int i = 0; i < count; i++)
            yield return builder?.Invoke(i) ?? string.Empty;
    }

    private int CompareDefaultValue(int value, int defaultValue)
    {
        int result = value < 0 ? defaultValue : value;
        return result < 0 ? 0 : result;
    }
    #endregion
    
    #region Override Require
    protected abstract int DefaultInputCount { get; }
    protected abstract int DefaultOutputCount { get; }
    protected abstract string DefineInputName(int tpNumber);
    protected abstract string DefineOutputName(int tpNumber);
    #endregion

    #region Set count on child
    protected int InputCount
    {
        get => CompareDefaultValue(_inputCount, DefaultInputCount);
        set
        {
            _inputCount = value;
            if (InputToken != null)
                ResetToken();
        }
    }

    protected int OutputCount
    {
        get => CompareDefaultValue(_outputCount, DefaultOutputCount);
        set
        {
            _outputCount = value;
            if (OutputToken != null)
                ResetToken();
        }
    }
    #endregion
}