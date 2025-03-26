using System;

public interface IClassedNode
{
    public string Name { get; set; }
    public string Id { get; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }

    public event Action<bool[]> OnInputUpdate;
    public event Action<IClassedNode> OpenPanel;
    public event Action<IClassedNode> OnDestroy;
    public void OutputUpdate(bool[] outputs);
    public Node GetNode();

    /// <summary>
    /// 새로운 ID를 반환함과 동시에 Id 필드도 동시에 업데이트 하도록 설계
    /// </summary>
    /// <returns></returns>
    public string GetNewId();
}
