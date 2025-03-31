using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleBackground : MonoBehaviour
{
    [SerializeField] private PUMPBackground pumpBackground;
    [SerializeField] private PuzzleDataPanel puzzleDataPanel;

    [SerializeField] private Button testButton;

    [SerializeField] private float testCaseDelay = 0.2f;

    //[SerializeField] private PuzzleData currentPuzzleData;
    private bool isValidating = false;

    // 테스트 결과 이벤트
    public event Action<bool> OnValidationComplete;
    public event Action<int, bool> OnTestCaseComplete; // 테스트케이스 인덱스, 성공 여부

    private void Start()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(() => ValidateAllTestCases().Forget());
        }
    }

    // PUMPSaveDataStructure에서 PuzzleData 설정
    public void SetPuzzleDataFromSaveData(PUMPSaveDataStructure saveData)
    {
        if (saveData.Tag is PuzzleData puzzleData)
        {
            //currentPuzzleData = puzzleData;
            //Debug.Log($"Loaded PuzzleData with {currentPuzzleData.testCases?.Count ?? 0} test cases.");
        }
        else
        {
            Debug.LogWarning("SaveData does not contain valid PuzzleData.");
        }
    }

    // 모든 테스트케이스 검증
    public async UniTaskVoid ValidateAllTestCases()
    {
        if (isValidating || puzzleDataPanel.currentPuzzleData.testCases == null || puzzleDataPanel.currentPuzzleData.testCases.Count == 0)
        {
            Debug.LogWarning("No test cases to validate or validation already in progress.");
            return;
        }

        isValidating = true;
        bool allTestsPassed = true;

        // UI 업데이트 등 필요한 초기화 작업
        Debug.Log("Starting validation of all test cases...");

        for (int i = 0; i < puzzleDataPanel.currentPuzzleData.testCases.Count; i++)
        {
            TestCase testCase = puzzleDataPanel.currentPuzzleData.testCases[i];
            bool testPassed = await ValidateTestCase(testCase, i);

            // 각 테스트케이스 결과 알림
            OnTestCaseComplete?.Invoke(i, testPassed);

            if (!testPassed)
            {
                allTestsPassed = false;
            }

            // 테스트 사이에 딜레이 추가
            await UniTask.Delay(TimeSpan.FromSeconds(testCaseDelay));
        }

        // 모든 테스트 결과 알림
        OnValidationComplete?.Invoke(allTestsPassed);
        Debug.Log($"All test cases validation completed. Result: {(allTestsPassed ? "PASSED" : "FAILED")}");

        isValidating = false;
    }
    // 단일 테스트케이스 검증
    private async UniTask<bool> ValidateTestCase(TestCase testCase, int index)
    {
        if (pumpBackground == null || pumpBackground.ExternalInput == null || pumpBackground.ExternalOutput == null)
        {
            Debug.LogError("PUMPBackground or its components are not set correctly.");
            return false;
        }

        Debug.Log($"Validating test case {index}...");

        // 입력값 설정
        if (testCase.ExternalInputStates != null)
        {
            for (int i = 0; i < testCase.ExternalInputStates.Count && i < pumpBackground.ExternalInput.GateCount; i++)
            {
                pumpBackground.ExternalInput[i].State = testCase.ExternalInputStates[i];
            }
        }

        // 로직이 실행될 시간을 주기 위해 대기
        await UniTask.Delay(TimeSpan.FromSeconds(testCaseDelay));

        // 출력값 검증
        bool testPassed = true;
        if (testCase.ExternalOutputStates != null)
        {
            for (int i = 0; i < testCase.ExternalOutputStates.Count && i < pumpBackground.ExternalOutput.GateCount; i++)
            {
                bool expected = testCase.ExternalOutputStates[i];
                bool actual = pumpBackground.ExternalOutput[i].State;

                if (expected != actual)
                {
                    Debug.Log($"Test case {index} failed at output {i}: Expected {expected}, got {actual}");
                    testPassed = false;
                    break;
                }
            }
        }

        Debug.Log($"Test case {index} validation result: {(testPassed ? "PASSED" : "FAILED")}");
        return testPassed;
    }
}
