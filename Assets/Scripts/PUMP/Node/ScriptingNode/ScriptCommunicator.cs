using System;
using System.Collections.Generic;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using UnityEngine;

public class ScriptCommunicator : IDisposable
{
    #region Static / Const
    private const string OUTPUT_APPLIER_SCRIPT_PATH = "PUMP/Py/OutputApplier";
    private const string APPLIER_INJECT_CODE = "output_applier = OutputApplier()";
    private static string _outputApplierScript;
    private static ScriptEngine _engine;

    private static string OutputApplierScript
    {
        get
        {
            if (string.IsNullOrEmpty(_outputApplierScript))
            {
                _outputApplierScript = Resources.Load<TextAsset>(OUTPUT_APPLIER_SCRIPT_PATH).text;
            }

            return _outputApplierScript;
        }
    }

    private static ScriptEngine Engine
    {
        get
        {
            _engine ??= Python.CreateEngine();
            return _engine;
        }
    }
    #endregion

    #region Privates
    private ScriptScope Scope { get; set; }
    private Action<string> _logger;
    private Action<Exception> _exLogger;
    private Action _initAction;
    private Action<List<bool>, int, bool, bool> _stateUpdateAction;
    private bool _disposed = false;

    private Dictionary<string, dynamic> _essentialMembers = new()
    {
        { "name", null },
        { "input_counts", null },
        { "output_counts", null },
        { "output_applier", null },
        { "init", null },
        { "state_update", null },
    };
    private dynamic OutputApplier { get; set; }
    #endregion

    #region Interface
    public ScriptCommunicator(Action<string> logger, Action<Exception> exLogger)
    {
        var prog = Loading.GetProgress();
        prog.SetProgress(50);

        try
        {
            Scope = Engine.CreateScope();
            Engine.Execute(OutputApplierScript, Scope);
            _logger = logger;
            _exLogger = exLogger;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            prog.SetComplete();
        }
    }

    public ScriptFieldInfo ScriptFieldInfo { get; private set; }
    public event Action<IList<bool>> OnOutputApply;

    /// <summary>
    /// 반환 확인 필수
    /// </summary>
    /// <param name="script"></param>
    /// <returns></returns>
    public bool SetScript(string script)
    {
        try
        {
            Engine.Execute(script, Scope);

            Dictionary<string, dynamic> tempMembers = new Dictionary<string, dynamic>();

            foreach (var member in _essentialMembers)
            {
                if (Scope.TryGetVariable(member.Key, out dynamic value))
                {
                    if (!CheckMemberType(member.Key, value, out string currentType, out string correctType))
                    {
                        _logger?.Invoke($"멤버 타입이 일치하지 않습니다: {member.Key}의 기대 타입: {correctType} / 현재 타입: {currentType}");
                        return false;
                    }
                    tempMembers[member.Key] = value;
                    continue;
                }
                _logger?.Invoke($"멤버가 존재하지 않습니다: {member.Key}");
                return false;
            }

            foreach (var pair in tempMembers)
            {
                _essentialMembers[pair.Key] = pair.Value;
            }

            Engine.Execute(APPLIER_INJECT_CODE, Scope);
            _essentialMembers["output_applier"] = Scope.GetVariable("output_applier");
            return RegistrationMembers();
        }
        catch (Exception e)
        {
            _logger?.Invoke("인터프리팅 에러");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    /// <summary>
    /// Python 스크립트 init 호출
    /// </summary>
    public void InvokeInit()
    {
        try
        {
            _initAction?.Invoke();
        }
        catch (Exception e)
        {
            _logger?.Invoke("init 실행 도중 예외 발생");
            _exLogger?.Invoke(e);
        }
    }

    /// <summary>
    /// Python 스크립트 state_update 호출
    /// </summary>
    /// <param name="args"></param>
    /// <param name="inputTokenState"></param>
    public void InvokeStateUpdate(TransitionEventArgs args, List<bool> inputTokenState)
    {
        try
        {
            if (args == null)
            {
                _stateUpdateAction?.Invoke(inputTokenState, -1, false, false);
                return;
            }

            _stateUpdateAction?.Invoke(inputTokenState, args.Index, args.State, args.IsStateChange);
        }
        catch (Exception e)
        {
            _logger?.Invoke("state_update 실행 도중 예외 발생");
            _exLogger?.Invoke(e);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            OutputApplier?.dispose();
            _initAction = null;
            _stateUpdateAction = null;
            _logger = null;
            OnOutputApply = null;
            Scope = null;
        }
        catch { }
    }
    #endregion

    #region Privates
    private bool RegistrationMembers()
    {
        try
        {
            OutputApplier = _essentialMembers["output_applier"]; // 딕셔너리에는 객체가 없음
            OutputApplier.set_callback(new Action<IList<bool>>(InvokeApplyOutput));

            dynamic initFunc = _essentialMembers["init"];
            _initAction = () => initFunc();

            dynamic stateUpdateFunc = _essentialMembers["state_update"];
            _stateUpdateAction = (inputs, index, state, isChanged) =>
                stateUpdateFunc(inputs, index, state, isChanged);

            ScriptFieldInfo =
                new ScriptFieldInfo(_essentialMembers["name"], _essentialMembers["input_counts"], _essentialMembers["output_counts"]);
            return true;
        }
        catch (Exception e)
        {
            _logger?.Invoke("인터프리팅 에러");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    private void InvokeApplyOutput(IList<bool> outputs)
    {
        try
        {
            OnOutputApply?.Invoke(outputs);
        }
        catch (Exception e)
        {
            _logger?.Invoke($"output_applier.apply()의 outputs의 길이가 노드의 출력과 다릅니다. outputs length: {outputs.Count}");
            _exLogger?.Invoke(e);
        }
    }

    private bool CheckMemberType(string memberName, dynamic value, out string currentType, out string correctType)
    {
        try
        {
            switch (memberName)
            {
                case "name":
                    correctType = "string";
                    currentType = value?.GetType().Name ?? "null";
                    return value is string;

                case "input_counts":
                case "output_counts":
                    correctType = "int";
                    currentType = value?.GetType().Name ?? "null";
                    return value is int;

                case "init":
                case "state_update":
                    correctType = "function";
                    currentType = "function" + (IsPythonFunction(value) ? "" : " 아님");
                    return IsPythonFunction(value);

                case "output_applier":
                    correctType = "OutputApplier";
                    currentType = value?.GetType().Name ?? "null";
                    return true;

                default:
                    correctType = "unknown";
                    currentType = value?.GetType().Name ?? "null";
                    return false;
            }
        }
        catch
        {
            correctType = "확인 불가";
            currentType = "확인 불가";
            return false;
        }
    }

    private bool IsPythonFunction(dynamic obj)
    {
        try
        {
            return obj.GetType().FullName.Contains("Function") ||
                   obj.GetType().FullName.Contains("Method");
        }
        catch
        {
            return false;
        }
    }

    ~ScriptCommunicator()
    {
        if (!_disposed)
            Dispose();
    }
    #endregion
}

public struct ScriptFieldInfo
{
    public ScriptFieldInfo(string name, int inputCount, int outputCount)
    {
        Name = name;
        InputCount = inputCount;
        OutputCount = outputCount;
    }
    public string Name;
    public int InputCount;
    public int OutputCount;
}