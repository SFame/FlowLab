using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleTestCasePanel : MonoBehaviour, ISoundable
{
    [SerializeField] private Transform testCaseSection;
    [SerializeField] private GameObject puzzleTestCaseItemPrefab;
    [SerializeField] private PuzzleBackground puzzleBackground;

    private List<PuzzleTestCaseItem> testCaseItems = new List<PuzzleTestCaseItem>();

    [SerializeField]  private Animator animator;
    private bool isShow = true;

    public event SoundEventHandler OnSounded;

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
            //결과 이미지 호출과 함께 사운드 호출
            // 결과에 따라 다른 사운드
            if (passed)
            {
                OnSounded?.Invoke(this, new (1, gameObject.transform.position)); // 성공 사운드 1
            }
            else
            {
                OnSounded?.Invoke(this, new (2, gameObject.transform.position)); // 실패 사운드 2
            }
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

        OnSounded?.Invoke(this, new (0, gameObject.transform.position)); // Panel 사운드 0

        for (int i = 0; i < testCaseItems.Count; i++)
        {
            testCaseItems[i].ResetResult(); // 처음 상태의 테스트케이스UI로 초기화 - 창을 닫고,열때마다
        }
    }
    public void HidePanel()
    {
        animator.SetBool("IsShow", false);
        isShow = false;

        OnSounded?.Invoke(this, new (0, gameObject.transform.position)); // Panel 사운드 0

    }
}