using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static ScriptingNode;
using Debug = UnityEngine.Debug;

[ResourceGetter("PUMP/Sprite/PaletteImage/scripting_node_palette")]
public class ScriptingNode : DynamicIONode, INodeAdditionalArgs<ScriptingNodeSerializeInfo>
{
    #region Private Static
    private const string DEFAULT_TEMPLATE_NAME_ENG = "new_scripting_node";
    private const string DEFAULT_TEMPLATE_NAME_KOR = "새_스크립트_노드";
    private const string TEMPLATE_PATH_ENG = "PUMP/Py/CoreTemplate/scripting_node";
    private const string TEMPLATE_PATH_KOR = "PUMP/Py/CoreTemplate/scripting_node_kor";
    private static Lazy<string> _templateEngLazy = new Lazy<string>(() => Resources.Load<TextAsset>(TEMPLATE_PATH_ENG).text);
    private static Lazy<string> _templateKorLazy = new Lazy<string>(() => Resources.Load<TextAsset>(TEMPLATE_PATH_KOR).text);
    #endregion

    #region Privates
    private ScriptingSupport _scriptingSupport;

    private string Script { get; set; } = string.Empty;
    private string FileName { get; set; } = string.Empty;

    private bool IsScriptReady { get; set; } = false;

    private ScriptCommunicator Communicator { get; set; } = null;

    private Func<int, string> InputNameGetter { get; set; } = null;

    private Func<int, string> OutputNameGetter { get; set; } = null;

    private Func<int, TransitionType> InputTypeGetter { get; set; } = null;

    private Func<int, TransitionType> OutputTypeGetter { get; set; } = null;

    private ScriptingSupport ScriptingSupport
    {
        get
        {
            _scriptingSupport ??= Support.GetComponent<ScriptingSupport>();
            return _scriptingSupport;
        }
    }

    private void ImportScript()
    {
        var tuple = FileBrowser.Load(new[] { "py", "txt" }, "Import Script", null, null);

        if (tuple == null || string.IsNullOrEmpty(tuple.Value.value))
        {
            return;
        }

        AddScriptAsync(tuple.Value.fileName, tuple.Value.value).Forget();
    }

    private void ExportScript()
    {
        if (!IsScriptReady || string.IsNullOrEmpty(Script))
        {
            return;
        }

        string nodeName = string.IsNullOrEmpty(FileName) ? "new_script" : FileName;

        FileBrowser.Save(Script, nodeName, new [] { "py", "txt" }, "Export Script", null, null);
    }

    private void CreateTemplateEng()
    {
        FileBrowser.Save(_templateEngLazy.Value, DEFAULT_TEMPLATE_NAME_ENG, new [] { "py", "txt" }, "Create Python Template", null, null);
    }

    private void CreateTemplateKor()
    {
        FileBrowser.Save(_templateKorLazy.Value, DEFAULT_TEMPLATE_NAME_KOR, new [] { "py", "txt" }, "파이썬 템플릿 생성", null, null);
    }
    #endregion

    #region Override
    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> baseList = base.ContextElements;
            baseList.Add(new ContextElement("Import", ImportScript));

            if (IsScriptReady)
            {
                baseList.Add(new ContextElement("Export", ExportScript));
            }

            baseList.Add(new ContextElement("Show Log", ScriptingSupport.OpenLoggingPanel));
            baseList.Add(new ContextElement("Remove Script", DisposeScript));
            baseList.Add(new ContextElement("Create Template-ENG", CreateTemplateEng));
            baseList.Add(new ContextElement("Create Template-KOR", CreateTemplateKor));

            return baseList;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SCRIPTING";

    protected override float InEnumeratorXPos => -88f;

    protected override float OutEnumeratorXPos => 88f;

    protected override float EnumeratorMargin => 5f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(210f, 100f);

    protected override string NodeDisplayName => "Scripting";

    protected override float NameTextSize => 25f;

    protected override int DefaultInputCount => 0;

    protected override int DefaultOutputCount => 0;

    protected override string DefineInputName(int tpIndex)
    {
        if (InputNameGetter == null)
        {
            return $"in {tpIndex}";
        }

        return InputNameGetter(tpIndex);
    }

