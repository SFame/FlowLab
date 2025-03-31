using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;

public class PuzzleDataPanel : MonoBehaviour
{
    [SerializeField] private Transform testCaseContainer;
    [SerializeField] private Button addTestCaseButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openButton;

    [SerializeField] private GameObject testCaseItemPrefab;

    [SerializeField] private PUMPBackground background;

    [SerializeField] private PuzzleTestCasePanel testCasePanel;

    private List<TestCaseController> testCaseControllers = new List<TestCaseController>();
    public PuzzleData currentPuzzleData = new PuzzleData();

    public event Action<PuzzleData> OnPuzzleDataChanged;

    private void Start()
    {
        // 버튼 이벤트 설정
        addTestCaseButton.onClick.AddListener(AddNewTestCase);
        saveButton.onClick.AddListener(SavePuzzleData);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        closeButton.onClick.AddListener(() => openButton.gameObject.SetActive(true));

    }

    public void AdjustTestCasesToBackground()
    {
        if (background == null)
        {
            background = FindAnyObjectByType<PUMPBackground>();
            if (background == null)
            {
                Debug.LogError("PUMPBackground not found. Cannot adjust test cases.");
                return;
            }
        }

        foreach (TestCaseController controller in testCaseControllers)
        {
            controller.AdjustToBackground(background);
        }
    }

    public void AddNewTestCase()
    {
        GameObject newTestCaseItem = Instantiate(testCaseItemPrefab, testCaseContainer);
        TestCaseController controller = newTestCaseItem.GetComponent<TestCaseController>();

        if (controller != null)
        {
            controller.Initialize(this);
            testCaseControllers.Add(controller);

            if (background == null)
            {
                background = FindAnyObjectByType<PUMPBackground>();
            }

            if (background != null)
            {
                controller.AdjustToBackground(background);
            }
        }
        else
        {
            Debug.LogError("프리펩에 TestCaseController 컴포넌트 확인요망");
        }
    }

    public void RemoveTestCase(TestCaseController controller)
    {
        if (testCaseControllers.Count <= 1)
        {
            Debug.LogWarning("최소 하나이상 필요");
            return;
        }

        testCaseControllers.Remove(controller);
        Destroy(controller.gameObject);
    }

    public void SavePuzzleData()
    {
        currentPuzzleData.testCases = new List<TestCase>();

        foreach (TestCaseController controller in testCaseControllers)
        {
            TestCase testCase = controller.GetTestCaseData();
            currentPuzzleData.testCases.Add(testCase);
        }

        OnPuzzleDataChanged?.Invoke(currentPuzzleData);
    }
    // 기존 테스트 케이스 UI 제거
    public void ClearTestCases()
    {
        foreach (var controller in testCaseControllers)
        {
            Destroy(controller.gameObject);
        }
        testCaseControllers.Clear();
    }
    // 퍼즐 데이터로부터 테스트 케이스 UI 로드
    public void LoadFromPuzzleData(PuzzleData puzzleData)
    {
        // 기존 UI 제거
        ClearTestCases();

        // 새 데이터 설정
        UpdatePuzzleData(puzzleData);

        // 테스트 케이스가 없으면 빈 케이스 하나 추가
        if (puzzleData.testCases == null || puzzleData.testCases.Count == 0)
        {
            AddNewTestCase();
            return;
        }

        // 테스트 케이스 UI 생성
        foreach (var testCase in puzzleData.testCases)
        {
            // 새 테스트 케이스 UI 추가
            GameObject newTestCaseItem = Instantiate(testCaseItemPrefab, testCaseContainer);
            TestCaseController controller = newTestCaseItem.GetComponent<TestCaseController>();

            if (controller != null)
            {
                controller.Initialize(this);
                controller.SetTestCaseData(testCase);
                testCaseControllers.Add(controller);
            }
        }
    }

    // 테스트 케이스 배열에서 직접 UI 생성 (PuzzleExtractor에서 사용)
    public void LoadFromTestCases(List<TestCase> testCases)
    {
        LoadFromPuzzleData(new PuzzleData { testCases = testCases });
    }
    public void UpdatePuzzleData(PuzzleData newData)
    {
        currentPuzzleData = newData;
        OnPuzzleDataChanged?.Invoke(currentPuzzleData);
    }

}
