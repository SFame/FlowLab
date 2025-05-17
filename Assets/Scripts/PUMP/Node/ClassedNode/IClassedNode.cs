using System;
using System.Collections.Generic;

public interface IClassedNode
{
    string Name { get; set; }
    string Id { get; set; }
    int InputCount { get; set; }
    int OutputCount { get; set; }

    event Action<Transition[]> OnInputUpdate;
    event Action<IClassedNode> OpenPanel;
    event Action<IClassedNode> OnDestroy;
    void OutputStateUpdate(Transition[] outputs);
    void InputStateValidate(Transition[] exInStates);
    List<Action<TransitionType>> GetInputTypeApplier();
    List<Action<TransitionType>> GetOutputTypeApplier();
    Node GetNode();
}