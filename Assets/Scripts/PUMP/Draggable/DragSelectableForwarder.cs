using UnityEngine;

public class DragSelectableForwarder : MonoBehaviour, IDragSelectableForwarder
{
    #region On Inspector
    [SerializeField] private Component m_DragSelectable;
    [SerializeField] private bool m_AutoFindOnAwake = false;
    #endregion

    #region Privates
    private IDragSelectable _target;

    private IDragSelectable Target
    {
        get
        {
            _target ??= m_DragSelectable?.GetComponent<IDragSelectable>();
            return _target;
        }
    }

    private void Awake()
    {
        if (m_AutoFindOnAwake && Target == null)
        {
            _target = GetComponentInParent<IDragSelectable>();
        }

        if (Target == null)
        {
            Debug.LogWarning($"{name}: IDragSelectable component can't find");
        }
    }
    #endregion

    public IDragSelectable GetDragSelectable()
    {
        return Target;
    }
}