using UnityEngine;

[RequireComponent (typeof(CanvasGroup))]
public class NodeSelectingDragIgnoreTarget : MonoBehaviour
{
    private NodeSelectingHandler _nodeSelectingHandler;
    private CanvasGroup _canvasGroup;
    private bool _ignoreRaycast = false;

    private CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            
            return _canvasGroup;
        }
    }

    private void Start()
    {
        _nodeSelectingHandler = GetComponentInParent<NodeSelectingHandler>();

        if (_nodeSelectingHandler != null)
        {
            _nodeSelectingHandler.AddIgnoreTarget(this);
        }
    }

    private void OnDestroy()
    {
        if (_nodeSelectingHandler == null)
        {
            return;
        }

        _nodeSelectingHandler.RemoveIgnoreTarget(this);
    }

    public bool IgnoreRaycast
    {
        get => _ignoreRaycast;
        set
        {
            _ignoreRaycast = value;
            CanvasGroup.blocksRaycasts = !_ignoreRaycast;
            CanvasGroup.interactable = !_ignoreRaycast;
        }
    }
}
