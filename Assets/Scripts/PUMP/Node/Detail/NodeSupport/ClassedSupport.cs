using UnityEngine;

public class ClassedSupport : MonoBehaviour
{
    [SerializeField] private GameObject m_isChangeGameObject;

    private bool _isChange = false;

    public bool IsChange
    {
        get => _isChange;
        set
        {
            _isChange = value;
            m_isChangeGameObject.SetActive(_isChange);
        }
    }
}