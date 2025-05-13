using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.IO;
using Unity.VisualScripting;

[Serializable]
public class DialogueChoice
{
    public string choiceText; // 선택지 텍스트
    public string nextDialogueId; // 선택 시 전환될 대화 ID
    public string eventId; // 선택 시 발생할 이벤트 ID
}
[Serializable]
public class DialogueEntry
{
    public string speakerName;
    public string messageText;
    public string speakerImagePath;

    public List<DialogueChoice> choices = new List<DialogueChoice>(); // 선택지 목록
    public bool hasChoices => choices != null && choices.Count > 0;
}

[Serializable]
public class DialogueSequence
{
    public string id;
    public string title;
    public List<DialogueEntry> entries = new List<DialogueEntry>();
}

/// <summary>
/// 모든 대화 시퀸스를 저장하는 DB
/// </summary>
[Serializable]
public class DialogueDatabase
{
    public List<DialogueSequence> sequences = new List<DialogueSequence>();
}

public static class TextDisplay
{
    // Configuration constants
    private const string DIALOGUE_PREFAB_PATH = "Prefab/UI/UI_TextMessage";
    private const string CHOICE_BUTTON_PREFAB_PATH = "Prefab/UI/DialogueChoiceButton";
    private const string DIALOGUE_DATABASE_PATH = "Dialogues/dialogue_data";

    // Events
    public static event Action<string> OnDialogueStarted;
    public static event Action<string> OnDialogueEnded;
    public static event Action<int, DialogueEntry> OnDialogueAdvanced; // 현재 인덱스, 엔트리

    public static event Action<string, string> OnChoiceMade; // 선택한 eventId, nextDialogueId

    // Runtime state
    private static GameObject _dialoguePanelInstance;
    private static DialogueDatabase _dialogueDatabase;
    private static DialogueSequence _currentSequence;
    private static int _currentEntryIndex = -1;
    private static bool _isDisplayingDialogue = false;
    private static CancellationTokenSource _typingCts;

    private static GameObject _choiceButtonPrefab;
    private static List<GameObject> _activeChoiceButtons = new List<GameObject>();

    // References to UI components
    private static TextMeshProUGUI _messageText;
    private static TextMeshProUGUI _speakerNameText;
    private static Image _speakerImage;
    private static Image _backgroundPanel;
    private static Image _textPanel;

    private static Transform _choiceContainer;

    // Configuration
    private static float _typingSpeed = 0.03f;
    private static bool _useTypewriterEffect = true;


    static TextDisplay()
    {
        LoadDialogueDatabase();
    }

    private static void LoadDialogueDatabase()
    {
        try
        {
            TextAsset databaseFile = Resources.Load<TextAsset>(DIALOGUE_DATABASE_PATH);
            if (databaseFile != null)
            {
                _dialogueDatabase = JsonUtility.FromJson<DialogueDatabase>(databaseFile.text);
                Debug.Log($"Dialogue database loaded with {_dialogueDatabase.sequences.Count} sequences");
            }
            else
            {
                Debug.LogError("Failed to load dialogue database. Make sure the file exists at Resources/" + DIALOGUE_DATABASE_PATH);
                _dialogueDatabase = new DialogueDatabase();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading dialogue database: {e.Message}");
            _dialogueDatabase = new DialogueDatabase();
        }
    }


    public static void ShowDialogue(string dialogueId)
    {
        // Find the dialogue sequence with the given ID
        DialogueSequence sequence = _dialogueDatabase.sequences.Find(seq => seq.id == dialogueId);
        if (sequence == null)
        {
            Debug.LogError($"Dialogue sequence with ID '{dialogueId}' not found");
            return;
        }

        ShowDialogueSequence(sequence).Forget();
    }


    
    private static async UniTaskVoid ShowDialogueSequence(DialogueSequence sequence)
    {
        if (_isDisplayingDialogue)
        {
            // 이전 대화의 상태만 초기화하고 UI는 계속 유지
            _currentSequence = null;
            _currentEntryIndex = -1;
            ClearChoiceButtons();
            _typingCts?.Cancel();
        }

        _currentSequence = sequence;
        _currentEntryIndex = -1;
        _isDisplayingDialogue = true;

        await EnsureDialoguePanelExists();

        _dialoguePanelInstance.SetActive(true);

        OnDialogueStarted?.Invoke(_currentSequence.id);

        AdvanceDialogue();
    }

    /// 이벤트 시스템이 존재하는지 확인하고 없으면 생성
    private static void EnsureEventSystemExists()
    {

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.Log("No EventSystem found in the scene. Creating one...");

            
            GameObject eventSystemObject = new GameObject("EventSystem");

            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();

            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            GameObject.DontDestroyOnLoad(eventSystemObject);

            Debug.Log("EventSystem created successfully");
        }
    }

