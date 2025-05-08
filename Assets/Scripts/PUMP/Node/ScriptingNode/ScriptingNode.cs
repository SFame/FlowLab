using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using VFolders.Libs;
using static ScriptingNode;

[ResourceGetter("PUMP/Sprite/PaletteImage/scripting_node_palette")]
public class ScriptingNode : DynamicIONode, INodeAdditionalArgs<ScriptingNodeSerializeInfo>
{
    #region Privates
    private ScriptingSupport _scriptingSupport;

    private string Script { get; set; } = string.Empty;
    private string FileName { get; set; } = string.Empty;

    private bool IsScriptReady { get; set; } = false;

    private ScriptCommunicator Communicator { get; set; } = null;

    private Func<int, string> InputNameGetter { get; set; } = null;

    private Func<int, string> OutputNameGetter { get; set; } = null;

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
             return;

         AddScript(tuple.Value.fileName, tuple.Value.value);
    }

    private void ExportScript()
    {
        if (!IsScriptReady || string.IsNullOrEmpty(Script))
            return;

        string nodeName = string.IsNullOrEmpty(FileName) ? "new_script" : FileName;

        FileBrowser.Save(Script, nodeName, new [] { "py", "txt" }, "Export Script", null, null);
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
            return baseList;
        }
    }

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefabPath => "PUMP/Prefab/Node/SCRIPTING";

    protected override float InEnumeratorXPos => -88f;

    protected override float OutEnumeratorXPos => 88f;

    protected override float EnumeratorMargin => 5f;

    protected override float EnumeratorPadding => 10f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(210f, 100f);

    protected override string NodeDisplayName => "Scripting";

    protected override float TextSize => 25f;

    protected override int DefaultInputCount => 0;

    protected override int DefaultOutputCount => 0;

    protected override string DefineInputName(int tpNumber)
    {
        if (InputNameGetter == null)
            return $"in {tpNumber}";

        return InputNameGetter(tpNumber);
    }

    protected override string DefineOutputName(int tpNumber)
    {
        if (OutputNameGetter == null)
            return $"out {tpNumber}";

        return OutputNameGetter(tpNumber);
    }

    protected override void OnAfterInit()
    {
        ScriptingSupport.Initialize();

        if (!IsDeserialized)
            return;

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
            return;

        InvokeInit();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (Communicator == null)
            return;

        Communicator.InvokeStateUpdate(args, InputToken.Select(tp => tp.State).ToList());
    }

    protected override void OnBeforeRemove()
    {
        InternalDisposeScript();
    }
    #endregion

    #region Scripting
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

            Communicator = new ScriptCommunicator(ScriptingSupport.Log, ScriptingSupport.LogException);

            if (Communicator.SetScript(Script))
            {
                Support.SetName(Communicator.ScriptFieldInfo.Name);
                SetTpWithList(Communicator.ScriptFieldInfo.InputList, Communicator.ScriptFieldInfo.OutputList);
                Communicator.OnOutputApply += OutputToken.ApplyStatesAll;
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
            return;

        try
        {
            Communicator.InvokeInit(InputToken.Select(tp => tp.State).ToList());
            if (Communicator.ScriptFieldInfo.AutoStateUpdateAfterInit)
            {
                StateUpdate(null);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void SetTpWithList(IList<object> inputList, IList<object> outputList)
    {
        int inputCount = inputList.Count;
        int outputCount = outputList.Count;

        SetNameGetter(inputList, outputList);

        InputCount = inputCount;
        OutputCount = outputCount;
    }

    private void SetNameGetter(IList<object> inputList, IList<object> outputList)
    {
        InputNameGetter = index =>
        {
            if (index >= inputList.Count)
                return $"null {index}";

            return inputList[index].ToString();
        };

        OutputNameGetter = index =>
        {
            if (index >= outputList.Count)
                return $"null {index}";

            return outputList[index].ToString();
        };
    }

    private void SetNameGetterDefault()
    {
        InputNameGetter = null;
        OutputNameGetter = null;
    }

    // Scripting Interface ---------------
    public void AddScript(string fileName, string script)
    {
        if (string.IsNullOrEmpty(script))
        {
            ScriptingSupport.Log("스크립트가 비어있습니다");
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
            return $"input: {_inputCount} / output: {_outputCount} / script: {_script}";
        }
    }
    #endregion
}