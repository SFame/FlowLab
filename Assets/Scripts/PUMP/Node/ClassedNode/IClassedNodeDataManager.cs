using Cysharp.Threading.Tasks;
using System;

public interface IClassedNodeDataManager
{
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public PUMPBackground BaseBackground { get; set; }
    public bool HasCurrent();
    public void SetCurrent(IClassedNode classedNode);
    public CurrentClassedPairManagerToken GetCurrent();
    public void OverrideToCurrent(PUMPSaveDataStructure structure);
    public UniTask ApplyCurrentById(string id);
    public void DestroyClassed(IClassedNode classedNode);
    public void DiscardCurrent();
    public UniTask AddNew(IClassedNode classedNode);
    public void Push(string name);
}
