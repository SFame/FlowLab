using UnityEngine;

public class InteractionTest : MonoBehaviour, IInteractable
{

    [Header("Visual")]
    [SerializeField] private GameObject highlightIndicator;

    private bool _onSelected = false;

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
        Debug.Log("Interacted with " + name);
    }


}