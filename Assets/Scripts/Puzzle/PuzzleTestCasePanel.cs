using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleTestCasePanel : MonoBehaviour
{
    [SerializeField] private Transform testCaseSection;
    [SerializeField] private GameObject puzzleTestCaseItemPrefab;
    [SerializeField] private PuzzleBackground puzzleBackground;
    [SerializeField] private PuzzleDataPanel puzzleDataPanel;

    private List<GameObject> testCaseItems = new List<GameObject>();

    private void OnEnable()
    {
        if (puzzleBackground != null)
            puzzleBackground.OnTestCaseComplete += HandleTestCaseResult;

        if (puzzleDataPanel != null)
            puzzleDataPanel.OnPuzzleDataChanged += SetupTestCases;
    }

    private void OnDisable()
    {
        if (puzzleBackground != null)
            puzzleBackground.OnTestCaseComplete -= HandleTestCaseResult;

        if (puzzleDataPanel != null)
            puzzleDataPanel.OnPuzzleDataChanged -= SetupTestCases;
    }

    public void SetupTestCases(PuzzleData puzzleData)
    {
        ClearTestCases();

        if (puzzleData.testCases.Count == 0)
            return;

        for (int i = 0; i < puzzleData.testCases.Count; i++)
        {
            CreateTestCaseItem(puzzleData.testCases[i], i);
        }
    }

    private void CreateTestCaseItem(TestCase testCase, int index)
    {
        GameObject item = Instantiate(puzzleTestCaseItemPrefab, testCaseSection);
        PuzzleTestCaseItem testCaseItem = item.GetComponent<PuzzleTestCaseItem>();

        if (testCaseItem != null)
        {
            testCaseItem.SetupTestCase(testCase, index);
            testCaseItems.Add(item);
        }
    }

    private void ClearTestCases()
    {
        foreach (var item in testCaseItems)
        {
            Destroy(item);
        }
        testCaseItems.Clear();
    }

    // Method to update validation results
    public void UpdateTestCaseResult(int index, bool passed)
    {
        if (index >= 0 && index < testCaseItems.Count)
        {
            PuzzleTestCaseItem item = testCaseItems[index].GetComponent<PuzzleTestCaseItem>();
            item?.SetValidationResult(passed);
        }
    }

    private void HandleTestCaseResult(int index, bool passed)
    {
        UpdateTestCaseResult(index, passed);
    }
}