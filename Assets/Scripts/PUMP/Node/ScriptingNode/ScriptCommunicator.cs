using Cysharp.Threading.Tasks;
using IronPython.Hosting;
using IronPython.Runtime.Types;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ScriptCommunicator : IDisposable
{
    #region Static / Const
    private const string CALLBACKS_SCRIPT_PATH = "PUMP/Py/CoreTemplate/script_bridge";
    private const string CALLBACKS_INJECT_CODE = @"output_applier = OutputApplier()
printer = Printer()";
    private static string _callbacksScript;
    private static CompiledCode _callbackCompiled;
    private static readonly object _initLock = new object();
    private static ScriptEngine _engine;
    private static readonly Type[] _availableType = new[] { typeof(bool), typeof(int), typeof(float), typeof(BigInteger), typeof(double), typeof(string) };

    private static string CallbacksScript
    {
        get
        {
            lock (_initLock)
            {
                if (string.IsNullOrEmpty(_callbacksScript))
                {
                    _callbacksScript = Resources.Load<TextAsset>(CALLBACKS_SCRIPT_PATH).text;
                }
            }

            return _callbacksScript;
        }
    }

    private static CompiledCode CallbackCompiled
    {
        get
        {
            lock (_initLock)
            {
                if (_callbackCompiled != null)
                {
                    return _callbackCompiled;
                }

                ScriptSource callbackScriptSource = _engine.CreateScriptSourceFromString(CallbacksScript);
                _callbackCompiled = callbackScriptSource.Compile();
            }

            return _callbackCompiled;
        }
    }

    private static ScriptEngine Engine
    {
        get
        {
            lock (_initLock)
            {
                if (_engine != null)
                {
                    return _engine;
                }

                _engine = Python.CreateEngine();
                string stdLibPath = Path.Combine(Application.streamingAssetsPath, "IronPython.StdLib.3.4.2", "content", "lib");
                if (Directory.Exists(stdLibPath))
                {
                    ICollection<string> paths = _engine.GetSearchPaths();
                    paths.Add(stdLibPath);
                    _engine.SetSearchPaths(paths);
                }
            }

            return _engine;
        }
    }
    #endregion

    #region Privates
    private Action<string> _logger;
    private Action<Exception> _exLogger;
    private Action<List<dynamic>> _initAction;
    private Action _terminateAction;
    private Action<List<dynamic>, int, dynamic, dynamic, bool> _stateUpdateAction;
    private Func<dynamic> _pulseInstanceGetter;
    private SafetyCancellationTokenSource _asyncModeCts;
    private readonly string _pulseInstanceId = Guid.NewGuid().ToString();
    private bool _isAsync = false;
    private bool _isSetAsync = false;
    private bool _disposed = false;

    private ScriptScope Scope { get; set; }

    private Dictionary<string, dynamic> EssentialMembers { get; } = new()
    {
        { "name", null },
        { "input_list", null },
        { "output_list", null },
        { "input_types", null },
        { "output_types", null },
        { "is_async", null },
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
    #endregion

    #region Interface
    public static async UniTask<ScriptCommunicator> CreateAsync(Action<string> logger, Action<Exception> exLogger)
    {
        try
        {
            ScriptCommunicator communicator = new ScriptCommunicator();

            await UniTask.RunOnThreadPool(() => { communicator.Scope = Engine.CreateScope(); });

            CallbackCompiled.Execute(communicator.Scope);
            communicator.Scope.SetVariable("reference_ex_logger", new Action<string>(communicator.LoggingMissingReference));
            communicator._logger = logger;
            communicator._exLogger = exLogger;
            return communicator;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    public static ScriptCommunicator Create(Action<string> logger, Action<Exception> exLogger)
    {
        try
        {
            ScriptCommunicator communicator = new ScriptCommunicator();
            communicator.Scope = Engine.CreateScope();
            CallbackCompiled.Execute(communicator.Scope);
            communicator.Scope.SetVariable("reference_ex_logger", new Action<string>(communicator.LoggingMissingReference));
            communicator._logger = logger;
            communicator._exLogger = exLogger;
            return communicator;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    public ScriptFieldInfo ScriptFieldInfo { get; private set; }
    public event Action<IList<Transition?>> OnOutputApply;
    public event Action<int, Transition?> OnOutputApplyAt;
    public event Action<string, Transition?> OnOutputApplyTo;
    public event Action<string> OnPrint;

    /// <summary>
    /// 반환 확인 필수
    /// </summary>
    /// <param name="script">스크립트 문자열</param>
    /// <returns>성공 여부</returns>
    public async UniTask<bool> SetScriptAsync(string script)
    {
        try
        {
            await UniTask.RunOnThreadPool(() =>
            {
                ScriptSource scriptSource = _engine.CreateScriptSourceFromString(script);
                CompiledCode compiledCode = scriptSource.Compile();
                compiledCode.Execute(Scope);
            });

            Dictionary<string, dynamic> tempMembers = new Dictionary<string, dynamic>();

            foreach (var member in EssentialMembers)
            {
                if (Scope.TryGetVariable(member.Key, out dynamic value))
                {
                    if (!CheckMemberType(member.Key, value, out string currentType, out string correctType))
                    {
                        _logger?.Invoke(
                            $"Element type mismatch: Expected type for {member.Key}: {correctType} / Current type: {currentType}");
                        return false;
                    }

                    tempMembers[member.Key] = value;
                    continue;
                }

                _logger?.Invoke($"Required field does not exist: {member.Key}");
                return false;
            }

            foreach (var pair in tempMembers)
            {
                EssentialMembers[pair.Key] = pair.Value;
            }

            Engine.Execute(CALLBACKS_INJECT_CODE, Scope);
            _pulseInstanceGetter = Scope.GetVariable("get_pulse_instance");
            _pulseInstanceGetter()._set_instance_id(_pulseInstanceId);
            EssentialMembers["output_applier"] = Scope.GetVariable("output_applier");
            EssentialMembers["printer"] = Scope.GetVariable("printer");

            bool isSuccess = RegistrationMembers();

            if (isSuccess)
            {
                IsAsync = EssentialMembers["is_async"];
                _logger?.Invoke("<b><color=green>Compile success</color></b>");
            }

            return isSuccess;
        }
        catch (SyntaxErrorException se)
        {
            int startCol = se.RawSpan.Start.Column;
            int endCol = se.RawSpan.End.Column;
            string errorCode = HighlightErrorCode(se.GetCodeLine(), startCol, endCol);
            _logger?.Invoke($"[SyntaxError] <b>Line: {se.Line}, Column: {se.Column}</b>\n\"{errorCode}\"");
            _exLogger?.Invoke(se);
            return false;
        }
        catch (MissingReferenceException re)
        {
            _logger?.Invoke($"Reference not found:\n{re.Message}");
            _exLogger?.Invoke(re);
            return false;
        }
        catch (Exception e)
        {
            _logger?.Invoke($"Interpreting error\n{e.Message}");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    /// <summary>
    /// 반환 확인 필수
    /// </summary>
    /// <param name="script">스크립트 문자열</param>
    /// <returns>성공 여부</returns>
    public bool SetScript(string script)
    {
        try
        {
            ScriptSource scriptSource = _engine.CreateScriptSourceFromString(script);
            CompiledCode compiledCode = scriptSource.Compile();
            compiledCode.Execute(Scope);

            Dictionary<string, dynamic> tempMembers = new Dictionary<string, dynamic>();

            foreach (var member in EssentialMembers)
            {
                if (Scope.TryGetVariable(member.Key, out dynamic value))
                {
                    if (!CheckMemberType(member.Key, value, out string currentType, out string correctType))
                    {
                        _logger?.Invoke(
                            $"Element type mismatch: Expected type for {member.Key}: {correctType} / Current type: {currentType}");
                        return false;
                    }

                    tempMembers[member.Key] = value;
                    continue;
                }

                _logger?.Invoke($"Required field does not exist: {member.Key}");
                return false;
            }

            foreach (var pair in tempMembers)
            {
                EssentialMembers[pair.Key] = pair.Value;
            }

            Engine.Execute(CALLBACKS_INJECT_CODE, Scope);
            _pulseInstanceGetter = Scope.GetVariable("get_pulse_instance");
            _pulseInstanceGetter()._set_instance_id(_pulseInstanceId);
            EssentialMembers["output_applier"] = Scope.GetVariable("output_applier");
            EssentialMembers["printer"] = Scope.GetVariable("printer");

            bool isSuccess = RegistrationMembers();

            if (isSuccess)
            {
                IsAsync = EssentialMembers["is_async"];
                _logger?.Invoke("<b><color=green>Compile success</color></b>");
            }

            return isSuccess;
        }
        catch (SyntaxErrorException se)
        {
            int startCol = se.RawSpan.Start.Column;
            int endCol = se.RawSpan.End.Column;
            string errorCode = HighlightErrorCode(se.GetCodeLine(), startCol, endCol);
            _logger?.Invoke($"[SyntaxError] <b>Line: {se.Line}, Column: {se.Column}</b>\n\"{errorCode}\"");
            _exLogger?.Invoke(se);
            return false;
        }
        catch (MissingReferenceException re)
        {
            _logger?.Invoke($"Reference not found:\n{re.Message}");
            _exLogger?.Invoke(re);
            return false;
        }
        catch (Exception e)
        {
            _logger?.Invoke($"Interpreting error\n{e.Message}");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    /// <summary>
    /// Python 스크립트 init 호출
    /// </summary>
    public void InvokeInit(List<Transition> inputTokenState)
    {
        List<dynamic> dynamicStateList = inputTokenState.Select(state =>
        {
            dynamic dynamicState = state.GetValueAsDynamic();

            if (dynamicState is Pulse)
            {
                return _pulseInstanceGetter();
            }

            return dynamicState;
        }).ToList();


        if (IsAsync)
        {
            UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    _initAction?.Invoke(dynamicStateList);
                }
                catch (Exception e)
                {
                    UniTask.Post(() =>
                    {
                        _logger?.Invoke("An exception occurred while executing 'init()'");
                        _exLogger?.Invoke(e);
                    });
                }
            },
            cancellationToken: _asyncModeCts.SafeGetToken(out _asyncModeCts));

            return;
        }

        try
        {
            _initAction?.Invoke(dynamicStateList);
        }
        catch (Exception e)
        {
            _logger?.Invoke("An exception occurred while executing 'init()'");
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
            _logger?.Invoke("An exception occurred while executing 'terminate()'");
            _exLogger?.Invoke(e);
        }
    }

    /// <summary>
    /// Python 스크립트 state_update 호출
    /// </summary>
    /// <param name="args"></param>
    /// <param name="inputTokenState"></param>
    public void InvokeStateUpdate(TransitionEventArgs args, List<Transition> inputTokenState)
    {
        if (args == null)
        {
            _logger?.Invoke("Scripting Node system error");
            Debug.LogError("InvokeStateUpdate: Null Args Detected");
            return;
        }

        // Create Arguments
        int index = args.Index;
        dynamic state = args.State.GetValueAsDynamic();
        state = state is Pulse ? _pulseInstanceGetter() : state;
        dynamic beforeState = args.BeforeState.GetValueAsDynamic();
        beforeState = beforeState is Pulse ? _pulseInstanceGetter : beforeState;
        bool isStateChange = args.IsStateChange;

        List<dynamic> dynamicStateList = inputTokenState.Select(state =>
        {
            dynamic dynamicState = state.GetValueAsDynamic();

            if (dynamicState is Pulse)
            {
                return _pulseInstanceGetter();
            }

            return dynamicState;
        }).ToList();

        if (IsAsync)
        {
            UniTask.RunOnThreadPool(() =>
                {
                    try
                    {
                        _stateUpdateAction?.Invoke(dynamicStateList, index, state, beforeState, isStateChange);
                    }
                    catch (Exception e)
                    {
                        UniTask.Post(() =>
                        {
                            _logger?.Invoke("An exception occurred while executing 'state_update()'");
                            _exLogger?.Invoke(e);
                        });
                    }
                },
                cancellationToken: _asyncModeCts.SafeGetToken(out _asyncModeCts));

            return;
        }

        try
        {
            _stateUpdateAction?.Invoke(dynamicStateList, index, state, beforeState, isStateChange);
        }
        catch (Exception e)
        {
            _logger?.Invoke("An exception occurred while executing 'state_update()'");
            _exLogger?.Invoke(e);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        try
        {
            _asyncModeCts.SafeCancelAndDispose();
            OutputApplier?.dispose();
            Printer?.dispose();
            InvokeTerminate();
            _initAction = null;
            _stateUpdateAction = null;
            _terminateAction = null;
            _logger = null;
            _exLogger = null;
            OnOutputApply = null;
            OnOutputApplyAt = null;
            OnOutputApplyTo = null;
            OnPrint = null;
            Scope = null;
        }
        catch (Exception e)
        {
            _logger?.Invoke("An exception occurred while disposing the node");
            _exLogger?.Invoke(e);
        }
    }
    #endregion

    #region Privates
    private bool RegistrationMembers()
    {
        try
        {
            OutputApplier = EssentialMembers["output_applier"];
            OutputApplier.set_callback
            (
                new Action<IList<dynamic>>(InvokeApplyOutput), 
                new Action<int, dynamic>(InvokeApplyOutputAt), 
                new Action<string, dynamic>(InvokeApplyOutputTo)
            );

            Printer = EssentialMembers["printer"];
            Printer.set_callback(new Action<object>(InvokePrint));

            dynamic initFunc = EssentialMembers["init"];
            _initAction = inputs => initFunc(inputs);

            dynamic terminateFunc = EssentialMembers["terminate"];
            _terminateAction = () => terminateFunc();

            dynamic stateUpdateFunc = EssentialMembers["state_update"];
            _stateUpdateAction = (inputs, index, state, isChanged, isDisconnected) =>
                stateUpdateFunc(inputs, index, state, isChanged, isDisconnected);

            (IList<Type>, IList<Type>) typeTuple = TypeCheck(EssentialMembers["input_list"], EssentialMembers["output_list"],
                EssentialMembers["input_types"], EssentialMembers["output_types"]);

            ScriptFieldInfo = new ScriptFieldInfo
            (
                EssentialMembers["name"],
                EssentialMembers["input_list"],
                EssentialMembers["output_list"],
                typeTuple.Item1,
                typeTuple.Item2,
                EssentialMembers["is_async"]
            );
            return true;
        }
        catch (ArgumentException argumentEx)
        {
            _logger?.Invoke(argumentEx.Message);
            _exLogger?.Invoke(argumentEx);
            return false;
        }
        catch (Exception e)
        {
            _logger?.Invoke("Failed to map reference object");
            _exLogger?.Invoke(e);
            return false;
        }
    }

    private (IList<Type>, IList<Type>) TypeCheck(IList<object> inputList, IList<object> outputList, IList<object> inputTypes, IList<object> outputTypes)
    {
        if (inputList.Count != inputTypes.Count)
        {
            throw new ArgumentException("Length of 'input_list' and 'input_types' do not match");
        }

        if (outputList.Count != outputTypes.Count)
        {
            throw new ArgumentException("Length of 'output_list' and 'output_types' do not match");
        }

        Type pulseType = typeof(Pulse);
        List<Type> inTypes = new();
        foreach (object obj in inputTypes)
        {
            if (obj is PythonType pyType)
            {
                if (PythonType.Get__name__(pyType) == nameof(Pulse))
                {
                    inTypes.Add(pulseType);
                    continue;
                }

                Type type = pyType.__clrtype__();

                if (!_availableType.Contains(type))
                {
                    throw new ArgumentException($"'input_types' contains an unsupported type: {type.Name}");
                }

                inTypes.Add(type);
                continue;
            }

            throw new ArgumentException("'input_types' contains a non-Type object");

        }

        List<Type> outTypes = new();
        foreach (object obj in outputTypes)
        {
            if (obj is PythonType pyType)
            {
                if (PythonType.Get__name__(pyType) == nameof(Pulse))
                {
                    outTypes.Add(pulseType);
                    continue;
                }

                Type type = pyType.__clrtype__();

                if (!_availableType.Contains(type))
                {
                    throw new ArgumentException($"'output_types' contains an unsupported type: {type.Name}");
                }

                outTypes.Add(type);
                continue;
            }

            throw new ArgumentException("'output_types' contains a non-Type object");
        }

        return (inTypes, outTypes);
    }

    private void InvokeActionOnMainThread(Action action, bool dispatchMainThread)
    {
        if (dispatchMainThread)
        {
            UniTask.Post(action);
            return;
        }

        action?.Invoke();
    }

    private void InvokeApplyOutput(IList<dynamic> values)
    {
        Action applyAction = () =>
        {
            try
            {
                List<Transition?> transitions = values.Select<dynamic, Transition?>(value =>
                {
                    if (value == null)
                        return null;

                    try
                    {
                        if (value._get_instance_id() == _pulseInstanceId)
                        {
                            return Transition.Pulse();
                        }
                    }
                    catch (RuntimeBinderException) { }

                    return new Transition(value);
                }).ToList();

                OnOutputApply?.Invoke(transitions);
            }
            catch (TransitionException tEx)
            {
                _logger?.Invoke($"Output type mismatch or Null was assigned\n{tEx.Message}");
                _exLogger?.Invoke(tEx);
            }
            catch (ArgumentException e)
            {
                _logger?.Invoke($"Length of the list passed to 'output_applier.apply()' does not match the node's outputs. length: {values.Count}");
                _exLogger?.Invoke(e);
            }
            catch (Exception e)
            {
                _logger?.Invoke("An error occurred during 'output_applier.apply()'");
                _exLogger?.Invoke(e);
            }
        };

        InvokeActionOnMainThread(applyAction, IsAsync);
    }

    private void InvokeApplyOutputAt(int index, dynamic value)
    {
        Action applyAction = () =>
        {
            try
            {
                if (value == null)
                {
                    OnOutputApplyAt?.Invoke(index, null);
                    return;
                }

                try
                {
                    if (value._get_instance_id() == _pulseInstanceId)
                    {
                        OnOutputApplyAt?.Invoke(index, Transition.Pulse());
                        return;
                    }
                }
                catch (RuntimeBinderException) { }

                OnOutputApplyAt?.Invoke(index, new Transition(value));
            }
            catch (TransitionException tEx)
            {
                _logger?.Invoke($"Output type mismatch or Null was assigned: value: ({value})");
                _exLogger?.Invoke(tEx);
            }
            catch (IndexOutOfRangeException ie)
            {
                _logger?.Invoke($"Index for 'output_applier.apply_at()' is out of range: index: ({index})");
                _exLogger?.Invoke(ie);
            }
            catch (Exception e)
            {
                _logger?.Invoke("An error occurred during 'output_applier.apply_at()'");
                _exLogger?.Invoke(e);
            }
        };

        InvokeActionOnMainThread(applyAction, IsAsync);
    }

    private void InvokeApplyOutputTo(string name, dynamic value)
    {
        Action applyAction = () =>
        {
            try
            {
                if (value == null)
                {
                    OnOutputApplyTo?.Invoke(name, null);
                    return;
                }

                try
                {
                    if (value._get_instance_id() == _pulseInstanceId)
                    {
                        OnOutputApplyTo?.Invoke(name, Transition.Pulse());
                        return;
                    }
                }
                catch (RuntimeBinderException) { }

                OnOutputApplyTo?.Invoke(name, new Transition(value));
            }
            catch (TransitionException tEx)
            {
                _logger?.Invoke($"Output type mismatch or Null was assigned: value: ({value})");
                _exLogger?.Invoke(tEx);
            }
            catch (KeyNotFoundException ke)
            {
                _logger?.Invoke($"Output not found: name: ({name})");
                _exLogger?.Invoke(ke);
            }
            catch (AmbiguousMatchException ae)
            {
                _logger?.Invoke($"Cannot use 'output_applier.apply_to()' when 'output_list' contains duplicate strings: name: ({name})");
                _exLogger?.Invoke(ae);
            }
            catch (Exception e)
            {
                _logger?.Invoke("An error occurred during 'output_applier.apply_to()'");
                _exLogger?.Invoke(e);
            }
        };

        InvokeActionOnMainThread(applyAction, IsAsync);
    }

    private void InvokePrint(object value)
    {
        Action printAction = () =>
        {
            try
            {
                OnPrint?.Invoke(value.ToString());
            }
            catch (Exception e)
            {
                _logger?.Invoke("An error occurred during 'printer.print()'");
                _exLogger?.Invoke(e);
            }
        };

        InvokeActionOnMainThread(printAction, IsAsync);
    }

    private void LoggingMissingReference(string assembly)
    {
        throw new MissingReferenceException($"Except: add_reference({assembly})");
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
                case "input_types":
                case "output_types":
                    correctType = "IList<object>";
                    currentType = value?.GetType().Name ?? "null";
                    return value is IList<object>;

                case "is_async":
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