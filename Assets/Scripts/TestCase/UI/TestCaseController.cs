using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TestCaseController : MonoBehaviour
{
    [SerializeField] private Button removeButton;
    [SerializeField] private Transform inputTogglesContainer;
    [SerializeField] private Transform outputTogglesContainer;
    [SerializeField] private Button addInputToggleButton;
    [SerializeField] private Button removeInputToggleButton;
    [SerializeField] private Button addOutputToggleButton;
    [SerializeField] private Button removeOutputToggleButton;

    [SerializeField] private GameObject togglePrefab;

    private PuzzleDataPanel puzzlePanel;
    private PuzzleBackground puzzleBackground;

    private List<Toggle> inputToggles = new List<Toggle>();
    private List<Toggle> outputToggles = new List<Toggle>();

    public void Initialize(PuzzleDataPanel puzzlePanel)
    {
        this.puzzlePanel = puzzlePanel;

        // 버튼 이벤트 설정
        removeButton.onClick.AddListener(() => puzzlePanel.RemoveTestCase(this));

        addInputToggleButton.onClick.AddListener(AddInputToggle);
        removeInputToggleButton.onClick.AddListener(RemoveInputToggle);

        addOutputToggleButton.onClick.AddListener(AddOutputToggle);
        removeOutputToggleButton.onClick.AddListener(RemoveOutputToggle);

    }
    public void AdjustToBackground(PUMPBackground background)
    {
        if (background == null)
            return;

        // 기존 토글 제거
        ClearAllToggles();

        // External Input 개수에 맞게 input 토글 생성
        int inputCount = background.ExternalInput.GateCount;
        for (int i = 0; i < inputCount; i++)
        {
            AddInputToggle();
        }

        // External Output 개수에 맞게 output 토글 생성
        int outputCount = background.ExternalOutput.GateCount;
        for (int i = 0; i < outputCount; i++)
        {
            AddOutputToggle();
        }
    }
    public void AddInputToggle()
    {
        Toggle newToggle = CreateToggle(inputTogglesContainer);
        inputToggles.Add(newToggle);
        UpdateToggleLabels(inputToggles, "IN");
    }

    public void RemoveInputToggle()
    {
        if (inputToggles.Count <= 1)
        {
            Debug.LogWarning("Cannot remove the last input toggle!");
            return;
        }

        Toggle lastToggle = inputToggles[inputToggles.Count - 1];
        inputToggles.Remove(lastToggle);
        Destroy(lastToggle.gameObject);

        UpdateToggleLabels(inputToggles, "IN");
    }

    public void AddOutputToggle()
    {
        Toggle newToggle = CreateToggle(outputTogglesContainer);
        outputToggles.Add(newToggle);
        UpdateToggleLabels(outputToggles, "OUT");
    }

    public void RemoveOutputToggle()
    {
        if (outputToggles.Count <= 1)
        {
            Debug.LogWarning("Cannot remove the last output toggle!");
            return;
        }

        Toggle lastToggle = outputToggles[outputToggles.Count - 1];
        outputToggles.Remove(lastToggle);
        Destroy(lastToggle.gameObject);

        UpdateToggleLabels(outputToggles, "OUT");
    }

    private Toggle CreateToggle(Transform parent)
    {
        GameObject toggleObj = Instantiate(togglePrefab, parent);
        return toggleObj.GetComponent<Toggle>();
    }

    private void UpdateToggleLabels(List<Toggle> toggles, string prefix)
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            TMPro.TextMeshProUGUI label = toggles[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{prefix} {i}";
            }
        }
    }
    public TestCase GetTestCaseData()
    {
        TestCase testCase = new TestCase();

        testCase.ExternalInputStates = new List<bool>();
        foreach (Toggle toggle in inputToggles)
        {
            testCase.ExternalInputStates.Add(toggle.isOn);
        }

        testCase.ExternalOutputStates = new List<bool>();
        foreach (Toggle toggle in outputToggles)
        {
            testCase.ExternalOutputStates.Add(toggle.isOn);
        }

        return testCase;
    }
    // 테스트 케이스 데이터를 UI에 적용
    public void SetTestCaseData(TestCase testCase)
    {
        // 기존 UI 요소 정리
        ClearAllToggles();

        // 인풋 토글 생성 및 설정
        if (testCase.ExternalInputStates != null)
        {
            for (int i = 0; i < testCase.ExternalInputStates.Count; i++)
            {
                // 필요한 만큼 토글 추가
                if (i >= inputToggles.Count)
                {
                    AddInputToggle();
                }

                // 상태 설정
                if (i < inputToggles.Count)
                {
                    inputToggles[i].isOn = testCase.ExternalInputStates[i];
                }
            }
        }

        // 아웃풋 토글 생성 및 설정
        if (testCase.ExternalOutputStates != null)
        {
            for (int i = 0; i < testCase.ExternalOutputStates.Count; i++)
            {
                // 필요한 만큼 토글 추가
                if (i >= outputToggles.Count)
                {
                    AddOutputToggle();
                }

                // 상태 설정
                if (i < outputToggles.Count)
                {
                    outputToggles[i].isOn = testCase.ExternalOutputStates[i];
                }
            }
        }
    }

    // 모든 토글 제거
    private void ClearAllToggles()
    {
        // 인풋 토글 제거
        foreach (var toggle in inputToggles)
        {
            Destroy(toggle.gameObject);
        }
        inputToggles.Clear();

        // 아웃풋 토글 제거
        foreach (var toggle in outputToggles)
        {
            Destroy(toggle.gameObject);
        }
        outputToggles.Clear();
    }
}
