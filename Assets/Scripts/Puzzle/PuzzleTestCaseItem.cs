using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleTestCaseItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI caseNumberText;
    [SerializeField] private Transform inputContainer;
    [SerializeField] private Transform outputContainer;
    [SerializeField] private GameObject togglePrefab;

    [SerializeField] private Color passedColor = Color.white;
    [SerializeField] private Color failedColor = Color.red;

    private List<Toggle> inputToggles = new List<Toggle>();
    private List<Toggle> outputToggles = new List<Toggle>();

    public void SetupTestCase(TestCase testCase, int index)
    {
        caseNumberText.text = $"Case #{index + 1}";
        caseNumberText.color = passedColor;

        ClearToggles();


        if (testCase.ExternalInputStates != null)
        {
            for (int i = 0; i < testCase.ExternalInputStates.Count; i++)
            {
                Toggle toggle = CreateToggle(inputContainer, $"IN {i}", testCase.ExternalInputStates[i]);
                inputToggles.Add(toggle);
            }
        }

        // Create output toggles
        if (testCase.ExternalOutputStates != null)
        {
            for (int i = 0; i < testCase.ExternalOutputStates.Count; i++)
            {
                Toggle toggle = CreateToggle(outputContainer, $"OUT {i}", testCase.ExternalOutputStates[i]);
                outputToggles.Add(toggle);
            }
        }

       
    }

    private Toggle CreateToggle(Transform parent, string label, bool isOn)
    {
        GameObject toggleObj = Instantiate(togglePrefab, parent);
        Toggle toggle = toggleObj.GetComponent<Toggle>();
        Text labelText = toggle.GetComponentInChildren<Text>();

        if (toggle != null)
        {
            toggle.isOn = isOn;
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
    }
}