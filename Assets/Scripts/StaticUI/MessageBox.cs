using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MessageBox : MonoBehaviour, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_Title;
    [SerializeField] private Button[] m_Buttons;
    #endregion

    #region Privates
    private List<KeyValuePair<Button, TextMeshProUGUI>> _buttonPair;
    private List<GameObject> _buttonsGo;
    private List<KeyValuePair<Button, TextMeshProUGUI>> ButtonPair
    {
        get
        {
            _buttonPair ??= m_Buttons.Select(button => new KeyValuePair<Button, TextMeshProUGUI>(button, button.GetComponentInChildren<TextMeshProUGUI>())).ToList();
            return _buttonPair;
        }
    }

    private List<GameObject> ButtonsGo
    {
        get
        {
            _buttonsGo ??= m_Buttons.Select(button => button.gameObject).ToList();
            return _buttonsGo;
        }
    }

    private void BoxReset()
    {
        for (int i = 0; i < ButtonPair.Count; i++)
        {
            ButtonPair[i].Key.onClick.RemoveAllListeners();
            ButtonPair[i].Value.text = string.Empty;
            ButtonsGo[i].gameObject.SetActive(false);
        }

        m_Title.text = string.Empty;
    }

    private void Exit()
    {
        BoxReset();
        gameObject.SetActive(false);
        OnExit?.Invoke();
    }
    #endregion

    #region Interface
    public event Action OnExit;

    public void Set(string title, List<string> buttonTexts, List<Action> buttonActions)
    {
        BoxReset();

        if (buttonTexts.Count == buttonActions.Count && buttonActions.Count <= m_Buttons.Length)
        {
            m_Title.text = title;
            int count = buttonActions.Count;
            for (int i = 0; i < count; i++)
            {
                ButtonsGo[i].SetActive(true);
                int index = i;
                ButtonPair[i].Key.onClick.AddListener(() =>
                {
                    buttonActions[index]?.Invoke();
                    Exit();
                });
                ButtonPair[i].Value.text = buttonTexts[i];
            }
            gameObject.SetActive(true);
            return;
        }

        Debug.LogWarning("Button text count and action count mismatch or exceeds available buttons");
    }
    #endregion

    public void OnPointerClick(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);

        if (result.Count <= 0)
            return;

        if (result[0].gameObject == gameObject)
            Exit();
    }
}