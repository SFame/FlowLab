using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Serialization;

public static class CutsceneDisplay
{
    private const string CUTSCENE_PREFAB_PATH = "Prefab/UI/UI_Cutscene";
    private const string CUTSCENE_DATABASE_PATH = "Cutscenes/cutscene_data";

    public static event Action<string> OnCutsceneStarted;
    public static event Action<string> OnCutsceneEnded;
    public static event Action<CutsceneEntry> OnCutsceneAdvanced;

    private static GameObject _cutscenePanelInstance;
    private static CutsceneDatabase _cutsceneDatabase;
    private static CutsceneSequence _currentSequence;
    private static CutsceneEntry _currentEntry;
    private static bool _isDisplayingCutscene = false;
    private static CancellationTokenSource _cutsceneCts;

    private static Image _cutsceneImage;
    private static Image _overlayImage; // For transitions
    private static RectTransform _cutscenePanel;

    private static bool _initialized = false;

    static CutsceneDisplay()
    {
        LoadCutsceneDatabase();

        // Subscribe to TextDisplay event
        TextDisplay.OnDialogueEnded += HandleDialogueEnded;
    }
    private static void LoadCutsceneDatabase()
    {
        try
        {
            TextAsset databaseFile = Resources.Load<TextAsset>(CUTSCENE_DATABASE_PATH);
            if (databaseFile != null)
            {
                _cutsceneDatabase = JsonUtility.FromJson<CutsceneDatabase>(databaseFile.text);
                Debug.Log($"Cutscene database loaded with {_cutsceneDatabase.sequences.Count} sequences");
            }
            else
            {
                Debug.LogError("Failed to load cutscene database. Make sure the file exists at Resources/" + CUTSCENE_DATABASE_PATH);
                _cutsceneDatabase = new CutsceneDatabase();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading cutscene database: {e.Message}");
            _cutsceneDatabase = new CutsceneDatabase();
        }
    }

    public static void ShowCutscene(string cutsceneId)
    {
        foreach (CutsceneSequence sequence in _cutsceneDatabase.sequences)
        {
            CutsceneEntry entry = sequence.entries.Find(e => e.id == cutsceneId);
            if (entry != null)
            {
                _currentSequence = sequence;
                ShowCutsceneEntry(entry).Forget();
                return;
            }
        }

        Debug.LogError($"Cutscene with ID '{cutsceneId}' not found");
    }

    public static void ShowCutsceneSequence(string sequenceId)
    {
        CutsceneSequence sequence = _cutsceneDatabase.sequences.Find(seq => seq.cutsceneId == sequenceId);
        if (sequence == null || sequence.entries.Count == 0)
        {
            Debug.LogError($"Cutscene sequence with ID '{sequenceId}' not found or has no entries");
            return;
        }

        _currentSequence = sequence;
        ShowCutsceneEntry(sequence.entries[0]).Forget();
    }

    private static async UniTaskVoid ShowCutsceneEntry(CutsceneEntry entry)
    {
        if (_isDisplayingCutscene)
        {
            // 이미 진행 중인 컷신이 있다면 종료
            _cutsceneCts?.Cancel();
        }

        _currentEntry = entry;
        _isDisplayingCutscene = true;

        await EnsureCutscenePanelExists();

        _cutscenePanelInstance.SetActive(true);

        // 컷씬 시작 알림
        OnCutsceneStarted?.Invoke(entry.id);

        // 컷씬 이미지 표시
        await DisplayCutsceneImage(entry);

        // 대화 ID가 있다면 대화 호출해서 표시
        if (!string.IsNullOrEmpty(entry.dialogueId))
        {
            TextDisplay.ShowDialogue(entry.dialogueId);
        }

        // 만약 duration이 0보다 크다면 끝날 때까지 기다림 (대화가 없으면 duration이 끝날 때까지 기다림)
        if (entry.duration > 0)
        {
            _cutsceneCts = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(entry.duration), cancellationToken: _cutsceneCts.Token);
                // 만약 대화가 있다면 대화가 끝날 때까지 기다림,
                if (string.IsNullOrEmpty(entry.dialogueId))
                {
                    AdvanceCutscene();
                }
            }
            catch (OperationCanceledException)
            {
                // Cutscene was cancelled, do nothing
            }
        }
        else if (string.IsNullOrEmpty(entry.dialogueId))
        {
            // 대화, duration이 없으면 바로 다음 컷신으로 넘어감
            AdvanceCutscene();
        }
        // 그렇지 않으면 대화가 끝날 때까지 기다림 (이벤트로 처리됨)
    }

    private static async UniTask EnsureCutscenePanelExists()
    {
        if (_cutscenePanelInstance == null)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                GameObject.DontDestroyOnLoad(eventSystemObject);
            }

            GameObject prefab = Resources.Load<GameObject>(CUTSCENE_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"Cutscene panel prefab not found: {CUTSCENE_PREFAB_PATH}");
                return;
            }

            _cutscenePanelInstance = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(_cutscenePanelInstance);

            _cutscenePanel = _cutscenePanelInstance.transform.Find("CutsceneBackground")?.GetComponent<RectTransform>();
            _cutsceneImage = _cutscenePanelInstance.transform.Find("CutsceneImage")?.GetComponent<Image>();
            _overlayImage = _cutscenePanelInstance.transform.Find("TransitionOverlay")?.GetComponent<Image>();

            // 대사가 많을때, 다음 컷신으로 스킵(스킵버튼 미구현)
            //Button skipButton = _cutscenePanelInstance.transform.Find("SkipButton")?.GetComponent<Button>();
            //if (skipButton != null)
            //{
            //    skipButton.onClick.AddListener(() => AdvanceCutscene());
            //}

            _cutscenePanelInstance.SetActive(false);

            _initialized = true;
            await UniTask.Yield();
        }
    }

    private static async UniTask DisplayCutsceneImage(CutsceneEntry entry)
    {
        if (_cutsceneImage == null) return;

        // Load cutscene image
        Sprite sprite = Resources.Load<Sprite>(entry.imagePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Cutscene image not found: {entry.imagePath}");
            _cutsceneImage.sprite = null;
            _cutsceneImage.color = new Color(0, 0, 0, 0); // Visible
            return;
        }

        // Apply transition effect
        switch (entry.m_Fade)
        {
            case FadeType.None:
                _cutsceneImage.sprite = sprite;
                break;

            case FadeType.Fade:
                await FadeTransition(sprite, entry.transitionDuration);
                break;

            case FadeType.CrossFade:
                await CrossFadeTransition(sprite, entry.transitionDuration);
                break;

            case FadeType.SlideLeft:
                await SlideTransition(sprite, entry.transitionDuration, true);
                break;

            case FadeType.SlideRight:
                await SlideTransition(sprite, entry.transitionDuration, false);
                break;
        }
    }

    #region Transition effects
    private static async UniTask FadeTransition(Sprite newSprite, float duration)
    {
        if (_overlayImage == null) return;

        // Prepare overlay
        _overlayImage.color = new Color(0, 0, 0, 0);
        _overlayImage.gameObject.SetActive(true);

        // Fade out to black
        float elapsed = 0;
        while (elapsed < duration / 2)
        {
            float alpha = Mathf.Lerp(0, 1, elapsed / (duration / 2));
            _overlayImage.color = new Color(0, 0, 0, alpha);

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        // Change the image
        _cutsceneImage.sprite = newSprite;

        // Fade in from black
        elapsed = 0;
        while (elapsed < duration / 2)
        {
            float alpha = Mathf.Lerp(1, 0, elapsed / (duration / 2));
            _overlayImage.color = new Color(0, 0, 0, alpha);

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        _overlayImage.gameObject.SetActive(false);
    }

    private static async UniTask CrossFadeTransition(Sprite newSprite, float duration)
    {
        if (_overlayImage == null) return;

        // Setup overlay with current image
        _overlayImage.sprite = _cutsceneImage.sprite;
        _overlayImage.color = new Color(1, 1, 1, 1);
        _overlayImage.gameObject.SetActive(true);

        // Set new image on main display but transparent
        _cutsceneImage.sprite = newSprite;

        // Fade out old image (overlay) while fading in new image
        float elapsed = 0;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            _overlayImage.color = new Color(1, 1, 1, 1 - progress);

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        _overlayImage.gameObject.SetActive(false);
    }

    private static async UniTask SlideTransition(Sprite newSprite, float duration, bool slideLeft)
    {
        if (_overlayImage == null) return;

        // Setup overlay with new image
        _overlayImage.sprite = newSprite;
        _overlayImage.color = Color.white;
        _overlayImage.gameObject.SetActive(true);

        // Position the new image off-screen
        RectTransform overlayRect = _overlayImage.GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(slideLeft ? Screen.width : -Screen.width, 0);
        Vector2 endPos = Vector2.zero;

        // Also move the current image
        RectTransform imageRect = _cutsceneImage.GetComponent<RectTransform>();
        Vector2 currentStartPos = Vector2.zero;
        Vector2 currentEndPos = new Vector2(slideLeft ? -Screen.width : Screen.width, 0);

        // Slide animation
        float elapsed = 0;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            overlayRect.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            imageRect.anchoredPosition = Vector2.Lerp(currentStartPos, currentEndPos, progress);

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        // Set final state
        _cutsceneImage.sprite = newSprite;
        imageRect.anchoredPosition = Vector2.zero;
        _overlayImage.gameObject.SetActive(false);
    }
    #endregion

    private static void HandleDialogueEnded(string dialogueId)
    {
        // 현재 진행 중인 컷신이 대화를 마치면 다음컷신으로 진행
        if (_isDisplayingCutscene && _currentEntry != null && _currentEntry.dialogueId == dialogueId)
        {
            AdvanceCutscene();
        }
    }

    public static void AdvanceCutscene()
    {
        if (!_isDisplayingCutscene || _currentEntry == null)
            return;

        // 컷신 진행 알림
        OnCutsceneAdvanced?.Invoke(_currentEntry);

        // 다음 컷신이 있다면 진행
        if (!string.IsNullOrEmpty(_currentEntry.nextCutsceneId))
        {
            CutsceneEntry nextEntry = null;

            if (_currentSequence != null)
            {
                nextEntry = _currentSequence.entries.Find(e => e.id == _currentEntry.nextCutsceneId);
            }

            // 만약 현재 시퀀스에 다음 컷신이 없다면 다른 시퀀스에서 찾음
            if (nextEntry == null)
            {
                foreach (CutsceneSequence sequence in _cutsceneDatabase.sequences)
                {
                    nextEntry = sequence.entries.Find(e => e.id == _currentEntry.nextCutsceneId);
                    if (nextEntry != null)
                    {
                        _currentSequence = sequence;
                        break;
                    }
                }
            }

            if (nextEntry != null)
            {
                ShowCutsceneEntry(nextEntry).Forget();
                return;
            }
        }

        // 다음 컷신이 없다면 컷신 종료
        EndCutscene();
    }

    public static void EndCutscene()
    {
        if (!_isDisplayingCutscene)
            return;

        string cutsceneId = _currentEntry?.id ?? "";

        _cutsceneCts?.Cancel();
        _cutsceneCts?.Dispose();
        _cutsceneCts = null;

        if (_cutscenePanelInstance != null)
        {
            _cutscenePanelInstance.SetActive(false);
        }

        _isDisplayingCutscene = false;
        _currentEntry = null;


        OnCutsceneEnded?.Invoke(cutsceneId);
    }

    // Utility
    public static bool IsDisplayingCutscene()
    {
        return _isDisplayingCutscene;
    }

    public static void SkipCurrentCutscene()
    {
        EndCutscene();
    }
}

[Serializable]
public class CutsceneEntry
{
    public string id;               // Unique identifier for this cutscene
    public string imagePath;        // Path to the cutscene image in Resources
    public string dialogueId;       // ID of the dialogue to display with this cutscene
    public float duration = 0;      // Duration (0 = wait for dialogue to complete)
    public string nextCutsceneId;   // Next cutscene to show (empty = end sequence)
    [FormerlySerializedAs("transition")] public FadeType m_Fade = FadeType.Fade;
    public float transitionDuration = 1.0f;
}

[Serializable]
public class CutsceneSequence
{
    public string cutsceneId;
    public string title;
    public List<CutsceneEntry> entries = new List<CutsceneEntry>();
}

[Serializable]
public class CutsceneDatabase
{
    public List<CutsceneSequence> sequences = new List<CutsceneSequence>();
}

public enum FadeType
{
    None,
    Fade,
    CrossFade,
    SlideLeft,
    SlideRight
}