    protected override string DefineOutputName(int tpIndex)
    {
        if (OutputNameGetter == null)
        {
            return $"out {tpIndex}";
        }

        return OutputNameGetter(tpIndex);
    }

    protected override TransitionType DefineInputType(int tpIndex)
    {
        if (InputTypeGetter == null)
        {
            return TransitionType.Bool;
        }

        return InputTypeGetter(tpIndex);
    }

    protected override TransitionType DefineOutputType(int tpIndex)
    {
        if (OutputTypeGetter == null)
        {
            return TransitionType.Bool;
        }

        return OutputTypeGetter(tpIndex);
    }

    protected override void OnAfterInit()
    {
        ScriptingSupport.Initialize();

        if (!IsDeserialized)
        {
            return;
        }

        if (string.IsNullOrEmpty(Script))
        {
            InternalDisposeScript(false);
            return;
        }

        InternalAddScript(FileName, Script);
    }

    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        if (!IsDeserialized)
        {
            return;
        }

        InvokeInit();
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        Func<int, TransitionType> outputTypeGetter = OutputTypeGetter ?? (_ => TransitionType.Bool);
        List<Transition> stateList = new();

        for (int i = 0; i < outputCount; i++)
        {
            stateList.Add(Transition.Null(outputTypeGetter(i)));
        }

