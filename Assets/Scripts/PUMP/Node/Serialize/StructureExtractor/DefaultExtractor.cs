using System.Collections.Generic;
using UnityEngine;

public class DefaultExtractor : SaveLoadStructureExtractor
{
    [SerializeField] private PUMPBackground background;

    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        background.SetInfos(structure.NodeInfos, true);
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        return background.GetInfos();
    }

    public override object GetTag()
    {
        return null;
    }
}
