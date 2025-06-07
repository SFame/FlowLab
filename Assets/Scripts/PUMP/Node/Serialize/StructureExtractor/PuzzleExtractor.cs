using System.Collections.Generic;
using UnityEngine;
public class PuzzleExtractor : SaveLoadStructureExtractor
{
    [SerializeField] private PUMPBackground background;
    [SerializeField] private PuzzleDataPanel puzzleDataPanel;

    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        background.SetInfos(structure.NodeInfos, true);
        Debug.Log("Applying NodeInfos to PuzzleBackground");

        if (structure.Tag is PuzzleData puzzleData)
        {
            Debug.Log("Applying PuzzleData to PuzzleDataPanel");
            puzzleDataPanel.currentPuzzleData = puzzleData;

            // TestCase UI
            ResetAndCreateTestCaseUI(puzzleData.testCases);
        }
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        return background.GetInfos();
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