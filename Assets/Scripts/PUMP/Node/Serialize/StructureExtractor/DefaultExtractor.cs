using System.Collections.Generic;
using UnityEngine;
using Utils;

public class DefaultExtractor : SaveLoadStructureExtractor
{
    [SerializeField] private PUMPBackground background;

    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        background.SetSerializeNodeInfos(structure.NodeInfos);
        background.RecordHistoryOncePerFrame();
    }

    public override string GetImagePath()
    {
        return ((RectTransform)background.Rect.parent).CaptureToFile();
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        return background.GetSerializeNodeInfos();
    }

    public override object GetTag()
    {
        return null;
    }
}
