using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public interface IScriptCommunicator : IDisposable
{
    event Action<IList<Transition?>> OnOutputApply;
    event Action<int, Transition?> OnOutputApplyAt;
    event Action<string, Transition?> OnOutputApplyTo;
    event Action<string> OnPrint;

    Action<string> Logger { get; set; }
    Action<Exception> ExLogger { get; set; }
    Action<string> ReferenceExLogger { set; }

    ScriptFieldInfo ScriptFieldInfo { get; }
    bool SetScript(string script);
    UniTask<bool> SetScriptAsync(string script);
    void InvokeInit(List<Transition> inputTokenState);
    void InvokeStateUpdate(TransitionEventArgs args, List<Transition> inputTokenState);
    void InvokeTerminate();
}

public struct ScriptFieldInfo
{
    public ScriptFieldInfo(string name, IList<object> inputList, IList<object> outputList, IList<Type> inputTypes, IList<Type> outputTypes, bool isAsync)
    {
        Name = name;
        InputList = inputList.Select(obj => obj.ToString()).ToList();
        OutputList = outputList.Select(obj => obj.ToString()).ToList();
        InputTypes = inputTypes.Select(type =>
        {
            Type convertedType = type;
            TransitionType transitionType;
            try
            {
                if (type == typeof(BigInteger))
                    convertedType = typeof(int);
                else if (type == typeof(double))
                    convertedType = typeof(float);

                transitionType = convertedType.AsTransitionType();
            }
            catch
            {
                return TransitionType.Bool;
            }

            return transitionType;
        }).ToList();
        OutputTypes = outputTypes.Select(type =>
        {
            Type convertedType = type;
            TransitionType transitionType;
            try
            {
                if (type == typeof(BigInteger))
                    convertedType = typeof(int);
                else if (type == typeof(double))
                    convertedType = typeof(float);

                transitionType = convertedType.AsTransitionType();
            }
            catch
            {
                return TransitionType.Bool;
            }

            return transitionType;
        }).ToList();
        IsAsync = isAsync;
    }

    public string Name;
    public List<string> InputList;
    public List<string> OutputList;
    public List<TransitionType> InputTypes;
    public List<TransitionType> OutputTypes;
    public bool IsAsync;
}