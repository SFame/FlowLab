using System.Collections.Generic;
using UnityEngine;
using Utils;

public class ClassedNodeExtractor : SaveLoadStructureExtractor
{
    [SerializeField] private ClassedNodePanel classedNodePanel;
    [SerializeField] private RectTransform captureTargetBackground;

    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        classedNodePanel.SetCurrent(structure);
    }

    public override string GetImagePath()
    {
        return captureTargetBackground.CaptureToFile();
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        return classedNodePanel.GetCurrent().PairBackground.GetSerializeNodeInfos();
    }

    public override object GetTag()
    {
        return classedNodePanel.GetCurrent().ClassedNode.Id;
    }
}
