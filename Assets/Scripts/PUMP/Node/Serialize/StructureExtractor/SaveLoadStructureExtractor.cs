using OdinSerializer;
using System.Collections.Generic;
using UnityEngine;

public abstract class SaveLoadStructureExtractor : MonoBehaviour
{
    #region Extract
    public abstract List<SerializeNodeInfo> GetNodeInfos();
    public abstract string GetImagePath();
    public abstract object GetTag();
    #endregion

    #region Apply
    public abstract void ApplyData(PUMPSaveDataStructure structure);
    #endregion
}
