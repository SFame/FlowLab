using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleTestCasePanel : MonoBehaviour
{
    [SerializeField] private Transform testCaseSection;
    [SerializeField] private GameObject puzzleTestCaseItemPrefab;
    [SerializeField] private PuzzleBackground puzzleBackground;

    private List<PuzzleTestCaseItem> testCaseItems = new List<PuzzleTestCaseItem>();

    [SerializeField]  private Animator animator;
    private bool isShow = true;

    private void Awake()
    {
        
        if (animator != null)
            animator.SetBool("IsShow", true);
    }

    private void OnEnable()
    {
        if (puzzleBackground != null)
        {
            puzzleBackground.OnTestCaseComplete += HandleTestCaseResult;
            puzzleBackground.OnTestCaseResultDetailed += HandleDetailedTestCaseResult;
        }


    }

    private void OnDisable()
    {
        if (puzzleBackground != null)
        {
            puzzleBackground.OnTestCaseComplete -= HandleTestCaseResult;
            puzzleBackground.OnTestCaseResultDetailed -= HandleDetailedTestCaseResult;

        }
            

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
            testCaseItems.Add(testCaseItem);
        }
    }

    public void ClearTestCases()
    {
        foreach (var item in testCaseItems)
        {
            Destroy(item.gameObject);
            Debug.Log("Destroying test case item");
        }
        testCaseItems.Clear();
    }

    public void UpdateTestCaseResult(int index, bool passed)
    {
        if (index >= 0 && index < testCaseItems.Count)
        {
            PuzzleTestCaseItem item = testCaseItems[index];
            item?.SetValidationResult(passed);
        }
    }

    private void HandleTestCaseResult(int index, bool passed)
    {
        if (!isShow)
            ShowPanel();

        UpdateTestCaseResult(index, passed);
    }

    public void UpdateTestCaseDetailedResult(int index, bool[] actualOutputs, bool passed)
    {
        if (index >= 0 && index < testCaseItems.Count)
        {
            PuzzleTestCaseItem item = testCaseItems[index];
            item?.SetDetailedValidationResult(actualOutputs, passed);
        }
    }
    private void HandleDetailedTestCaseResult(int index, bool[] actualOutputs, bool passed)
    {

        if (!isShow)
            ShowPanel();

        UpdateTestCaseDetailedResult(index, actualOutputs, passed);
    }
    public void ShowPanel()
    {
        animator.SetBool("IsShow", true);
        isShow = true;
    }
    public void HidePanel()
    {
        animator.SetBool("IsShow", false);
        isShow = false;
    }
}