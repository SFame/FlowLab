using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactionPoint;

    private IInteractable closestInteractable;
    private bool _isInteracting = false;
    private void OnEnable()
    {
        TextDisplay.OnDialogueStarted += HandleDialogueStarted;
        TextDisplay.OnDialogueEnded += HandleDialogueEnded;
    }
    private void OnDisable()
    {

        TextDisplay.OnDialogueStarted -= HandleDialogueStarted;
        TextDisplay.OnDialogueEnded -= HandleDialogueEnded;
    }
    private void Start()
    {
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }
    }

    private void Update()
    {
        if (_isInteracting)
        {
            return;
        }
        IInteractable newClosestInteractable = FindClosestInteractable();

        if (newClosestInteractable != closestInteractable) 
        {
            if (closestInteractable != null)
            {
                closestInteractable.OnSelected = false;
            }
            if (newClosestInteractable != null)
            {
                newClosestInteractable.OnSelected = true;
            }

            closestInteractable = newClosestInteractable;
        }
        if (Input.GetKeyDown(KeyCode.E) && closestInteractable != null)
        {
            closestInteractable.Interact();
        }
    }

    private IInteractable FindClosestInteractable()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            interactionPoint.position,
            interactionRadius,
            interactableLayer
        );

        if (colliders.Length == 0)
        {
            return null;
        }

        float closestDistance = float.MaxValue;
        IInteractable closest = null;

        foreach (Collider2D collider in colliders)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                float distance = Vector2.Distance(
                    interactionPoint.position,
                    interactable.GetTransform().position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }
        }

        return closest;
    }

    private void HandleDialogueStarted(string dialogueId)
    {
        _isInteracting = true;
    }

    private void HandleDialogueEnded(string dialogueId)
    {
        _isInteracting = false;
    }
}