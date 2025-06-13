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
    protected sealed override List<TransitionType> InputTypes => GetTypes(DefineInputType, InputCount).ToList();
    protected sealed override List<TransitionType> OutputTypes => GetTypes(DefineOutputType, OutputCount).ToList();

    private IEnumerable<string> GetNames(Func<int, string> builder, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return builder?.Invoke(i) ?? string.Empty;
        }
    }

    private IEnumerable<TransitionType> GetTypes(Func<int, TransitionType> builder, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return builder?.Invoke(i) ?? TransitionType.Bool;
        }
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
    protected abstract string DefineInputName(int tpIndex);
    protected abstract string DefineOutputName(int tpIndex);
    protected abstract TransitionType DefineInputType(int tpIndex);
    protected abstract TransitionType DefineOutputType(int tpIndex);
    protected abstract override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes);
    #endregion

    #region Set count on child
    protected int InputCount
    {
        get => CompareDefaultValue(_inputCount, DefaultInputCount);
        set
        {
            _inputCount = CompareDefaultValue(value, DefaultInputCount);
            if (InputToken != null)
                ResetInputToken();
        }
    }

    protected int OutputCount
    {
        get => CompareDefaultValue(_outputCount, DefaultOutputCount);
        set
        {
            _outputCount = CompareDefaultValue(value, DefaultOutputCount);
            if (OutputToken != null)
                ResetOutputToken(true);
        }
    }

    /// <summary>
    /// 한꺼번에 설정
    /// </summary>
    protected void FuseIOCounts(int inputCount, int outputCount)
    {
        _inputCount = CompareDefaultValue(inputCount, DefaultInputCount);
        _outputCount = CompareDefaultValue(outputCount, DefaultOutputCount);

        if (InputToken != null && OutputToken != null)
        {
            ResetOutputToken(false);
            ResetInputToken();
            ((INodeLifecycleCallable)this).CallSetOutputResetStates();
        }
    }
    #endregion
}