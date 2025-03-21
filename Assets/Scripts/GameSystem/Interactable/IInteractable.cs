using UnityEngine;

public interface IInteractable
{
    bool OnSelected { get; set; }

    Transform GetTransform();

    void Interact();

}