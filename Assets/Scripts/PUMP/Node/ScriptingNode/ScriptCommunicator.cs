using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using UnityEngine;

public class ScriptCommunicator : IDisposable
{
    #region Static / Const
    private const string CALLBACKS_SCRIPT_PATH = "PUMP/Py/CoreTemplate/script_callbacks";
    private const string CALLBACKS_INJECT_CODE = @"output_applier = OutputApplier()
printer = Printer()";
    private static string _callbacksScript;
    private static CompiledCode _callbackCompiled;
    private static readonly object _callbackCompileLock = new object();
    private static ScriptEngine _engine;

    private static string CallbacksScript
    {
        get
        {
            if (string.IsNullOrEmpty(_callbacksScript))
            {
                _callbacksScript = Resources.Load<TextAsset>(CALLBACKS_SCRIPT_PATH).text;
            }

            return _callbacksScript;
        }
    }

    private static CompiledCode CallbackCompiled
    {
        get
        {
            if (_callbackCompiled == null)
            {
                lock (_callbackCompileLock)
                {
                    if (_callbackCompiled == null)
                    {
                        ScriptSource callbackScriptSource = _engine.CreateScriptSourceFromString(CallbacksScript);
                        _callbackCompiled = callbackScriptSource.Compile();
                    }
                }
            }

            return _callbackCompiled;
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
    private Action _terminateAction;
    private Action<List<bool>, int, bool, bool> _stateUpdateAction;
    private SafetyCancellationTokenSource _asyncModeCts;
    private bool _isAsync = false;
    private bool _isSetAsync = false;
    private bool _disposed = false;

    private Dictionary<string, dynamic> _essentialMembers = new()
    {
        { "name", null },
        { "input_list", null },
        { "output_list", null },
        { "is_async", null },
        { "auto_state_update_after_init", null },
        { "output_applier", null },
        { "printer", null },
        { "init", null },
        { "terminate", null },
        { "state_update", null },
    };
    private dynamic OutputApplier { get; set; }
    private dynamic Printer { get; set; }

    private bool IsAsync
    {
        get => _isAsync;
        set
        {
            if (_isSetAsync)
                return;

            _isSetAsync = true;
            _isAsync = value;
        }
    }

    private SafetyCancellationTokenSource AsyncModeCts
    {
        get
        {
            _asyncModeCts ??= new SafetyCancellationTokenSource();
            return _asyncModeCts;
        }
    }
    #endregion

    #region Interface
    public ScriptCommunicator(Action<string> logger, Action<Exception> exLogger)
    {
        var prog = Loading.GetProgress();
        prog.SetProgress(50);

        try
        {
            Scope = Engine.CreateScope();
            CallbackCompiled.Execute(Scope);
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
    public event Action<string> OnPrint;

    /// <summary>
    /// 반환 확인 필수
    /// </summary>
    /// <param name="script"></param>
    /// <returns></returns>
    public bool SetScript(string script)
    {
        try
        {
            ScriptSource scriptSource = _engine.CreateScriptSourceFromString(script);
            CompiledCode compiledCode = scriptSource.Compile();
            compiledCode.Execute(Scope);

            Dictionary<string, dynamic> tempMembers = new Dictionary<string, dynamic>();

            foreach (var member in _essentialMembers)
            {
                if (Scope.TryGetVariable(member.Key, out dynamic value))
                {
                    if (!CheckMemberType(member.Key, value, out string currentType, out string correctType))
                    {
                        _logger?.Invoke($"요소 타입이 일치하지 않습니다: {member.Key}의 기대 타입: {correctType} / 현재 타입: {currentType}");
                        return false;
                    }

                    tempMembers[member.Key] = value;
                    continue;
                }

                _logger?.Invoke($"필소 항목이 존재하지 않습니다: {member.Key}");
                return false;
            }

            foreach (var pair in tempMembers)
            {
                _essentialMembers[pair.Key] = pair.Value;
            }

            Engine.Execute(CALLBACKS_INJECT_CODE, Scope);
            _essentialMembers["output_applier"] = Scope.GetVariable("output_applier");
            _essentialMembers["printer"] = Scope.GetVariable("printer");

            bool isSuccess = RegistrationMembers();

            if (isSuccess)
            {
                IsAsync = _essentialMembers["is_async"];
                _logger?.Invoke("<b><color=green>Compile success</color></b>");
            }

            return isSuccess;
        }
        catch (SyntaxErrorException syntaxError)
        {
            int startCol = syntaxError.RawSpan.Start.Column;
            int endCol = syntaxError.RawSpan.End.Column;
            string errorCode = HighlightErrorCode(syntaxError.GetCodeLine(), startCol, endCol);
            _logger?.Invoke($"[SyntaxError] <b>Line: {syntaxError.Line}, Column: {syntaxError.Column}</b>\n\"{errorCode}\"");
            _exLogger?.Invoke(syntaxError);
            return false;
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
        if (IsAsync)
        {
            UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    _initAction?.Invoke();
                }
                catch (Exception e)
                {
                    UniTask.Post(() =>
                    {
                        _logger?.Invoke("init 실행 도중 예외가 발생했습니다");
                        _exLogger?.Invoke(e);
                    });
                }
            },
            cancellationToken: AsyncModeCts.Token);

            return;
        }

        try
        {
            _initAction?.Invoke();
        }
        catch (Exception e)
        {
            _logger?.Invoke("init 실행 도중 예외가 발생했습니다");
            _exLogger?.Invoke(e);
        }
    }

    public void InvokeTerminate()
    {
        try
        {
            _terminateAction?.Invoke();
        }
        catch (Exception e)
        {
            _logger?.Invoke("terminate 실행 도중 예외가 발생했습니다");
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
        bool argsIsNull = args == null;
        int Index = argsIsNull ? -1 : args.Index;
        bool State = !argsIsNull && args.State;
        bool IsStateChange = !argsIsNull && args.IsStateChange;

        if (IsAsync)
        {
            UniTask.RunOnThreadPool(() =>
                {
                    try
                    {
                        _stateUpdateAction?.Invoke(inputTokenState, Index, State, IsStateChange);
                    }
                    catch (Exception e)
                    {
                        UniTask.Post(() =>
                        {
                            _logger?.Invoke("state_update 실행 도중 예외가 발생했습니다");
                            _exLogger?.Invoke(e);
                        });
                    }
                },
                cancellationToken: AsyncModeCts.Token);

            return;
        }

        try
        {
            _stateUpdateAction?.Invoke(inputTokenState, Index, State, IsStateChange);
        }
        catch (Exception e)
        {
            _logger?.Invoke("state_update 실행 도중 예외가 발생했습니다");
            _exLogger?.Invoke(e);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);

        try
        {
            if (IsAsync)
            {
                AsyncModeCts.Cancel();
                AsyncModeCts.Dispose();
            }

            OutputApplier?.dispose();
            Printer?.dispose();
            InvokeTerminate();
            _initAction = null;
            _stateUpdateAction = null;
            _terminateAction = null;
            _logger = null;
            _exLogger = null;
            OnOutputApply = null;
            OnPrint = null;
            Scope = null;
        }
        catch (Exception e)
        {
            _logger?.Invoke("terminate 실행 도중 예외가 발생했습니다");
            _exLogger?.Invoke(e);
        }
    }
    #endregion

    #region Privates
    private bool RegistrationMembers()
    {
        try
        {
            OutputApplier = _essentialMembers["output_applier"];
            OutputApplier.set_callback(new Action<IList<bool>>(InvokeApplyOutput));

            Printer = _essentialMembers["printer"];
            Printer.set_callback(new Action<object>(InvokePrint));

            dynamic initFunc = _essentialMembers["init"];
            _initAction = () => initFunc();

            dynamic terminateFunc = _essentialMembers["terminate"];
            _terminateAction = () => terminateFunc();

            dynamic stateUpdateFunc = _essentialMembers["state_update"];
            _stateUpdateAction = (inputs, index, state, isChanged) =>
                stateUpdateFunc(inputs, index, state, isChanged);

            ScriptFieldInfo = new ScriptFieldInfo
            (
                _essentialMembers["name"],
                _essentialMembers["input_list"],
                _essentialMembers["output_list"],
                _essentialMembers["is_async"],
                _essentialMembers["auto_state_update_after_init"]
            );
            return true;
        }
        catch (Exception e)
        {
            _logger?.Invoke("참조 객체의 매핑을 실패했습니다");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    private void InvokeApplyOutput(IList<bool> outputs)
    {
        if (IsAsync)
        {
            UniTask.Post(() =>
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
            });

            return;
        }

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

    private void InvokePrint(object value)
    {
        if (IsAsync)
        {
            UniTask.Post(() =>
            {
                try
                {
                    OnPrint?.Invoke(value.ToString());
                }
                catch (Exception e)
                {
                    _logger?.Invoke("print 도중 문제가 발생했습니다");
                    _exLogger?.Invoke(e);
                }
            });

            return;
        }

        try
        {
            OnPrint?.Invoke(value.ToString());
        }
        catch (Exception e)
        {
            _logger?.Invoke("print 도중 문제가 발생했습니다");
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

                case "input_list":
                case "output_list":
                    correctType = "IList<object>";
                    currentType = value?.GetType().Name ?? "null";
                    return value is IList<object>;

                case "is_async":
                case "auto_state_update_after_init":
                    correctType = "bool";
                    currentType = value?.GetType().Name ?? "null";
                    return value is bool;

                case "init":
                case "terminate":
                case "state_update":
                    correctType = "PythonFunction";
                    currentType = value?.GetType().Name ?? "null";
                    return value is IronPython.Runtime.PythonFunction;

                case "output_applier":
                    correctType = "OutputApplier";
                    currentType = value?.GetType().Name ?? "null";
                    return true;

                case "printer":
                    correctType = "Printer";
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
            correctType = "unknown";
            currentType = "unknown";
            return false;
        }
    }

    private string HighlightErrorCode(string errorCode, int start, int end)
    {
        if (start < 0) start = 0;
        if (end > errorCode.Length) end = errorCode.Length;
        if (start >= errorCode.Length || end <= start) return errorCode;

        string highlightedCode = errorCode.Substring(0, start);
        highlightedCode += "<u>" + errorCode.Substring(start, end - start) + "</u>";

        if (end < errorCode.Length)
            highlightedCode += errorCode.Substring(end);

        return highlightedCode;
    }

    ~ScriptCommunicator()
    {
        Dispose();
    }
    #endregion
}

public struct ScriptFieldInfo
{
    public ScriptFieldInfo(string name, IList<object> inputList, IList<object> outputList, bool isAsync, bool autoStateUpdateAfterInit)
    {
        Name = name;
        InputList = inputList;
        OutputList = outputList;
        IsAsync = isAsync;
        AutoStateUpdateAfterInit = autoStateUpdateAfterInit;
    }

    public string Name;
    public IList<object> InputList;
    public IList<object> OutputList;
    public bool IsAsync;
    public bool AutoStateUpdateAfterInit;
}