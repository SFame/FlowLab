using OdinSerializer;
using System.Collections.Generic;
using UnityEngine;

public abstract class SaveLoadStructureExtractor : MonoBehaviour
{
    #region When Extract
    public abstract List<SerializeNodeInfo> GetNodeInfos();
    public abstract string GetImagePath();
    public abstract object GetTag();
    public virtual bool ValidateBeforeSerialization(PUMPSaveDataStructure structure) => true;
    #endregion

    #region When Apply
    public abstract void ApplyData(PUMPSaveDataStructure structure);
    #endregion
}
