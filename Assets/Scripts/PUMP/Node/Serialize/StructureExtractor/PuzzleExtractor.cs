using System.Collections.Generic;
using UnityEngine;
using Utils;

public class PuzzleExtractor : SaveLoadStructureExtractor
{
    [SerializeField] private PUMPBackground background;
    [SerializeField] private PuzzleDataPanel puzzleDataPanel;

    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        background.SetSerializeNodeInfos(structure.NodeInfos, true);

        if (structure.Tag is PuzzleData puzzleData)
        {
            puzzleDataPanel.currentPuzzleData = puzzleData;

            // TestCase UI
            ResetAndCreateTestCaseUI(puzzleData.testCases);
        }
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
        puzzleDataPanel.SavePuzzleData();
        return puzzleDataPanel.currentPuzzleData;
    }

    private void ResetAndCreateTestCaseUI(List<TestCase> testCases)
    {
        // PuzzleDataPanel에 구현된 메서드 사용
        puzzleDataPanel.LoadFromTestCases(testCases);
    }
}
