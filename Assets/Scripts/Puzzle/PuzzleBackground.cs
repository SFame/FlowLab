using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleBackground : MonoBehaviour, ISoundable
{
    public PUMPBackground pumpBackground;
    public PuzzleTestCasePanel testCasePanel;
    public GameObject ErrorUIPrefab;
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private Button testButton;
    public Button exitButton;

    [SerializeField] private float testCaseDelay = 0.3f;

    public PUMPSaveDataStructure currentData;
    public PuzzleData currentPuzzleData;
    private bool isValidating = false;


    // 테스트 결과 이벤트
    public event Action<bool> OnValidationComplete;
    public event Action<int, bool> OnTestCaseComplete; // 테스트케이스 인덱스, 성공 여부

    // 테스트케이스 판넬 속 Output을 위한 이벤트
    public event Action<int, bool[], bool> OnTestCaseResultDetailed;

    public event SoundEventHandler OnSounded;
    
    public PuzzleInteraction puzzleInteraction;


    private void Start()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(() => ValidateAllTestCases().Forget());
        }

        UpdateTimerDisplay(0);

    }
    private void Update()
    {
        UpdateTimerDisplay(puzzleInteraction._clearTime);
    }
    public void UpdateTimerDisplay(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    public void SetPuzzleData(PuzzleData puzzleData)
    {
        currentPuzzleData = puzzleData;
        Debug.Log($"Loaded PuzzleData with {currentPuzzleData.testCases?.Count ?? 0} test cases");
    }

    // PUMPSaveDataStructure에서 PuzzleData 설정
    public void SetPuzzleDataFromSaveData(PUMPSaveDataStructure saveData)
    {
        if (saveData.Tag is PuzzleData puzzleData)
        {
            currentPuzzleData = puzzleData;
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
        //OnSounded?.Invoke(this, new (0, gameObject.transform.position)); // 검증 사운드 0.... 그냥 버튼 Onclick에 사운드 붙이는게 낫나?

        if (isValidating || currentPuzzleData.testCases == null || currentPuzzleData.testCases.Count == 0)
        {
            Debug.LogWarning("No test cases to validate or validation already in progress.");
            return;
        }

        pumpBackground.CanInteractive = false;
        isValidating = true;
        bool allTestsPassed = true;

        Debug.Log("Starting validation of all test cases...");

        for (int i = 0; i < currentPuzzleData.testCases.Count; i++)
        {

            TestCase testCase = currentPuzzleData.testCases[i];
            bool testPassed = await ValidateTestCase(testCase, i);

            OnTestCaseComplete?.Invoke(i, testPassed);

            if (!testPassed)
            {
                allTestsPassed = false;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(testCaseDelay));
        }

        OnValidationComplete?.Invoke(allTestsPassed);
        Debug.Log($"All test cases validation completed. Result: {(allTestsPassed ? "PASSED" : "FAILED")}");

        pumpBackground.CanInteractive = true;
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
                if (!(pumpBackground.ExternalInput[i].Type == TransitionType.Bool))
                {
                    pumpBackground.CanInteractive = true;
                    isValidating = false;
                    // error ui, 리턴
                    Debug.LogError($"Input {i} is not of type Bool.");
                    ErrorUIPrefab.SetActive(true); // 에러 UI 활성화
                    return false;
                }
                pumpBackground.ExternalInput[i].State = testCase.ExternalInputStates[i];
            }
        }

        // 로직이 실행될 시간을 주기 위해 대기
        await UniTask.Delay(TimeSpan.FromSeconds(testCaseDelay));

        // 출력값 검증
        bool testPassed = true;
        for (int i = 0; i < pumpBackground.ExternalOutput.GateCount; i++)
        {
            if (!(pumpBackground.ExternalOutput[i].Type == TransitionType.Bool))
            {
                pumpBackground.CanInteractive = true;
                isValidating = false;
                testPassed = false;
                // error ui, 리턴
                Debug.LogError($"Output {i} is not of type Bool.");
                ErrorUIPrefab.SetActive(true); // 에러 UI 활성화
                return false;
            }
        }
        bool[] actualOutputStates = new bool[pumpBackground.ExternalOutput.GateCount]; // 실제 출력 상태를 저장할 배열
        if (testCase.ExternalOutputStates != null)
        {
            for (int i = 0; i < testCase.ExternalOutputStates.Count; i++)
            {
                bool expected = testCase.ExternalOutputStates[i]; // 예상 출력 상태 - 유저가 만들어야 하는 상태
                bool actual = pumpBackground.ExternalOutput[i].State; // 실제 출력 상태 - 현재 퍼즐의 상태

                actualOutputStates[i] = actual; // 실제 출력을 저장 -> 테스트케이스Item 속 Toggle들을 켜기 위함

                if (expected != actual)
                {
                    Debug.Log($"Test case {index} failed at output {i}: Expected {expected}, got {actual}");
                    testPassed = false;
                }
            }
        }
        OnTestCaseResultDetailed?.Invoke(index, actualOutputStates, testPassed);

        Debug.Log($"Test case {index} validation result: {(testPassed ? "PASSED" : "FAILED")}");
        return testPassed;
    }

    void UI_Interactive(bool puzzleFinish)
    {
        pumpBackground.CanInteractive = puzzleFinish;
        isValidating = !puzzleFinish;
    }
 
}