    /// 대화 패널 프리팹이 인스턴스화되어 있는지 확인
    private static async UniTask EnsureDialoguePanelExists()
    {
        EnsureEventSystemExists();

        _choiceButtonPrefab = Resources.Load<GameObject>(CHOICE_BUTTON_PREFAB_PATH);
        if (_choiceButtonPrefab == null)
        {
            Debug.LogWarning($"선택지 버튼 프리팹을 찾을 수 없습니다: {CHOICE_BUTTON_PREFAB_PATH}");
        }
        if (_dialoguePanelInstance == null)
        {
            GameObject prefab = Resources.Load<GameObject>(DIALOGUE_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"대화 패널 프리팹을 찾을 수 없습니다: {DIALOGUE_PREFAB_PATH}");
                return;
            }

            _dialoguePanelInstance = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(_dialoguePanelInstance);

            _messageText = _dialoguePanelInstance.transform.Find("TextPanel/MessageText")?.GetComponent<TextMeshProUGUI>();
            _speakerNameText = _dialoguePanelInstance.transform.Find("TextPanel/SpeakerText")?.GetComponent<TextMeshProUGUI>();
            _speakerImage = _dialoguePanelInstance.transform.Find("TextPanel/SpeakerImage")?.GetComponent<Image>();
            _backgroundPanel = _dialoguePanelInstance.transform.Find("Background")?.GetComponent<Image>();
            _textPanel = _dialoguePanelInstance.transform.Find("TextPanel")?.GetComponent<Image>();
            _choiceContainer = _dialoguePanelInstance.transform.Find("ChoiceContainer");

            if (_backgroundPanel != null)
            {
                Button backgroundButton = _backgroundPanel.gameObject.GetOrAddComponent<Button>();
                backgroundButton.onClick.AddListener(AdvanceDialogue);

            }

            if (_textPanel != null)
            {
                Button textPanelButton = _textPanel.gameObject.GetOrAddComponent<Button>();
                textPanelButton.onClick.AddListener(AdvanceDialogue);

            }

            _dialoguePanelInstance.SetActive(false);

            await UniTask.Yield();
        }
    }


    public static void AdvanceDialogue()
    {
        if (!_isDisplayingDialogue)
            return;

        // 선택지가 활성화된 상태면 클릭으로 진행하지 않음
        if (_activeChoiceButtons.Count > 0)
        {
            return;
        }
        //진행중인 타이핑 취소 - 타이핑중 클릭으로 넘어가려할때
        _typingCts?.Cancel();

        // 만약 텍스트가 다 표시되지 않았다면
        // 텍스트를 모두 표시하고 리턴
        if (_typingCts != null && _messageText.maxVisibleCharacters < _messageText.text.Length)
        {
            _messageText.maxVisibleCharacters = _messageText.text.Length;
            return;
        }

        _currentEntryIndex++;

        // 마지막 대사인가?
        if (_currentEntryIndex >= _currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        // 현재 대화 가져오기
        DialogueEntry entry = _currentSequence.entries[_currentEntryIndex];

        // 텍스트 표시 업데이트
        UpdateDialogueUI(entry);

        // 리스너에게 알림
        OnDialogueAdvanced?.Invoke(_currentEntryIndex, entry);
    }


    private static void UpdateDialogueUI(DialogueEntry entry)
    {
        // 스피커 이름 업뎃
        if (_speakerNameText != null)
        {
            _speakerNameText.text = entry.speakerName;
        }

        // 메세지 텍스트 업데이트
        if (_messageText != null)
        {
            _messageText.text = entry.messageText;

            // 타이핑 효과 사용시
            if (_useTypewriterEffect)
            {
                _typingCts = new CancellationTokenSource();
                TypewriterEffect(_messageText, _typingCts.Token).Forget();
            }
        }

        // 스피커 이미지 업데이트 (있을때만, 없으면 LogWarning)
        if (_speakerImage != null && !string.IsNullOrEmpty(entry.speakerImagePath))
        {
            Sprite sprite = Resources.Load<Sprite>(entry.speakerImagePath);
            if (sprite != null)
            {
                _speakerImage.sprite = sprite;
                _speakerImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Speaker image not found at {entry.speakerImagePath}");
                _speakerImage.enabled = false;
            }
        }
        else if (_speakerImage != null)
        {
            _speakerImage.enabled = false;
        }

        // 선택지 표시(있는 경우)
        ClearChoiceButtons(); // 이전 선택지 제거

        if (entry.hasChoices)
        {
            // 타이핑 효과가 끝난 후 선택지 표시
            if (_useTypewriterEffect)
            {
                ShowChoicesAfterTyping(entry).Forget();
            }
            else
            {
                ShowChoices(entry.choices);
            }
        }
    }
    /// <summary>
    /// 타이핑 효과가 끝난 후 선택지 표시
    /// </summary>
    private static async UniTaskVoid ShowChoicesAfterTyping(DialogueEntry entry)
    {
        // 타이핑이 끝날 때까지 대기
        while (_messageText.maxVisibleCharacters < _messageText.text.Length)
        {
            await UniTask.Yield();

            // 대화가 중단된 경우 종료
            if (!_isDisplayingDialogue)
                return;
        }

        // 타이핑이 끝나면 선택지 표시
        ShowChoices(entry.choices);
    }

    /// <summary>
    /// 선택지 버튼 표시
    /// </summary>
    private static void ShowChoices(List<DialogueChoice> choices)
    {
        if (_choiceContainer == null)
        {
            Debug.LogError("선택지 컨테이너가 설정되지 않았습니다. 선택지를 표시할 수 없습니다.");
            return;
        }
        // 선택지가 없으면 리턴
        if (choices == null || choices.Count == 0)
            return;

       

        // 각 선택지마다 버튼 생성
        foreach (DialogueChoice choice in choices)
        {
            GameObject buttonObj = GameObject.Instantiate(_choiceButtonPrefab, _choiceContainer);

            // 버튼 텍스트 설정
            TextMeshProUGUI textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = choice.choiceText;
            }

            // 버튼 동작 설정
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // 클로저 문제를 피하기 위해 로컬 변수에 저장
                DialogueChoice capturedChoice = choice;
                button.onClick.AddListener(() => HandleChoiceSelected(capturedChoice));
            }

            // 활성 버튼 목록에 추가
            _activeChoiceButtons.Add(buttonObj);
        }
    }

    // 타이핑 효과
    private static async UniTaskVoid TypewriterEffect(TextMeshProUGUI textComponent, CancellationToken cancellationToken)
    {
        if (textComponent == null) return;

        textComponent.maxVisibleCharacters = 0;

        try
        {
            for (int i = 0; i <= textComponent.text.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                textComponent.maxVisibleCharacters = i;
                await UniTask.Delay(TimeSpan.FromSeconds(_typingSpeed), cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 타이핑이 취소됨, 대화 진행 시 정상적인 상황
        }
    }
    // 선택지 버튼 클릭 처리
    private static void HandleChoiceSelected(DialogueChoice choice)
    {
        // 선택지 버튼 정리
        ClearChoiceButtons();

        // 리스너에게 알림
        OnChoiceMade?.Invoke(choice.eventId, choice.nextDialogueId);

        // 다음 대화가 있다면 표시
        if (!string.IsNullOrEmpty(choice.nextDialogueId))
        {
            // 현재 대화 종료
            string currentDialogueId = _currentSequence.id;
            _currentSequence = null;  // 현재 시퀀스 참조 제거

            // 대화 패널은 비활성화하지 않음 (연속적인 대화 흐름을 위해)
            _isDisplayingDialogue = true;

            // 다음 대화 시작
            ShowDialogue(choice.nextDialogueId);
        }
        else
        {
            // 다음 대화가 없으면 현재 대화의 다음 대사로 진행
            // 현재 대화가 이미 끝났는지 확인
            if (_currentEntryIndex >= _currentSequence.entries.Count - 1)
            {
                EndDialogue();
            }
            else
            {
                AdvanceDialogue();
            }
        }
    }
    // 모든 선택지 버튼 정리
    private static void ClearChoiceButtons()
    {
        foreach (GameObject button in _activeChoiceButtons)
        {
            if (button != null)
            {
                GameObject.Destroy(button);
            }
        }

        _activeChoiceButtons.Clear();
    }


    public static void EndDialogue()
    {
        if (!_isDisplayingDialogue)
            return;

        string dialogueId = _currentSequence.id;


        _typingCts?.Cancel();
        _typingCts?.Dispose();
        _typingCts = null;

        if (_dialoguePanelInstance != null)
        {
            _dialoguePanelInstance.SetActive(false);
        }

        _isDisplayingDialogue = false;
        _currentSequence = null;
        _currentEntryIndex = -1;

        // 현재 종료된 대화 ID 전달
        if (!string.IsNullOrEmpty(dialogueId))
        {
            OnDialogueEnded?.Invoke(dialogueId);
        }
    }

    // 임시 메세지 표시, 전체 대화 시퀀스 없이 간단한 메시지 표시
    public static void ShowMessage(string message, string speakerName = "", string speakerImagePath = "")
    {
        DialogueSequence sequence = new DialogueSequence
        {
            id = "temp_message",
            title = "Temporary Message",
            entries = new List<DialogueEntry>
            {
                new DialogueEntry
                {
                    speakerName = speakerName,
                    messageText = message,
                    speakerImagePath = speakerImagePath
                }
            }
        };

        ShowDialogueSequence(sequence).Forget();
    }

    // 텍스트 표시중인지 확인
    public static bool IsDialogueActive()
    {
        return _isDisplayingDialogue;
    }


    // 텍스트 타이핑 관련 설정
    public static void SetTypingSpeed(float secondsPerCharacter)
    {
        _typingSpeed = Mathf.Max(0.01f, secondsPerCharacter);
    }

    public static void SetTypewriterEffect(bool enabled)
    {
        _useTypewriterEffect = enabled;
    }
}