        return stateList.ToArray();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        Communicator?.InvokeStateUpdate(args, InputToken.Select(sf => sf.State).ToList());
    }

    protected override void OnBeforeRemove()
    {
        InternalDisposeScript();
    }
    #endregion

    #region Scripting
    private async UniTask InternalAddScriptAsync(string fileName, string script)
    {
        ScriptingSupport.SetLoadingAnimation(true);

        try
        {
            InternalDisposeScript();
            Script = script;
            FileName = fileName;

            if (string.IsNullOrEmpty(Script))
            {
                return;
            }

            Communicator = await ScriptCommunicator.CreateAsync(ScriptingSupport.Log, ScriptingSupport.LogException);

            if (await Communicator.SetScriptAsync(Script))
            {
                Support.SetName(Communicator.ScriptFieldInfo.Name);
                SetTpWithList
                (
                    Communicator.ScriptFieldInfo.InputList,
                    Communicator.ScriptFieldInfo.OutputList,
                    Communicator.ScriptFieldInfo.InputTypes,
                    Communicator.ScriptFieldInfo.OutputTypes
                );

                Communicator.OnOutputApply += Apply;
                Communicator.OnOutputApplyAt += ApplyAt;
                Communicator.OnOutputApplyTo += ApplyTo;
                Communicator.OnPrint += ScriptingSupport.Print;

                ScriptingSupport.ShowFileName(FileName);
                IsScriptReady = true;
                return;
            }

            InternalDisposeScript();
        }
        catch (Exception e)
        {
            InternalDisposeScript();
            Debug.LogException(e);
        }
        finally
        {
            ScriptingSupport.SetLoadingAnimation(false);
        }
    }

    private void InternalAddScript(string fileName, string script)
    {
        try
        {
            InternalDisposeScript();
            Script = script;
            FileName = fileName;

            if (string.IsNullOrEmpty(Script))
            {
                return;
            }

            Communicator = ScriptCommunicator.Create(ScriptingSupport.Log, ScriptingSupport.LogException);

            if (Communicator.SetScript(Script))
            {
                Support.SetName(Communicator.ScriptFieldInfo.Name);
                SetTpWithList
                (
                    Communicator.ScriptFieldInfo.InputList,
                    Communicator.ScriptFieldInfo.OutputList,
                    Communicator.ScriptFieldInfo.InputTypes,
                    Communicator.ScriptFieldInfo.OutputTypes
                );

                Communicator.OnOutputApply += Apply;
                Communicator.OnOutputApplyAt += ApplyAt;
                Communicator.OnOutputApplyTo += ApplyTo;
                Communicator.OnPrint += ScriptingSupport.Print;

                ScriptingSupport.ShowFileName(FileName);
                IsScriptReady = true;
                return;
            }

            InternalDisposeScript();
        }
        catch (Exception e)
        {
            InternalDisposeScript();
            Debug.LogException(e);
        }
    }

    private void InternalDisposeScript(bool countReset = true)
    {
        IsScriptReady = false;

        Script = string.Empty;
        FileName = string.Empty;
        Communicator?.Dispose();
        Communicator = null;

        if (countReset)
        {
            SetTypeGetterDefault();
            SetNameGetterDefault();
            InputCount = DefaultInputCount;
            OutputCount = DefaultOutputCount;
        }

        ScriptingSupport.RemoveFileName();
        ScriptingSupport.RemoveAllLog();
        Support.SetName(NodeDisplayName);
    }

    private void InvokeInit()
    {
        if (!IsScriptReady || Communicator == null)
        {
            return;
        }

        try
        {
            Communicator.InvokeInit(InputToken.Select(tp => tp.State).ToList());
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void SetTpWithList(IList<string> inputList, IList<string> outputList, IList<TransitionType> inputTypes, IList<TransitionType> outpTypes)
    {
        int inputCount = inputList.Count;
        int outputCount = outputList.Count;

        SetTypeGetter(inputTypes, outpTypes);
        SetNameGetter(inputList, outputList);

        InputCount = inputCount;
        OutputCount = outputCount;
    }

    private void SetNameGetter(IList<string> inputList, IList<string> outputList)
    {
        InputNameGetter = index =>
        {
            if (index >= inputList.Count)
                return $"null {index}";

            return inputList[index];
        };

        OutputNameGetter = index =>
        {
            if (index >= outputList.Count)
                return $"null {index}";

            return outputList[index];
        };
    }

    private void SetTypeGetter(IList<TransitionType> inputTypes, IList<TransitionType> outputTypes)
    {
        InputTypeGetter = index =>
        {
            if (index >= inputTypes.Count)
                return TransitionType.Bool;

            return inputTypes[index];
        };

        OutputTypeGetter = index =>
        {
            if (index >= outputTypes.Count)
                return TransitionType.Bool;

            return outputTypes[index];
        };
    }

    private void SetNameGetterDefault()
    {
        InputNameGetter = null;
        OutputNameGetter = null;
    }

    private void SetTypeGetterDefault()
    {
        InputTypeGetter = null;
        OutputTypeGetter = null;
    }

    private void Apply(IList<Transition?> values)
    {
        OutputToken.PushAllowingNull(values);
    }

    private void ApplyAt(int index, Transition? state)
    {
        IStateful adapter = OutputToken[index];

        if (state == null)
        {
            adapter.State = adapter.Type.Null();
            return;
        }

        state.Value.ThrowIfTypeMismatch(adapter.Type);
        adapter.State = state.Value;
    }

    private void ApplyTo(string name, Transition? state)
    {
        IStateful adapter = OutputToken[name];

        if (state == null)
        {
            adapter.State = adapter.Type.Null();
            return;
        }

        state.Value.ThrowIfTypeMismatch(adapter.Type);
        adapter.State = state.Value;
    }

    // Scripting Interface ---------------
    public async UniTask AddScriptAsync(string fileName, string script)
    {
        if (string.IsNullOrEmpty(script))
        {
            ScriptingSupport.Log("Script is empty");
            InternalDisposeScript();
            return;
        }

        await InternalAddScriptAsync(fileName, script);
        InvokeInit();
        ReportChanges();
    }

    public void AddScript(string fileName, string script)
    {
        if (string.IsNullOrEmpty(script))
        {
            ScriptingSupport.Log("Script is empty");
            InternalDisposeScript();
            return;
        }

        InternalAddScript(fileName, script);
        InvokeInit();
        ReportChanges();
    }

    public void DisposeScript()
    {
        InternalDisposeScript(true);
        ReportChanges();
    }
    // ---------------------------------
    #endregion

    #region Serialization
    public ScriptingNodeSerializeInfo AdditionalArgs
    {
        get => new()
        {
            _inputCount = InputCount,
            _outputCount = OutputCount,
            _fileName = FileName,
            _script = Script
        };
        set
        {
            InputCount = value._inputCount;
            OutputCount = value._outputCount;
            FileName = value._fileName;
            Script = value._script;
        }
    }

    [Serializable]
    public struct ScriptingNodeSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public int _outputCount;
        [OdinSerialize] public string _fileName;
        [OdinSerialize] public string _script;

        public override string ToString()
        {
            return $"input: {_inputCount}\noutput: {_outputCount}\nfileName: {_fileName}\nscript: {_script}";
        }
    }
    #endregion
}