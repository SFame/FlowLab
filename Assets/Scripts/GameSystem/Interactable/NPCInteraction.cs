using UnityEngine;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("NPC Configuration")]
    [SerializeField] private string npcDialogueId;
    [SerializeField] private GameObject highlightIndicator;

    [Header("Optional")]
    [SerializeField] private bool disableInteractionDuringDialogue = true;

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
        // 대화중이거나 대화를 시작할 수 없는 상태라면 대화를 시작하지 않음
        if (disableInteractionDuringDialogue && TextDisplay.IsDialogueActive())
        {
            // 대화중이면 다음 대화로 진행
            TextDisplay.AdvanceDialogue();
            return;
        }

        // If we don't have a dialogue ID, we can't start a dialogue
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