using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class Door : MonoBehaviour, IInteractable
{
    public Room _room;
    public Vector3 exitPosition;

    private bool _selected;
    private bool _opened;

    public bool OnSelected { get => _selected; set => _selected = value; }
    public List<PuzzleInteraction> PuzzleList
    {
        get
        {
            return _room.GetRoomObjects().Select( x => x.GetComponent<PuzzleInteraction>() ).ToList();
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Interact()
    {
        Debug.Log($"{PuzzleList.Where(x => x.Clear).ToList().Count}");
        Debug.Log($"{PuzzleList.Count}");
        if(PuzzleList.Where(x => x.Clear).ToList().Count == PuzzleList.Count)
            GameManager.Instance.Player.transform.position = exitPosition;
    }
}
