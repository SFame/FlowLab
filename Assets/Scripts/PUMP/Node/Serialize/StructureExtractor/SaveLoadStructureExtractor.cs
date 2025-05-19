using System.Collections.Generic;
using UnityEngine;

public abstract class SaveLoadStructureExtractor : MonoBehaviour
{
    #region When Extract
    public abstract List<SerializeNodeInfo> GetNodeInfos();
    public abstract object GetTag();
    public virtual bool ValidateBeforeSerialization(PUMPSaveDataStructure structure) => true; // 직렬화 하기 완성된 데이터 검증 가능
    #endregion

    #region When Apply
    public abstract void ApplyData(PUMPSaveDataStructure structure);
    #endregion
}