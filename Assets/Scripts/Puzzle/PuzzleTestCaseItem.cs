using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Windows;

public class PuzzleTestCaseItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI caseNumberText;
    [SerializeField] private Transform inputContainer;
    [SerializeField] private Transform outputContainer;
    [SerializeField] private GameObject inputTogglePrefab;
    [SerializeField] private GameObject outputTogglePrefab;

    [SerializeField] private GameObject resultImageObject;
    [SerializeField] private Sprite passedSprite;
    [SerializeField] private Sprite failedSprite;

    [SerializeField] private Color passedColor = Color.white;
    [SerializeField] private Color failedColor = Color.red;


    private List<Toggle> inputToggles = new List<Toggle>();
    private List<Toggle> outputToggles = new List<Toggle>();
    public void SetupTestCase(TestCase testCase, int index)
    {
        caseNumberText.text = $"Case #{index + 1}";
        caseNumberText.color = passedColor;

        // 새 테스트 케이스 설정 시 결과 이미지 초기화 및 숨김
        if (resultImageObject != null)
        {
            resultImageObject.SetActive(false);
        }

        ClearToggles();


        if (testCase.ExternalInputStates != null)
        {
            for (int i = 0; i < testCase.ExternalInputStates.Count; i++)
            {
                Toggle toggle = CreateToggle(inputContainer, $"IN {i}", testCase.ExternalInputStates[i], true);
                inputToggles.Add(toggle);
            }
        }

        // Create output toggles
        if (testCase.ExternalOutputStates != null)
        {
            for (int i = 0; i < testCase.ExternalOutputStates.Count; i++)
            {
                Toggle toggle = CreateToggle(outputContainer, $"OUT {i}", testCase.ExternalOutputStates[i], false);
                outputToggles.Add(toggle);
            }
        }
    }

    private Toggle CreateToggle(Transform parent, string label, bool isOn, bool isInput)
    {
        GameObject prefab = isInput ? inputTogglePrefab : outputTogglePrefab;
        GameObject toggleObj = Instantiate(prefab, parent);
        Toggle toggle = toggleObj.GetComponent<Toggle>();
        Text labelText = toggle.GetComponentInChildren<Text>();

        if (toggle != null)
        {
            // Input 토글은 상태 그대로 설정
            if (isInput)
            {
                toggle.isOn = isOn;
            }
            // Output 토글은 항상 꺼진 상태로 설정
            else
            {
                toggle.isOn = false;

                // Effect 이미지를 직접 자식 중에서만 찾기
                Transform effectTransform = null;
                for (int i = 0; i < toggleObj.transform.childCount; i++)
                {
                    Transform child = toggleObj.transform.GetChild(i);
                    if (child.name == "Effect")
                    {
                        effectTransform = child;
                        break;
                    }
                }

                if (effectTransform != null)
                {
                    // 현재 상태 로깅
                    Debug.Log($"Output toggle '{label}': Setting Effect active = {isOn}");

                    // 활성화 상태 설정
                    effectTransform.gameObject.SetActive(isOn);

                }

            }

            toggle.interactable = false;
        }

        if (labelText != null)
        {
            labelText.text = label;
        }

        return toggle;
    }

    private void ClearToggles()
    {
        foreach (var toggle in inputToggles)
        {
            Destroy(toggle.gameObject);
        }
        inputToggles.Clear();

        foreach (var toggle in outputToggles)
        {
            Destroy(toggle.gameObject);
        }
        outputToggles.Clear();
    }

    public void SetValidationResult(bool passed)
    {
        caseNumberText.color = passed ? passedColor : failedColor;

        if (resultImageObject != null)
        {
            // 결과에 따른 스프라이트
            Image resultImage = resultImageObject.GetComponent<Image>();
            if (resultImage != null)
            {
                // 결과에 따라 적절한 스프라이트 설정
                resultImage.sprite = passed ? passedSprite : failedSprite;
            }

            // 결과 표시
            resultImageObject.SetActive(true);
            resultImageObject.GetComponent<Image>().color = passed ? passedColor : failedColor;

            // 파티클 효과 추가예정
        }
    }
    public void ResetResult()
    {
        // 결과 이미지 초기화 및 숨김
        if (resultImageObject != null)
        {
            resultImageObject.SetActive(false);
        }

        // 텍스트 색상 초기화
        caseNumberText.color = passedColor;

        // 모든 토글 초기화
        foreach (var toggle in outputToggles)
        {
            toggle.isOn = false;
        }
    }
    public void SetDetailedValidationResult(bool[] actualOutputs, bool passed)
    {

        // 실제 출력값에 따라 토글 상태만 업데이트하고, Effect는 변경하지 않음
        for (int i = 0; i < actualOutputs.Length && i < outputToggles.Count; i++)
        {
            outputToggles[i].isOn = actualOutputs[i];

            
        }
    }
}