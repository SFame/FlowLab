using UnityEngine;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("NPC Configuration")]
    [SerializeField] private string npcDialogueId;
    [SerializeField] private GameObject highlightIndicator;


    private bool _onSelected = false;
    private bool _interactionInProgress = false;

    private void Start()
    {
        // Subscribe to dialogue events to track when dialogue ends
        TextDisplay.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        TextDisplay.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueEnded(string dialogueId)
    {
        // If this was our dialogue that ended, we're no longer in an interaction
        if (dialogueId == npcDialogueId)
        {
            _interactionInProgress = false;
        }
    }

    public bool OnSelected
    {
        get => _onSelected;
        set
        {
            if (_onSelected != value)
            {
                _onSelected = value;
                if (highlightIndicator != null)
                {
                    highlightIndicator.SetActive(value);
                }
            }
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Interact()
    {
        if (TextDisplay.IsDialogueActive())
        {
            // 대화중이면 다음 대화로 진행
            TextDisplay.AdvanceDialogue();
            return;
        }

        // 대화데이터에 대화ID가 없으면 경고
        if (string.IsNullOrEmpty(npcDialogueId))
        {
            Debug.LogWarning($"NPC {gameObject.name} doesn't have a dialogue ID assigned");
            return;
        }

        // 대화시작
        TextDisplay.ShowDialogue(npcDialogueId);
        _interactionInProgress = true;

        Debug.Log($"Started dialogue with NPC: {gameObject.name}");
    }
}