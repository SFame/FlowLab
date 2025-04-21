using System;

public interface IClassedNode
{
    string Name { get; set; }
    string Id { get; set; }
    int InputCount { get; set; }
    int OutputCount { get; set; }

    event Action<bool[]> OnInputUpdate;
    event Action<IClassedNode> OpenPanel;
    event Action<IClassedNode> OnDestroy;
    void OutputStateUpdate(bool[] outputs);
    void InputStateValidate(bool[] exInStates);
    Node GetNode();
}
