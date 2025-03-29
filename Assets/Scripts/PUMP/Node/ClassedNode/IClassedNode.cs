using System;

public interface IClassedNode
{
    public string Name { get; set; }
    public string Id { get; set; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }

    public event Action<bool[]> OnInputUpdate;
    public event Action<IClassedNode> OpenPanel;
    public event Action<IClassedNode> OnDestroy;
    public void OutputStateUpdate(bool[] outputs);
    public Node GetNode();
}
