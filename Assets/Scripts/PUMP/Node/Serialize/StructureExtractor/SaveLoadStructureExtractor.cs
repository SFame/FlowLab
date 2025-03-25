using OdinSerializer;
using System.Collections.Generic;
using UnityEngine;

public abstract class SaveLoadStructureExtractor : MonoBehaviour
{
    public abstract List<SerializeNodeInfo> GetNodeInfos();
    public abstract string GetImagePath();
    public abstract object GetTag();
    public abstract void ApplyData(PUMPSaveDataStructure structure);
}
