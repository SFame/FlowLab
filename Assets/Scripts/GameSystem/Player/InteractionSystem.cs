using System.Collections.Generic;
using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private Transform interactionPoint;

    private CircleCollider2D _interactionCollider;
    private IInteractable closestInteractable;
    private bool _isInteracting = false;

    private Dictionary<Collider2D, IInteractable> _interactablesInRange = new Dictionary<Collider2D, IInteractable>();

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
    private void Awake()
    {
        //플레이어 프리펩에 주변 오브젝트 체크용 CircleCollider2D 추가 요망
        _interactionCollider = GetComponent<CircleCollider2D>();

        _interactionCollider.radius = interactionRadius;
        _interactionCollider.isTrigger = true;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            _interactablesInRange[other] = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_interactablesInRange.ContainsKey(other))
        {
            if (_interactablesInRange[other] == closestInteractable)
            {
                closestInteractable.OnSelected = false;
                closestInteractable = null;
            }

            _interactablesInRange.Remove(other);
        }
    }

    private IInteractable FindClosestInteractable()
    {
        if (_interactablesInRange.Count == 0)
        {
            return null;
        }

        float closestDistance = float.MaxValue;
        IInteractable closest = null;

        List<Collider2D> toRemove = new List<Collider2D>();
        foreach (var entry in _interactablesInRange)
        {
            if (entry.Key == null || entry.Value == null || entry.Value.GetTransform() == null)
            {
                toRemove.Add(entry.Key);
                continue;
            }

            float distance = Vector2.Distance(
                transform.position,
                entry.Value.GetTransform().position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = entry.Value;
            }
        }

        // Remove any null entries
        foreach (var key in toRemove)
        {
            _interactablesInRange.Remove(key);
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