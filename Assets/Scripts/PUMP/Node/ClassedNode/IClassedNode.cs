using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IClassedNode
{
    string Name { get; set; }
    string Id { get; set; }
    int InputCount { get; set; }
    int OutputCount { get; set; }

    event Action<TransitionEventArgs> OnInputUpdate;
    event Action<IClassedNode> OpenPanel;
    event Action<IClassedNode> OnDestroy;
    void OutputsApplyAll(Transition[] outputs);
    void OutputApply(TransitionEventArgs args);
    void InputStateValidate(Transition[] exInStates);
    void OutputStateValidate(Transition[] exOutStates);
    List<Action<TransitionType>> GetInputTypeApplier();
    List<Action<TransitionType>> GetOutputTypeApplier();
    Node GetNode();
    Task WaitForDeserializationComplete();
}